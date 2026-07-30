using System.Text.Json.Nodes;
using GenerativeAI.Types;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.Ai;
using RudeBot.Services.UserProfileService;

namespace RudeBot.Tests.Ai;

public class ChatRagToolTests
{
    private const long ChatId = 100;

    private readonly IChatMessageRepository _messages = Substitute.For<IChatMessageRepository>();
    private readonly IUserProfileService _profiles = Substitute.For<IUserProfileService>();

    private static readonly RosterEntry[] Roster =
    {
        new()
        {
            UserId = 7, DisplayName = "John Doe", Aliases = new[] { "johndoe" },
            Karma = 12, Warns = 1, TotalMessages = 340, TotalBadWords = 25,
        },
        new() { UserId = 8, DisplayName = "Petro Petrenko" },
        new() { UserId = 9, DisplayName = "Petro Ivanenko", Aliases = new[] { "petya" } },
    };

    private ChatRagTool CreateTool(int callBudget = 6) =>
        new(ChatId, Roster, _messages, _profiles, NullLogger.Instance, callBudget);

    private static FunctionCall Call(string name, params (string Key, object Value)[] args)
    {
        var node = new JsonObject();

        foreach (var (key, value) in args)
        {
            node[key] = value switch
            {
                string s => JsonValue.Create(s),
                int i => JsonValue.Create(i),
                _ => JsonValue.Create(value),
            };
        }

        return new FunctionCall { Id = "call-1", Name = name, Args = node };
    }

    private static string Field(FunctionResponse response, string name) =>
        response.Response?[name]?.ToString() ?? "";

    [Fact]
    public void AsTool_DeclaresAllFourFunctions()
    {
        var declarations = CreateTool().AsTool().FunctionDeclarations;

        Assert.NotNull(declarations);
        Assert.Equal(
            new[] { "get_user_messages", "get_user_profile", "get_user_stats", "search_messages" },
            declarations!.Select(d => d.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(declarations, d => Assert.False(string.IsNullOrWhiteSpace(d.Description)));
        Assert.All(declarations, d => Assert.NotNull(d.Parameters));
    }

    [Theory]
    [InlineData("get_user_profile", true)]
    [InlineData("get_user_messages", true)]
    [InlineData("get_user_stats", true)]
    [InlineData("search_messages", true)]
    [InlineData("drop_database", false)]
    public void IsContainFunction_RecognisesOnlyOwnFunctions(string name, bool expected)
    {
        Assert.Equal(expected, CreateTool().IsContainFunction(name));
    }

    [Fact]
    public async Task CallAsync_ReturnsProfileForResolvedUser()
    {
        _profiles.GetAsync(ChatId, 7).Returns(new UserChatProfile
        {
            ChatId = ChatId, UserId = 7, UserName = "johndoe",
            Profile = "Любить теслу", UpdatedAt = new DateTime(2026, 7, 1, 10, 30, 0, DateTimeKind.Utc),
        });

        var response = await CreateTool().CallAsync(Call("get_user_profile", ("user_name", "@johndoe")));

        Assert.Equal("John Doe", Field(response, "user_name"));
        Assert.Equal("Любить теслу", Field(response, "profile"));
        Assert.Equal("call-1", response.Id);
    }

    [Fact]
    public async Task CallAsync_ReturnsNoProfileWhenDigestHasNotBuiltOneYet()
    {
        _profiles.GetAsync(ChatId, 7).Returns((UserChatProfile?)null);

        var response = await CreateTool().CallAsync(Call("get_user_profile", ("user_name", "johndoe")));

        Assert.Equal("no_profile", Field(response, "error"));
    }

    [Fact]
    public async Task CallAsync_ReturnsStatsFromRosterWithoutTouchingDatabase()
    {
        var response = await CreateTool().CallAsync(Call("get_user_stats", ("user_name", "John Doe")));

        Assert.Equal("12", Field(response, "karma"));
        Assert.Equal("1", Field(response, "warns"));
        Assert.Equal("340", Field(response, "total_messages"));
        Assert.Equal("25", Field(response, "total_bad_words"));
        await _profiles.DidNotReceive().GetAsync(Arg.Any<long>(), Arg.Any<long>());
    }

    [Fact]
    public async Task CallAsync_ReturnsCandidatesForAmbiguousName()
    {
        var response = await CreateTool().CallAsync(Call("get_user_stats", ("user_name", "petro")));

        Assert.Equal("ambiguous", Field(response, "error"));
        Assert.Equal(2, response.Response?["candidates"]?.AsArray().Count);
    }

    [Fact]
    public async Task CallAsync_ReturnsKnownUsersForUnknownName()
    {
        var response = await CreateTool().CallAsync(Call("get_user_stats", ("user_name", "хтось")));

        Assert.Equal("not_found", Field(response, "error"));
        Assert.Equal(3, response.Response?["known_users"]?.AsArray().Count);
    }

    [Fact]
    public async Task CallAsync_ReturnsUnknownFunctionForUnexpectedName()
    {
        var response = await CreateTool().CallAsync(Call("drop_database"));

        Assert.Equal("unknown_function", Field(response, "error"));
    }

    [Fact]
    public async Task CallAsync_StopsServingDataOnceBudgetIsSpent()
    {
        var tool = CreateTool(callBudget: 2);
        var call = Call("get_user_stats", ("user_name", "John Doe"));

        Assert.Equal("", Field(await tool.CallAsync(call), "error"));
        Assert.Equal("", Field(await tool.CallAsync(call), "error"));
        Assert.Equal("call_budget_exceeded", Field(await tool.CallAsync(call), "error"));
    }

    [Fact]
    public async Task CallAsync_EnforcesBudgetAtomicallyUnderConcurrency()
    {
        var tool = CreateTool(callBudget: 3);
        var call = Call("get_user_stats", ("user_name", "John Doe"));

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => tool.CallAsync(call)));

        var succeeded = responses.Count(r => Field(r, "error") == "");
        var exceeded = responses.Count(r => Field(r, "error") == "call_budget_exceeded");

        Assert.Equal(3, succeeded);
        Assert.Equal(7, exceeded);
    }

