namespace RudeBot.Managers;

public interface ITicketManager
{
    Task<string> GetChatTickets(long chatId);
    Task AddTicket(long chatId, string text);
    Task<bool> RemoveTicket(long requestChatId, long ticketId);
}