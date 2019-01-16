namespace todo
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Azure.CosmosDB.BulkExecutor;
    using Microsoft.Azure.CosmosDB.BulkExecutor.BulkImport;
    using System.Threading;
    using System.IO;
    using System.Text;
    using todo.Server;
    using log4net;

    public static class DocumentDBRepository<T> where T : class
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["database"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["collection"];
        private static readonly string qrcodeTable = ConfigurationManager.AppSettings["qrcodeTable"];
        private static readonly bool ShouldCleanupOnStart = false;
        private static readonly bool ShouldCleanupOnFinish = false;
        private static readonly long numberOfBatches = 10;
        private static readonly long numberOfDocumentsPerBatch = 700000;

        private static ILog logger = LogManager.GetLogger("LogHelper");

        private static DocumentClient client;

        public static async Task<T> GetItemAsync(string id)
        {
            try
            {
                Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1 })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
        }

        public static async Task DeleteItemAsync(string id)
        {
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
        }

        public static void Initialize()
        {
            client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]), ConfigurationManager.AppSettings["authKey"], ConnectionPolicy);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private static readonly ConnectionPolicy ConnectionPolicy = new ConnectionPolicy
        {
            ConnectionMode = ConnectionMode.Direct,
            ConnectionProtocol = Protocol.Tcp
        };

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 批量导入
        /// </summary>
        /// <returns></returns>
        public static async Task RunBulkImportAsync(string file, string orderId)
        {
            //读取文件

            try
            {
                DocumentCollection dataCollection = null;
                if (client == null)
                {
                    client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]), ConfigurationManager.AppSettings["authKey"], ConnectionPolicy);
                }
                dataCollection = Utils.GetCollectionIfExists(client, DatabaseId, qrcodeTable);
                if (dataCollection == null)
                {
                    throw new Exception("The data collection does not exist");
                }

                // Prepare for bulk import.
                // Creating documents with simple partition key here.
                string partitionKeyProperty = dataCollection.PartitionKey.Paths[0].Replace("/", "");

                // Set retry options high for initialization (default values).
                client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
                client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;

                IBulkExecutor bulkExecutor = new BulkExecutor(client, dataCollection);
                await bulkExecutor.InitializeAsync();

                // Set retries to 0 to pass control to bulk executor.
                client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
                client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

                BulkImportResponse bulkImportResponse = null;
                long totalNumberOfDocumentsInserted = 0;
                double totalRequestUnitsConsumed = 0;
                double totalTimeTakenSec = 0;

                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;

                StreamReader sourceFileStream = new StreamReader(file, Encoding.Default);

                for (int i = 0; i < numberOfBatches && sourceFileStream != null; i++)
                {
                    // Generate JSON-serialized documents to import.
                    List<string> documentsToImportInBatch = new List<string>();
                    long prefix = i * numberOfDocumentsPerBatch;
                    // 批量文档写入，读取文件, 写入bulk中
                    documentsToImportInBatch = Utils.AddDocumentFromFile(sourceFileStream, numberOfDocumentsPerBatch, orderId, out sourceFileStream);
                    // Invoke bulk import API.
                    var tasks = new List<Task>();
                    tasks.Add(Task.Run(async () =>
                    {
                        //Trace.TraceInformation(String.Format("Executing bulk import for batch {0}", i));
                        do
                        {
                            try
                            {
                                bulkImportResponse = await bulkExecutor.BulkImportAsync(
                                    documents: documentsToImportInBatch,
                                    enableUpsert: true,
                                    disableAutomaticIdGeneration: true,
                                    maxConcurrencyPerPartitionKeyRange: null,
                                    maxInMemorySortingBatchSize: null,
                                    cancellationToken: token);
                            }
                            catch (DocumentClientException de)
                            {
                                //Trace.TraceError("Document client exception: {0}", de);
                                break;
                            }
                            catch (Exception e)
                            {
                                //Trace.TraceError("Exception: {0}", e);
                                break;
                            }
                        } while (bulkImportResponse.NumberOfDocumentsImported < documentsToImportInBatch.Count);

                        totalNumberOfDocumentsInserted += bulkImportResponse.NumberOfDocumentsImported;
                        totalRequestUnitsConsumed += bulkImportResponse.TotalRequestUnitsConsumed;
                        totalTimeTakenSec += bulkImportResponse.TotalTimeTaken.TotalSeconds;
                        logger.InfoFormat("fileName:{0} orderId:{1} ", file, orderId);
                        logger.InfoFormat("totalNumberOfDocumentsInserted:{0} totalRequestUnitsConsumed:{1} totalTimeTakenSec:{2}", totalNumberOfDocumentsInserted, totalRequestUnitsConsumed, totalTimeTakenSec);
                    },
                    token));

                    await Task.WhenAll(tasks);
                }


            }
            catch (Exception de)
            {
                logger.InfoFormat("Insert Error: ", de);
            }


        }
    }
}