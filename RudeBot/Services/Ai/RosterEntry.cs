namespace RudeBot.Services.Ai;

/// <summary>
/// Учасник чату в тому вигляді, в якому його бачать промпт і інструменти:
/// одне канонічне ім'я плюс усі аліаси, за якими модель може його назвати.
/// </summary>
public sealed class RosterEntry
{
    public long UserId { get; init; }
    public string DisplayName { get; init; } = "";
    public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();
    public int Karma { get; init; }
    public int Warns { get; init; }
    public int TotalMessages { get; init; }
    public int TotalBadWords { get; init; }
}