    [Fact]
    public async Task CallAsync_SwallowsRepositoryFailures()
    {
        _profiles.GetAsync(ChatId, 7).Throws(new InvalidOperationException("db down"));

        var response = await CreateTool().CallAsync(Call("get_user_profile", ("user_name", "johndoe")));

        Assert.Equal("internal", Field(response, "error"));
    }

    [Fact]
    public async Task CallAsync_HandlesMissingArguments()
    {
        var response = await CreateTool().CallAsync(new FunctionCall { Id = "x", Name = "get_user_stats", Args = null });

        Assert.Equal("not_found", Field(response, "error"));
    }

    private static ChatMessage Msg(long userId, string userName, string text, DateTime createdAt) =>
        new() { ChatId = ChatId, UserId = userId, UserName = userName, Text = text, CreatedAt = createdAt };

    [Fact]
    public async Task CallAsync_ReturnsUserMessagesWithDefaultCount()
    {
        var t0 = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        _messages.GetLastNByUserAsync(ChatId, 7, ChatRagTool.DefaultUserMessages, Arg.Any<bool>())
            .Returns(new List<ChatMessage> { Msg(7, "johndoe", "привіт", t0) });

        var response = await CreateTool().CallAsync(Call("get_user_messages", ("user_name", "johndoe")));

        Assert.Equal("John Doe", Field(response, "user_name"));
        var messages = response.Response?["messages"]?.AsArray();
        Assert.Single(messages!);
        Assert.Equal("привіт", messages![0]?["text"]?.ToString());
        Assert.Equal("2026-07-01 12:00", messages[0]?["created_at"]?.ToString());
    }

    [Fact]
    public async Task CallAsync_ClampsUserMessageCount()
    {
        _messages.GetLastNByUserAsync(ChatId, 7, Arg.Any<int>(), Arg.Any<bool>()).Returns(new List<ChatMessage>());
        var tool = CreateTool();

        await tool.CallAsync(Call("get_user_messages", ("user_name", "johndoe"), ("count", 999)));
        await tool.CallAsync(Call("get_user_messages", ("user_name", "johndoe"), ("count", 0)));

        await _messages.Received(1).GetLastNByUserAsync(ChatId, 7, ChatRagTool.MaxUserMessages, Arg.Any<bool>());
        await _messages.Received(1).GetLastNByUserAsync(ChatId, 7, 1, Arg.Any<bool>());
    }

