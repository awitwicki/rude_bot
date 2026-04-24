namespace RudeBot.Models;

public class ChatMessage
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Text { get; set; } = "";
    public int? MessageId { get; set; }
    public DateTime CreatedAt { get; set; }
}
