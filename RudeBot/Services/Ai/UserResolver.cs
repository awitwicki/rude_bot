namespace RudeBot.Services.Ai;

public sealed record UserResolution(RosterEntry? Entry, IReadOnlyList<string> Candidates)
{
    public static readonly UserResolution NotFound = new(null, Array.Empty<string>());

    public bool IsResolved => Entry is not null;
    public bool IsAmbiguous => Entry is null && Candidates.Count > 0;
}

/// <summary>
/// Зводить ім'я, яке назвала модель, до конкретного учасника ростера.
/// Неоднозначність і відсутність — не помилки, а результат: інструмент віддає їх
/// моделі як звичайну відповідь, і та може перепитати або уточнити.
/// </summary>
public static class UserResolver
{
    public static UserResolution Resolve(IReadOnlyList<RosterEntry> roster, string? query)
    {
        var needle = Normalize(query);

        if (needle.Length == 0) return UserResolution.NotFound;

        var exact = roster
            .Where(e => NamesOf(e).Any(n => Normalize(n) == needle))
            .ToList();

        if (exact.Count > 0) return Pick(exact);

        var partial = roster
            .Where(e => NamesOf(e).Any(n => Normalize(n).Contains(needle)))
            .ToList();

        if (partial.Count > 0) return Pick(partial);

        return UserResolution.NotFound;
    }

    private static UserResolution Pick(List<RosterEntry> matches) =>
        matches.Count == 1
            ? new UserResolution(matches[0], Array.Empty<string>())
            : new UserResolution(null, matches.Select(m => m.DisplayName).ToList());

    private static IEnumerable<string> NamesOf(RosterEntry entry)
    {
        yield return entry.DisplayName;

        foreach (var alias in entry.Aliases)
        {
            yield return alias;
        }
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().TrimStart('@').ToLowerInvariant();
}