    [Fact]
    public async Task CallAsync_TruncatesLongMessageText()
    {
        var longText = new string('я', ChatRagTool.MaxTextLength + 50);
        _messages.GetLastNByUserAsync(ChatId, 7, Arg.Any<int>(), Arg.Any<bool>())
            .Returns(new List<ChatMessage> { Msg(7, "johndoe", longText, DateTime.UtcNow) });

        var response = await CreateTool().CallAsync(Call("get_user_messages", ("user_name", "johndoe")));

        var text = response.Response?["messages"]?.AsArray()[0]?["text"]?.ToString();
        Assert.Equal(ChatRagTool.MaxTextLength + 1, text!.Length);
        Assert.EndsWith("…", text);
    }

    [Fact]
    public async Task CallAsync_SearchesMessagesAndEchoesQuery()
    {
        var t0 = new DateTime(2026, 7, 1, 9, 15, 0, DateTimeKind.Utc);
        _messages.SearchAsync(ChatId, Arg.Is<string>(q => q == "тесла"), ChatRagTool.DefaultSearchResults, Arg.Any<bool>())
            .Returns(new List<ChatMessage> { Msg(8, "petro", "тесла зарядилась", t0) });

        var response = await CreateTool().CallAsync(Call("search_messages", ("query", "тесла")));

        Assert.Equal("тесла", Field(response, "query"));
        var messages = response.Response?["messages"]?.AsArray();
        Assert.Equal("Petro Petrenko", messages![0]?["user_name"]?.ToString());
        Assert.Equal("тесла зарядилась", messages[0]?["text"]?.ToString());
    }

    [Fact]
    public async Task CallAsync_SearchFallsBackToRawHandleForUserNotInRoster()
    {
        var t0 = DateTime.UtcNow;
        _messages.SearchAsync(ChatId, "тесла", ChatRagTool.DefaultSearchResults)
            .Returns(new List<ChatMessage> { Msg(999999, "colduser", "стара тесла-згадка", t0) });

        var response = await CreateTool().CallAsync(Call("search_messages", ("query", "тесла")));

        var messages = response.Response?["messages"]?.AsArray();
        Assert.Equal("colduser", messages![0]?["user_name"]?.ToString());
    }

    [Fact]
    public async Task CallAsync_ClampsSearchCount()
    {
        _messages.SearchAsync(ChatId, Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>()).Returns(new List<ChatMessage>());

        await CreateTool().CallAsync(Call("search_messages", ("query", "тесла"), ("count", 999)));

        await _messages.Received(1).SearchAsync(ChatId, Arg.Is<string>(q => q == "тесла"), ChatRagTool.MaxSearchResults, Arg.Any<bool>());
    }

    [Fact]
    public async Task CallAsync_RejectsEmptySearchQuery()
    {
        var response = await CreateTool().CallAsync(Call("search_messages", ("query", "   ")));

        Assert.Equal("empty_query", Field(response, "error"));
        await _messages.DidNotReceive().SearchAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CallAsync_NeverReachesOutsideItsOwnChat()
    {
        _messages.GetLastNByUserAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<int>()).Returns(new List<ChatMessage>());
        _messages.SearchAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<int>()).Returns(new List<ChatMessage>());
        var tool = CreateTool();

        await tool.CallAsync(Call("get_user_messages", ("user_name", "johndoe")));
        await tool.CallAsync(Call("search_messages", ("query", "тесла")));

        await _messages.DidNotReceive().GetLastNByUserAsync(Arg.Is<long>(id => id != ChatId), Arg.Any<long>(), Arg.Any<int>());
        await _messages.DidNotReceive().SearchAsync(Arg.Is<long>(id => id != ChatId), Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CallAsync_HandlesQueryWithBackslashAndNewline()
    {
        _messages.SearchAsync(ChatId, Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>())
            .Returns(new List<ChatMessage>());

        var response = await CreateTool().CallAsync(Call("search_messages", ("query", "C:\\Users\\foo\ntest")));

        Assert.Equal("", Field(response, "error"));
        Assert.Equal("C:\\Users\\foo\ntest", Field(response, "query"));
    }
}
