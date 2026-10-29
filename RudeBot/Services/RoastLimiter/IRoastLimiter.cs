namespace RudeBot.Services.RoastLimiter;

public interface IRoastLimiter
{
    /// <summary>
    /// Займає денний слот /roastme для користувача в чаті. false — сьогодні вже було.
    /// </summary>
    bool TryAcquire(long chatId, long userId);

    /// <summary>
    /// Повертає слот, якщо roast не вдався (наприклад, Gemini впав), щоб людина могла спробувати ще.
    /// </summary>
    void Release(long chatId, long userId);
}
