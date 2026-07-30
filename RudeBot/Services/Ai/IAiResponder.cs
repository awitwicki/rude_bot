namespace RudeBot.Services.Ai;

public interface IAiResponder
{
    /// <summary>
    /// Відповідь «кота» на повідомлення. Кидає виняток, якщо Gemini недоступний —
    /// фолбек на Resources.OopsIDidntAgain лишається відповідальністю хендлера.
    /// </summary>
    Task<string> RespondAsync(
        long chatId,
        long userId,
        string userName,
        string message,
        CancellationToken cancellationToken = default);
}
