namespace RudeBot.Services.ChatDigestService;

public class ChatDigestService : IChatDigestService
{
    private readonly Dictionary<long, List<ChatDigestMessage>> _cache = new();
    private readonly object _lock = new();

    public void AddMessage(long chatId, string userName, string text)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(chatId, out var messages))
            {
                messages = new List<ChatDigestMessage>();
                _cache[chatId] = messages;
            }

            messages.Add(new ChatDigestMessage(userName, text, DateTime.UtcNow));
        }
    }

    public List<ChatDigestMessage> GetAndClearMessages(long chatId)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(chatId, out var messages))
            {
                var result = new List<ChatDigestMessage>(messages);
                messages.Clear();
                return result;
            }

            return new List<ChatDigestMessage>();
        }
    }

    public IEnumerable<long> GetActiveChatIds()
    {
        lock (_lock)
        {
            return _cache.Keys.ToList();
        }
    }
}
