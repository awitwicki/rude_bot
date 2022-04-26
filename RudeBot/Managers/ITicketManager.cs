using RudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Managers
{
    public interface ITicketManager
    {
        Task<string> GetChatTickets(long chatId);
    }
}
