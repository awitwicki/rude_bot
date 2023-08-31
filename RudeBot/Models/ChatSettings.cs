namespace RudeBot.Models;

public class ChatSettings
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public bool HaterussianLang { get; set; }
    public bool UseChatGpt { get; set; }
    public bool SendRandomMessages { get; set; }
}