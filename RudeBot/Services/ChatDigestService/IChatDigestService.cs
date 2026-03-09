namespace RudeBot.Services.ChatDigestService;

public interface IChatDigestService
{
    void AddMessage(long chatId, string userName, string text);
    List<ChatDigestMessage> GetAndClearMessages(long chatId);
    IEnumerable<long> GetActiveChatIds();
}
