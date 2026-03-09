using Cron.NET;
using NSubstitute;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.ChatDigestService;
using Telegram.Bot;
using Telegram.Bot.Requests;

namespace RudeBot.Tests;

public class ChatDigestBackgroundServiceUnitTests
{
    private readonly IChatDigestService _chatDigestService;
    private readonly IChatSettingsService _chatSettingsService;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatDigestSummaryGenerator _summaryGenerator;
    private readonly CronDaemon _cronDaemon;

    public ChatDigestBackgroundServiceUnitTests()
    {
        _chatDigestService = Substitute.For<IChatDigestService>();
        _chatSettingsService = Substitute.For<IChatSettingsService>();
        _botClient = Substitute.For<ITelegramBotClient>();
        _summaryGenerator = Substitute.For<IChatDigestSummaryGenerator>();
        _cronDaemon = Substitute.For<CronDaemon>();
    }

    private ChatDigestBackgroundService CreateService()
    {
        return new ChatDigestBackgroundService(
            _chatDigestService,
            _chatSettingsService,
            _botClient,
            _summaryGenerator,
            _cronDaemon);
    }

    [Fact]
    public async Task ProcessAllChats_WithEnabledChat_ShouldGenerateAndSendSummary()
    {
        // Arrange
        var messages = new List<ChatDigestMessage>
        {
            new("user1", "hello", DateTime.UtcNow),
            new("user2", "world", DateTime.UtcNow)
        };

        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1 });
        _chatSettingsService.GetChatSettings(1)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = true }));
        _chatDigestService.GetAndClearMessages(1).Returns(messages);
        _summaryGenerator.GenerateSummary(messages).Returns(Task.FromResult("Summary text"));

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        await _botClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.Text == "Summary text"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAllChats_WithDisabledChat_ShouldClearCacheAndNotSend()
    {
        // Arrange
        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1 });
        _chatSettingsService.GetChatSettings(1)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = false }));

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        _chatDigestService.Received(1).GetAndClearMessages(1);
        await _summaryGenerator.DidNotReceive().GenerateSummary(Arg.Any<List<ChatDigestMessage>>());
        await _botClient.DidNotReceive().SendRequest(
            Arg.Any<SendMessageRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAllChats_WithNullSettings_ShouldClearCacheAndNotSend()
    {
        // Arrange
        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1 });
        _chatSettingsService.GetChatSettings(1)
            .Returns(Task.FromResult<ChatSettings>(null));

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        _chatDigestService.Received(1).GetAndClearMessages(1);
        await _summaryGenerator.DidNotReceive().GenerateSummary(Arg.Any<List<ChatDigestMessage>>());
    }

    [Fact]
    public async Task ProcessAllChats_WithEmptyMessages_ShouldNotGenerateOrSend()
    {
        // Arrange
        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1 });
        _chatSettingsService.GetChatSettings(1)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = true }));
        _chatDigestService.GetAndClearMessages(1).Returns(new List<ChatDigestMessage>());

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        await _summaryGenerator.DidNotReceive().GenerateSummary(Arg.Any<List<ChatDigestMessage>>());
        await _botClient.DidNotReceive().SendRequest(
            Arg.Any<SendMessageRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAllChats_WithEmptySummary_ShouldNotSendMessage()
    {
        // Arrange
        var messages = new List<ChatDigestMessage>
        {
            new("user1", "hello", DateTime.UtcNow)
        };

        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1 });
        _chatSettingsService.GetChatSettings(1)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = true }));
        _chatDigestService.GetAndClearMessages(1).Returns(messages);
        _summaryGenerator.GenerateSummary(messages).Returns(Task.FromResult(string.Empty));

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        await _botClient.DidNotReceive().SendRequest(
            Arg.Any<SendMessageRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAllChats_WithNoActiveChats_ShouldDoNothing()
    {
        // Arrange
        _chatDigestService.GetActiveChatIds().Returns(new List<long>());

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        await _chatSettingsService.DidNotReceive().GetChatSettings(Arg.Any<long>());
        await _summaryGenerator.DidNotReceive().GenerateSummary(Arg.Any<List<ChatDigestMessage>>());
    }

    [Fact]
    public async Task ProcessAllChats_WithMultipleChats_ShouldProcessEachIndependently()
    {
        // Arrange
        var messages1 = new List<ChatDigestMessage> { new("user1", "msg1", DateTime.UtcNow) };
        var messages2 = new List<ChatDigestMessage> { new("user2", "msg2", DateTime.UtcNow) };

        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1, 2 });

        _chatSettingsService.GetChatSettings(1)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = true }));
        _chatSettingsService.GetChatSettings(2)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = true }));

        _chatDigestService.GetAndClearMessages(1).Returns(messages1);
        _chatDigestService.GetAndClearMessages(2).Returns(messages2);

        _summaryGenerator.GenerateSummary(messages1).Returns(Task.FromResult("Summary 1"));
        _summaryGenerator.GenerateSummary(messages2).Returns(Task.FromResult("Summary 2"));

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        await _botClient.Received(2).SendRequest(
            Arg.Any<SendMessageRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAllChats_WhenOneChatFails_ShouldContinueProcessingOthers()
    {
        // Arrange
        var messages2 = new List<ChatDigestMessage> { new("user2", "msg2", DateTime.UtcNow) };

        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1, 2 });

        _chatSettingsService.GetChatSettings(1)
            .Returns<Task<ChatSettings>>(x => throw new Exception("DB error"));
        _chatSettingsService.GetChatSettings(2)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = true }));

        _chatDigestService.GetAndClearMessages(2).Returns(messages2);
        _summaryGenerator.GenerateSummary(messages2).Returns(Task.FromResult("Summary 2"));

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        await _botClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.Text == "Summary 2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAllChats_MixedEnabledAndDisabled_ShouldOnlySendToEnabled()
    {
        // Arrange
        var messages1 = new List<ChatDigestMessage> { new("user1", "msg1", DateTime.UtcNow) };

        _chatDigestService.GetActiveChatIds().Returns(new List<long> { 1, 2 });

        _chatSettingsService.GetChatSettings(1)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = true }));
        _chatSettingsService.GetChatSettings(2)
            .Returns(Task.FromResult(new ChatSettings { SummarizeMessages = false }));

        _chatDigestService.GetAndClearMessages(1).Returns(messages1);

        _summaryGenerator.GenerateSummary(messages1).Returns(Task.FromResult("Summary 1"));

        var service = CreateService();

        // Act
        await service.ProcessAllChats();

        // Assert
        await _botClient.Received(1).SendRequest(
            Arg.Any<SendMessageRequest>(),
            Arg.Any<CancellationToken>());
        // Disabled chat should still have cache cleared
        _chatDigestService.Received(1).GetAndClearMessages(2);
    }
}
