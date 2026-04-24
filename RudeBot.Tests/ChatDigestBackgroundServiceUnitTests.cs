using Cron.NET;
using NSubstitute;
using RudeBot.Services;
using RudeBot.Services.ChatDigestService;

namespace RudeBot.Tests;

public class ChatDigestBackgroundServiceUnitTests
{
    private readonly IChatSettingsService _chatSettingsService;
    private readonly IChatDigestRunner _runner;
    private readonly CronDaemon _cronDaemon;

    public ChatDigestBackgroundServiceUnitTests()
    {
        _chatSettingsService = Substitute.For<IChatSettingsService>();
        _runner = Substitute.For<IChatDigestRunner>();
        _cronDaemon = Substitute.For<CronDaemon>();
    }

    private ChatDigestBackgroundService CreateService() =>
        new(_chatSettingsService, _runner, _cronDaemon);

    [Fact]
    public async Task ProcessAllChats_WithEnabledChat_CallsRunner()
    {
        _chatSettingsService.GetChatIdsWithSummarizeEnabled().Returns(new List<long> { 1 });
        _runner.RunForChat(1).Returns(ChatDigestResult.Posted);

        await CreateService().ProcessAllChats();

        await _runner.Received(1).RunForChat(1);
    }

    [Fact]
    public async Task ProcessAllChats_WithNoEnabledChats_DoesNothing()
    {
        _chatSettingsService.GetChatIdsWithSummarizeEnabled().Returns(new List<long>());

        await CreateService().ProcessAllChats();

        await _runner.DidNotReceive().RunForChat(Arg.Any<long>());
    }

    [Fact]
    public async Task ProcessAllChats_WithMultipleChats_CallsRunnerForEach()
    {
        _chatSettingsService.GetChatIdsWithSummarizeEnabled().Returns(new List<long> { 1, 2 });
        _runner.RunForChat(Arg.Any<long>()).Returns(ChatDigestResult.Posted);

        await CreateService().ProcessAllChats();

        await _runner.Received(1).RunForChat(1);
        await _runner.Received(1).RunForChat(2);
    }

    [Fact]
    public async Task ProcessAllChats_WhenOneChatFails_ContinuesOthers()
    {
        _chatSettingsService.GetChatIdsWithSummarizeEnabled().Returns(new List<long> { 1, 2 });
        _runner.RunForChat(1).Returns<Task<ChatDigestResult>>(_ => throw new Exception("boom"));
        _runner.RunForChat(2).Returns(ChatDigestResult.Posted);

        await CreateService().ProcessAllChats();

        await _runner.Received(1).RunForChat(1);
        await _runner.Received(1).RunForChat(2);
    }
}
