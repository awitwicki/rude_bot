using RudeBot.Models;
using RudeBot.Services.ChatContextService;

namespace RudeBot.Services.Ai;

/// <summary>
/// Зливає три джерела імен в один ростер. У базі співіснують дві форми імені:
/// display name (TelegramUser.UserName) і @handle (ChatMessage/UserChatProfile.UserName),
/// і модель бачить у контексті саме handle — тому потрібні обидві.
/// </summary>
public static class RosterBuilder
{
    public const int MaxEntries = 100;

    public static List<RosterEntry> Build(
        IEnumerable<UserChatStats> stats,
        IEnumerable<UserChatProfile> profiles,
        IEnumerable<ChatContextMessage> context,
        long botUserId)
    {
        var aliasesByUser = new Dictionary<long, List<string>>();

        void AddAlias(long userId, string? name)
        {
            if (userId == 0 || userId == botUserId) return;
            if (string.IsNullOrWhiteSpace(name)) return;

            if (!aliasesByUser.TryGetValue(userId, out var list))
            {
                list = new List<string>();
                aliasesByUser[userId] = list;
            }

            var trimmed = name.Trim();

            if (!list.Any(a => string.Equals(a, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(trimmed);
            }
        }

        foreach (var profile in profiles) AddAlias(profile.UserId, profile.UserName);
        foreach (var message in context) AddAlias(message.UserId, message.UserName);

        var entries = new List<RosterEntry>();

        foreach (var stat in stats)
        {
            if (stat.UserId == 0 || stat.UserId == botUserId) continue;

            var aliases = aliasesByUser.TryGetValue(stat.UserId, out var found)
                ? found
                : new List<string>();

            var displayName = stat.User?.UserName?.Trim() ?? "";

            if (displayName.Length == 0)
            {
                if (aliases.Count == 0) continue;
                displayName = aliases[0];
            }

            entries.Add(new RosterEntry
            {
                UserId = stat.UserId,
                DisplayName = displayName,
                Aliases = aliases
                    .Where(a => !string.Equals(a, displayName, StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                Karma = stat.Karma,
                Warns = stat.Warns,
                TotalMessages = stat.TotalMessages,
                TotalBadWords = stat.TotalBadWords,
            });
        }

        return entries
            .OrderByDescending(e => e.TotalMessages)
            .Take(MaxEntries)
            .ToList();
    }
}
