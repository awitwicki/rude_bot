using RudeBot.Domain.Resources;
using RudeBot.Models;
using RudeBot.Services.Ai;

namespace RudeBot.Tests.Ai;

public class RoastPromptBuilderTests
{
    [Fact]
    public void Build_IncludesPromptProfileStatsAndMessages()
    {
        var profile = new UserChatProfile { Profile = "любить теслу" };
        var stats = new UserChatStats { Karma = 5, TotalMessages = 100, TotalBadWords = 42, Warns = 1 };
        var messages = new List<ChatMessage> { new() { Text = "тесла топ" } };

        var prompt = RoastPromptBuilder.Build("vasya", profile, stats, messages);

        Assert.StartsWith(Resources.RoastMePrompt, prompt);
        Assert.Contains("vasya", prompt);
        Assert.Contains("любить теслу", prompt);
        Assert.Contains("матюків 42", prompt);
        Assert.Contains("- тесла топ", prompt);
    }

    [Fact]
    public void Build_NoData_StillHasPromptAndName()
    {
        var prompt = RoastPromptBuilder.Build("vasya", null, null, new List<ChatMessage>());

        Assert.StartsWith(Resources.RoastMePrompt, prompt);
        Assert.Contains("vasya", prompt);
        Assert.DoesNotContain("статистика", prompt);
    }

    [Fact]
    public void Build_TruncatesLongMessages()
    {
        var messages = new List<ChatMessage> { new() { Text = new string('a', 1000) } };

        var prompt = RoastPromptBuilder.Build("vasya", null, null, messages);

        Assert.DoesNotContain(new string('a', RoastPromptBuilder.MaxMessageLength + 1), prompt);
    }
}
