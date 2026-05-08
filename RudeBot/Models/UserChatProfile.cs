namespace RudeBot.Models;

public class UserChatProfile
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Profile { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}
