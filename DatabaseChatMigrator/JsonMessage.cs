using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseChatMigrator
{
    internal class JsonMessage
    {
        public int id { get; set; }
        public string type { get; set; }
        public DateTime date { get; set; }
        public string actor { get; set; }
        public string actor_id { get; set; }
        public string action { get; set; }
        public string title { get; set; }
        public object text { get; set; }
        public string from { get; set; }
        public string from_id { get; set; }
        public List<string> members { get; set; }
        public string photo { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public string file { get; set; }
        public string thumbnail { get; set; }
        public string media_type { get; set; }
        public string sticker_emoji { get; set; }
        public int? reply_to_message_id { get; set; }
    }
}
