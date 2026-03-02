namespace RudeBot.Services.ChatContextService;

public class ChatContextService : IChatContextService
{
    private const int MaxMessages = 10;
    private readonly Dictionary<long, LinkedList<ChatContextMessage>> _cache = new();
    private readonly object _lock = new();

    public void AddMessage(long chatId, string userName, string text)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(chatId, out var messages))
            {
                messages = new LinkedList<ChatContextMessage>();
                _cache[chatId] = messages;
            }

            messages.AddLast(new ChatContextMessage(userName, text));

            if (messages.Count > MaxMessages)
            {
                messages.RemoveFirst();
            }
        }
    }

    public List<ChatContextMessage> GetMessages(long chatId)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(chatId, out var messages))
            {
                return messages.ToList();
            }

            return new List<ChatContextMessage>();
        }
    }
}
