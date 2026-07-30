using RudeBot.Models;
using RudeBot.Services.Ai;
using RudeBot.Services.ChatContextService;

namespace RudeBot.Tests.Ai;

public class RosterBuilderTests
{
    private static UserChatStats Stats(long userId, string displayName, int totalMessages = 0) =>
        new()
        {
            UserId = userId,
            ChatId = 1,
            TotalMessages = totalMessages,
            User = new TelegramUser { Id = userId, UserName = displayName, UserMention = $"[{displayName}](tg://user?id={userId})" }
        };

    [Fact]
    public void Build_MergesAliasesFromProfilesAndContext()
    {
        var roster = RosterBuilder.Build(
            new[] { Stats(7, "John Doe") },
            new[] { new UserChatProfile { UserId = 7, UserName = "johndoe" } },
            new[] { new ChatContextMessage("johndoe_new", "привіт", 7) },
            botUserId: 999);

        var entry = Assert.Single(roster);
        Assert.Equal("John Doe", entry.DisplayName);
        Assert.Equal(new[] { "johndoe", "johndoe_new" }, entry.Aliases);
    }

    [Fact]
    public void Build_DeduplicatesAliasesIgnoringCase()
    {
        var roster = RosterBuilder.Build(
            new[] { Stats(7, "John") },
            new[] { new UserChatProfile { UserId = 7, UserName = "johndoe" } },
            new[]
            {
                new ChatContextMessage("JOHNDOE", "a", 7),
                new ChatContextMessage("johndoe", "b", 7),
            },
            botUserId: 999);

        var entry = Assert.Single(roster);
        Assert.Equal(new[] { "johndoe" }, entry.Aliases);
    }

    [Fact]
    public void Build_DropsAliasEqualToDisplayName()
    {
        var roster = RosterBuilder.Build(
            new[] { Stats(7, "johndoe") },
            new[] { new UserChatProfile { UserId = 7, UserName = "johndoe" } },
            Array.Empty<ChatContextMessage>(),
            botUserId: 999);

        Assert.Empty(Assert.Single(roster).Aliases);
    }

    [Fact]
    public void Build_ExcludesBotAndUnknownUsers()
    {
        var roster = RosterBuilder.Build(
            new[] { Stats(7, "John"), Stats(999, "RudeBot"), Stats(0, "Ніхто") },
            Array.Empty<UserChatProfile>(),
            new[] { new ChatContextMessage("RudeBot", "я бот", 999) },
            botUserId: 999);

        Assert.Equal(new long[] { 7 }, roster.Select(e => e.UserId));
    }

    [Fact]
    public void Build_FallsBackToFirstAliasWhenDisplayNameMissing()
    {
        var stats = Stats(7, "");
        var roster = RosterBuilder.Build(
            new[] { stats },
            new[] { new UserChatProfile { UserId = 7, UserName = "johndoe" } },
            Array.Empty<ChatContextMessage>(),
            botUserId: 999);

        var entry = Assert.Single(roster);
        Assert.Equal("johndoe", entry.DisplayName);
        Assert.Empty(entry.Aliases);
    }

    [Fact]
    public void Build_SkipsUsersWithNoNameAtAll()
    {
        var roster = RosterBuilder.Build(
            new[] { Stats(7, "") },
            Array.Empty<UserChatProfile>(),
            Array.Empty<ChatContextMessage>(),
            botUserId: 999);

        Assert.Empty(roster);
    }

    [Fact]
    public void Build_CarriesStatsAndSortsByTotalMessagesDescending()
    {
        var quiet = Stats(7, "Quiet", totalMessages: 3);
        var loud = Stats(8, "Loud", totalMessages: 50);
        loud.Karma = 12;
        loud.Warns = 1;
        loud.TotalBadWords = 4;

        var roster = RosterBuilder.Build(
            new[] { quiet, loud },
            Array.Empty<UserChatProfile>(),
            Array.Empty<ChatContextMessage>(),
            botUserId: 999);

        Assert.Equal(new[] { "Loud", "Quiet" }, roster.Select(e => e.DisplayName));
        Assert.Equal(12, roster[0].Karma);
        Assert.Equal(1, roster[0].Warns);
        Assert.Equal(50, roster[0].TotalMessages);
        Assert.Equal(4, roster[0].TotalBadWords);
    }

    [Fact]
    public void Build_CapsRosterAtMaxEntries()
    {
        var stats = Enumerable.Range(1, RosterBuilder.MaxEntries + 20)
            .Select(i => Stats(i, $"user{i}", totalMessages: i))
            .ToList();

        var roster = RosterBuilder.Build(stats, Array.Empty<UserChatProfile>(), Array.Empty<ChatContextMessage>(), botUserId: 999);

        Assert.Equal(RosterBuilder.MaxEntries, roster.Count);
        Assert.Equal($"user{RosterBuilder.MaxEntries + 20}", roster[0].DisplayName);
    }
}
