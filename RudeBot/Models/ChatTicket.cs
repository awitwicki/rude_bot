using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Models
{
    public class ChatTicket
    {
        public long Id { get; set; }
        public long ChatId { get; set; }
        public string Ticket { get; set; }
        public DateTime Created { get; set; }
    }
}
