using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text;
using log4net;
using System.Threading;

namespace todo.Server
{
    public class InsertDocument
    {
        // 数据库信息
        private static readonly string EndpointUrl = "https://vdpdemo.documents.azure.cn:443/";
        private static readonly string AuthorizationKey = "tFnZWfWFwbHQsrRvXjWTSJ75WsjdOvJOAdIfsw2RBLLa2cCFRqwP6dUU4t5L5p5wfivmVyQGwhcj3A8EYMBKpA==";
        private static readonly string DatabaseName = "ToDoList";
        private static readonly string CollectionName = "qrcode";

        private static ILog log = LogManager.GetLogger("LogHelper");


        // 定时器，等待触发后开始读取txt文件，导入数据库
        // 定时器，监视out文件夹内txt文件
        private static System.Threading.Timer timerFileMonitor = new System.Threading.Timer(new TimerCallback(MonitorTxtFile), null, -1, -1);
        private static bool isRunning = false;
        private static string orderId = string.Empty;
        private static string filePath = string.Empty;

        public void startInsert(string filePath, string zipFileName, string orderId)
        {
            var archive = ArchiveFactory.Open(zipFileName);
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory)
                {
                    //entry.WriteToFile(filePath.Replace(".zip", ".txt"));
                    entry.WriteToDirectory(filePath, new SharpCompress.Common.ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                }
            }
            // 删除zip文件
            archive.Dispose();
            File.Delete(zipFileName);

            InsertDocument.orderId = orderId;
            InsertDocument.filePath = filePath;
            // 解压完成，导入数据库
            timerFileMonitor.Change(0, -1);
        }

        public static void MonitorTxtFile(object sender)
        {
            if (!isRunning)
            {
                isRunning = true;
                // 读取文件
                BatchInsertDoc();
            }
        }

        private static void BatchInsertDoc()
        {
            // 读取文件 string.Format("~/{0}", "File")
            //string filePath = HttpContext.Current.Server.MapPath("~/File");
            string[] files = Directory.GetFiles(InsertDocument.filePath, "*.txt");
            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    DocumentDBRepository<Object>.RunBulkImportAsync(files[i], orderId).Wait();
                    //删除当前文件
                    File.Delete(files[i]);
                }
                BatchInsertDoc();
            }
            else
            {
                isRunning = false;
                timerFileMonitor.Change(-1, -1);
            }
        }

        
    }
}