using RudeBot.Domain.Resources;
using RudeBot.Services.Ai;
using RudeBot.Services.ChatContextService;

namespace RudeBot.Tests.Ai;

public class AiPromptBuilderTests
{
    private static readonly RosterEntry[] Roster =
    {
        new() { UserId = 7, DisplayName = "John Doe", Aliases = new[] { "johndoe" } },
        new() { UserId = 8, DisplayName = "Petro", Aliases = Array.Empty<string>() },
    };

    private static string Build(
        long userId = 7,
        string message = "як справи?",
        IReadOnlyList<ChatContextMessage>? context = null,
        IReadOnlyList<RosterEntry>? roster = null,
        long? creatorId = null) =>
        AiPromptBuilder.Build(
            userId,
            "johndoe",
            message,
            context ?? Array.Empty<ChatContextMessage>(),
            roster ?? Roster,
            botUserId: 999,
            creatorId: creatorId);

    [Fact]
    public void Build_RendersRosterWithAliases()
    {
        var prompt = Build();

        Assert.Contains("John Doe (johndoe)", prompt);
        Assert.Contains("Petro", prompt);
        Assert.DoesNotContain("Petro (", prompt);
    }

    [Fact]
    public void Build_TellsModelToCallFunctionsInsteadOfGuessing()
    {
        Assert.Contains("виклич відповідну функцію", Build());
    }

    [Fact]
    public void Build_OmitsRosterSectionWhenRosterIsEmpty()
    {
        var prompt = Build(roster: Array.Empty<RosterEntry>());

        Assert.DoesNotContain("Учасники цього чату", prompt);
    }

    [Fact]
    public void Build_UsesCreatorPromptOnlyForCreator()
    {
        Assert.StartsWith(Resources.AiPromptCreator, Build(userId: 7, creatorId: 7));
        Assert.StartsWith(Resources.AiPrompt, Build(userId: 7, creatorId: 8));
        Assert.StartsWith(Resources.AiPrompt, Build(userId: 7, creatorId: null));
    }

    [Fact]
    public void Build_MarksBotMessagesInContext()
    {
        var context = new[]
        {
            new ChatContextMessage("johndoe", "привіт", 7),
            new ChatContextMessage("RudeBot", "мяу", 999),
        };

        var prompt = Build(context: context);

        Assert.Contains("johndoe: привіт", prompt);
        Assert.Contains("[твоя відповідь] RudeBot: мяу", prompt);
    }

    [Fact]
    public void Build_PutsCurrentMessageLast()
    {
        var prompt = Build(message: "що там у Петра?");

        Assert.EndsWith("Повідомлення від johndoe:\nщо там у Петра?", prompt);
    }
}
