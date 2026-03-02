namespace RudeBot.Services.ChatContextService;

public interface IChatContextService
{
    void AddMessage(long chatId, string userName, string text);
    List<ChatContextMessage> GetMessages(long chatId);
}
