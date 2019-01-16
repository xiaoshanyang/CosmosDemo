using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace todo.Models
{
    public class qrcodeModel
    {
        public String id { get; set; }
        public String categoryId { get; set; }
        public String orderId { get; set; }
        public String content { get; set; }
        public String content1 { get; set; }
        public int state { get; set; }
        public String url { get; set; }
        public long serialNum { get; set; }
        //public int codeIndex { get; set; }
    }
}