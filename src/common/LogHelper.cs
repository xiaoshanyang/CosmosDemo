using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace todo.common
{
    public class LogHelper
    {
        private static ILog log = LogManager.GetLogger("LogHelper");
        public static void WriteLog(string errorMsg)
        {
            log.Error("\n----------------出错开始-------------------\n" + errorMsg + "\n----------------出错结束-------------------\n");
        }
    }

}