using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using todo.Models;

namespace todo.Server
{
    public class Utils
    {
        /// <summary>
        /// Get the collection if it exists, null if it doesn't.
        /// </summary>
        /// <returns>The requested collection.</returns>
        static internal DocumentCollection GetCollectionIfExists(DocumentClient client, string databaseName, string collectionName)
        {
            if (GetDatabaseIfExists(client, databaseName) == null)
            {
                return null;
            }

            return client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(databaseName))
                .Where(c => c.Id == collectionName).AsEnumerable().FirstOrDefault();
        }

        /// <summary>
        /// Get the database if it exists, null if it doesn't.
        /// </summary>
        /// <returns>The requested database.</returns>
        static internal Database GetDatabaseIfExists(DocumentClient client, string databaseName)
        {
            return client.CreateDatabaseQuery().Where(d => d.Id == databaseName).AsEnumerable().FirstOrDefault();
        }

        static internal String GenerateRandomDocumentString(String data, String url, String orderId, Random rd)
        {
            String content = data;
            String content1 = "";
            if (data.IndexOf(',') > 0)
            {
                content = data.Split(',')[0];
                content1 = data.Split(',')[1];
            }

            qrcodeModel doc = new qrcodeModel();
            doc.id = content.Substring(content.LastIndexOf('/') + 1);
            doc.categoryId = "201812061402363333";
            doc.orderId = orderId;
            doc.content = content;
            doc.content1 = content1;
            doc.url = url;
            doc.state = 1;
            //doc.serialNum = 1;
            //doc.codeIndex = rd.Next(1000);
            return JsonConvert.SerializeObject(doc);
        }

        static internal List<string> AddDocumentFromFile(StreamReader sourceReaderStream, long readLimitLineCount, String orderId, out StreamReader sr)
        {
            sr = null;
            List<string> documentsToImportInBatch = new List<string>();
            // 读取文件区间
            String line;
            while (readLimitLineCount-- > 0 && (line = sourceReaderStream.ReadLine()) != null)
            {
                //Console.WriteLine(line.ToString());
                documentsToImportInBatch.Add(GenerateRandomDocumentString(line, "", orderId, new Random()));
            }
            if (readLimitLineCount > 0)
            {
                sourceReaderStream.Close();
                sr = null;
            }
            else
            {
                sr = sourceReaderStream;
            }

            return documentsToImportInBatch;
        }
    }
}