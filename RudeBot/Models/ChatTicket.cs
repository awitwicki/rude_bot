namespace RudeBot.Models;

public class ChatTicket
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public string Ticket { get; set; }
    public DateTime Created { get; set; }
}