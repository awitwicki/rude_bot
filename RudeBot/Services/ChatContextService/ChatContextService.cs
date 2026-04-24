namespace RudeBot.Services.ChatContextService;

public class ChatContextService : IChatContextService
{
    private readonly IChatMessageRepository _repo;

    public ChatContextService(IChatMessageRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ChatContextMessage>> GetMessagesAsync(long chatId)
    {
        var rows = await _repo.GetLastNAsync(chatId, ChatContextConsts.WindowSize);
        return rows.Select(m => new ChatContextMessage(m.UserName, m.Text)).ToList();
    }
}
