namespace RudeBot.Services.ChatContextService;

public interface IChatContextService
{
    Task<List<ChatContextMessage>> GetMessagesAsync(long chatId);
}
