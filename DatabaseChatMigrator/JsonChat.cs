using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseChatMigrator
{
    internal class JsonChat
    {
        public string name { get; set; }
        public string type { get; set; }
        public long id { get; set; }
        public List<JsonMessage> messages = new List<JsonMessage>();
    }
}
