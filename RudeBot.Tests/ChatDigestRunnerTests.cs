using Autofac;
using NSubstitute;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.ChatDigestService;
using Telegram.Bot;
using Telegram.Bot.Requests;

namespace RudeBot.Tests;

public class ChatDigestRunnerTests
{
    private readonly IChatMessageRepository _repo;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatDigestSummaryGenerator _summaryGenerator;
    private readonly ILifetimeScope _rootScope;

    public ChatDigestRunnerTests()
    {
        _repo = Substitute.For<IChatMessageRepository>();
        _botClient = Substitute.For<ITelegramBotClient>();
        _summaryGenerator = Substitute.For<IChatDigestSummaryGenerator>();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(_repo).As<IChatMessageRepository>();
        _rootScope = builder.Build();
    }

    private ChatDigestRunner CreateRunner() =>
        new(_rootScope, _botClient, _summaryGenerator);

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
}
