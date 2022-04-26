using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Managers
{
    internal class TicketManager : ITicketManager
    {
        private readonly DataContext _dbContext;

        public TicketManager()
        {
            _dbContext = new DataContext();
        }

        public async Task<string> GetChatTickets(long chatId)
        {
            String tasksString = "Тікетів не знайдено";

            using (var _dbContext = new DataContext())
            {
                var tickets = await _dbContext.Tickets.AsNoTracking()
                    .Where(x => x.ChatId == chatId)
                    .ToListAsync();

                if (tickets.Any())
                {
                    tasksString = "Тікети чату:\n\n";

                    tickets.ForEach(x =>
                    {
                        tasksString += $"Id {x.Id}, Created {x.Created.ToShortDateString()}, {x.Ticket}\n";
                    });
                }
            }

            return tasksString;
        }
    }
}
