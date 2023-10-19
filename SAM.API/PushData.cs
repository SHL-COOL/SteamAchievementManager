using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAM.API
{
    public class PushData
    {
        public string appToken { get; set; }
        public int appId { get; set; }
        public List<string> uids { get; set; }
        public List<object> topicIds { get; set; }
        public string summary { get; set; }
        public string content { get; set; }
        public int contentType { get; set; }
        public bool verifyPay { get; set; }
    }
}
