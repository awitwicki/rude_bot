namespace RudeBot.Services.RoastLimiter;

/// <summary>
/// In-memory ліміт: один /roastme на користувача на чат за календарний день (UTC).
/// Після рестарту бота ліміти скидаються — для антиспаму цього достатньо.
/// </summary>
public class RoastLimiter : IRoastLimiter
{
    private readonly Func<DateTime> _utcNow;
    private readonly Dictionary<(long ChatId, long UserId), DateOnly> _lastUsed = new();
    private readonly object _lock = new();

    public RoastLimiter() : this(() => DateTime.UtcNow)
    {
    }

    internal RoastLimiter(Func<DateTime> utcNow)
    {
        _utcNow = utcNow;
    }

    public bool TryAcquire(long chatId, long userId)
    {
        var today = DateOnly.FromDateTime(_utcNow());

        lock (_lock)
        {
            if (_lastUsed.TryGetValue((chatId, userId), out var lastUsed) && lastUsed == today)
            {
                return false;
            }

            // Drop stale entries so the dictionary doesn't grow forever
            foreach (var key in _lastUsed.Where(x => x.Value < today).Select(x => x.Key).ToList())
            {
                _lastUsed.Remove(key);
            }

            _lastUsed[(chatId, userId)] = today;
            return true;
        }
    }

    public void Release(long chatId, long userId)
    {
        lock (_lock)
        {
            _lastUsed.Remove((chatId, userId));
        }
    }
}
