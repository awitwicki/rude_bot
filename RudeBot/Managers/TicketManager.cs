using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;

namespace RudeBot.Managers;

internal class TicketManager : ITicketManager
{
    private readonly DataContext _dbContext;

    public TicketManager()
    {
        _dbContext = new DataContext();
    }

    public async Task AddTicket(long chatId, string text)
    {
        using (var _dbContext = new DataContext())
        {
            var ticket = new ChatTicket()
            {
                Created = DateTime.UtcNow,
                ChatId = chatId,
                Ticket = text
            };

            await _dbContext.Tickets.AddAsync(ticket);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<string> GetChatTickets(long chatId)
    {
        var tasksString = "Тікетів не знайдено";

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

    public async Task<bool> RemoveTicket(long requestChatId, long ticketId)
    {
        using (var _dbContext = new DataContext())
        {
            var ticket = await _dbContext.Tickets.AsNoTracking()
                .Where(x => x.Id == ticketId && x.ChatId == requestChatId)
                .FirstOrDefaultAsync();

            if (ticket == null) 
                return false;

            _dbContext.Tickets.Remove(ticket);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}