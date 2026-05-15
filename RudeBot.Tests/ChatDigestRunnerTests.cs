using Autofac;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RudeBot.Domain;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.ChatDigestService;
using RudeBot.Services.UserProfileService;
using Telegram.Bot;
using Telegram.Bot.Requests;

namespace RudeBot.Tests;

public class ChatDigestRunnerTests
{
    private readonly IChatMessageRepository _repo;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatDigestSummaryGenerator _summaryGenerator;
    private readonly IUserProfileService _profileService;
    private readonly IUserProfileMerger _profileMerger;
    private readonly ILifetimeScope _rootScope;
    private readonly ILogger<ChatDigestRunner> _logger;

    public ChatDigestRunnerTests()
    {
        _repo = Substitute.For<IChatMessageRepository>();
        _botClient = Substitute.For<ITelegramBotClient>();
        _summaryGenerator = Substitute.For<IChatDigestSummaryGenerator>();
        _profileService = Substitute.For<IUserProfileService>();
        _profileMerger = Substitute.For<IUserProfileMerger>();
        _logger = Substitute.For<ILogger<ChatDigestRunner>>();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(_repo).As<IChatMessageRepository>();
        builder.RegisterInstance(_profileService).As<IUserProfileService>();
        builder.RegisterInstance(_profileMerger).As<IUserProfileMerger>();
        _rootScope = builder.Build();
    }

    private ChatDigestRunner CreateRunner() =>
        new(_rootScope, _botClient, _summaryGenerator, _logger);

    private static List<ChatMessage> Msgs(params string[] texts) =>
        texts.Select(t => new ChatMessage
        {
            UserName = "u",
            Text = t,
            CreatedAt = DateTime.UtcNow
        }).ToList();

    [Fact]
    public async Task RunForChat_WithNoMessages_ReturnsEmpty()
    {
        _repo.GetSinceAsync(1, Arg.Any<DateTime>()).Returns(new List<ChatMessage>());

        var result = await CreateRunner().RunForChat(1);

        Assert.Equal(ChatDigestResult.Empty, result);
        await _summaryGenerator.DidNotReceive().GenerateSummary(Arg.Any<List<ChatMessage>>());
        await _botClient.DidNotReceive().SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunForChat_WithEmptySummary_ReturnsGenerationFailed()
    {
        var messages = Msgs("hello");
        _repo.GetSinceAsync(1, Arg.Any<DateTime>()).Returns(messages);
        _summaryGenerator.GenerateSummary(messages).Returns(string.Empty);

        var result = await CreateRunner().RunForChat(1);

        Assert.Equal(ChatDigestResult.GenerationFailed, result);
        await _botClient.DidNotReceive().SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunForChat_WithMessagesAndSummary_PostsAndReturnsPosted()
    {
        var messages = Msgs("hello", "world");
        _repo.GetSinceAsync(1, Arg.Any<DateTime>()).Returns(messages);
        _summaryGenerator.GenerateSummary(messages).Returns("Summary text");

        var result = await CreateRunner().RunForChat(1);

        Assert.Equal(ChatDigestResult.Posted, result);
        await _botClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.Text == "Summary text"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunForChat_AfterSummary_UpdatesProfilesForEachActiveUserAndSkipsBot()
    {
        var messages = new List<ChatMessage>
        {
            new() { ChatId = 1, UserId = 10, UserName = "alice", Text = "got a dog", CreatedAt = DateTime.UtcNow },
            new() { ChatId = 1, UserId = 10, UserName = "alice", Text = "and a cat", CreatedAt = DateTime.UtcNow },
            new() { ChatId = 1, UserId = 20, UserName = "bob",   Text = "hello",     CreatedAt = DateTime.UtcNow },
            new() { ChatId = 1, UserId = Consts.BotUserId, UserName = "rude кіт", Text = "summary", CreatedAt = DateTime.UtcNow },
            new() { ChatId = 1, UserId = 30, UserName = "carol", Text = "   ",      CreatedAt = DateTime.UtcNow },
        };
        _repo.GetSinceAsync(1, Arg.Any<DateTime>()).Returns(messages);
        _summaryGenerator.GenerateSummary(messages).Returns("digest");
        _profileService.GetAsync(1, Arg.Any<long>()).Returns((UserChatProfile?)null);
        _profileMerger.MergeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<List<ChatMessage>>())
            .Returns(call => $"profile of {call.ArgAt<string>(1)}");

        var result = await CreateRunner().RunForChat(1);

        Assert.Equal(ChatDigestResult.Posted, result);

        // alice and bob get merged + upserted; bot and carol do not.
        await _profileMerger.Received(1).MergeAsync(Arg.Any<string>(), "alice", Arg.Is<List<ChatMessage>>(l => l.Count == 2));
        await _profileMerger.Received(1).MergeAsync(Arg.Any<string>(), "bob",   Arg.Is<List<ChatMessage>>(l => l.Count == 1));
        await _profileMerger.DidNotReceive().MergeAsync(Arg.Any<string>(), "rude кіт", Arg.Any<List<ChatMessage>>());
        await _profileMerger.DidNotReceive().MergeAsync(Arg.Any<string>(), "carol",    Arg.Any<List<ChatMessage>>());

        await _profileService.Received(1).UpsertAsync(1, 10, "alice", "profile of alice");
        await _profileService.Received(1).UpsertAsync(1, 20, "bob",   "profile of bob");
    }

    [Fact]
    public async Task RunForChat_OneUserMergeThrows_OthersStillProcessed_AndDigestStillPosted()
    {
        var messages = new List<ChatMessage>
        {
            new() { ChatId = 1, UserId = 10, UserName = "alice", Text = "boom",  CreatedAt = DateTime.UtcNow },
            new() { ChatId = 1, UserId = 20, UserName = "bob",   Text = "hello", CreatedAt = DateTime.UtcNow },
        };
        _repo.GetSinceAsync(1, Arg.Any<DateTime>()).Returns(messages);
        _summaryGenerator.GenerateSummary(messages).Returns("digest");
        _profileService.GetAsync(1, Arg.Any<long>()).Returns((UserChatProfile?)null);
        _profileMerger.MergeAsync(Arg.Any<string>(), "alice", Arg.Any<List<ChatMessage>>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("gemini down"));
        _profileMerger.MergeAsync(Arg.Any<string>(), "bob", Arg.Any<List<ChatMessage>>())
            .Returns("profile of bob");

        var result = await CreateRunner().RunForChat(1);

        Assert.Equal(ChatDigestResult.Posted, result);
        await _profileService.DidNotReceive().UpsertAsync(1, 10, Arg.Any<string>(), Arg.Any<string>());
        await _profileService.Received(1).UpsertAsync(1, 20, "bob", "profile of bob");
    }
}
