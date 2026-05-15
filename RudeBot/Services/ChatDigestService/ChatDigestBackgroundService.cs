using Autofac;
using Cron.NET;
using Microsoft.Extensions.Logging;

namespace RudeBot.Services.ChatDigestService;

public class ChatDigestBackgroundService : IStartable
{
    private readonly IChatSettingsService _chatSettingsService;
    private readonly IChatDigestRunner _runner;
    private readonly CronDaemon _cronDaemon;
    private readonly ILogger<ChatDigestBackgroundService> _logger;

    public ChatDigestBackgroundService(
        IChatSettingsService chatSettingsService,
        IChatDigestRunner runner,
        CronDaemon cronDaemon,
        ILogger<ChatDigestBackgroundService> logger)
    {
        _chatSettingsService = chatSettingsService;
        _runner = runner;
        _cronDaemon = cronDaemon;
        _logger = logger;
    }

    public void Start()
    {
        _cronDaemon.AddJob(ChatDigestConsts.CronExpression, () =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await ProcessAllChats();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChatDigest cron job failed");
                }
            });
        });

        _cronDaemon.Start();
        _logger.LogInformation("ChatDigest cron scheduled ({CronExpression})", ChatDigestConsts.CronExpression);
    }

    public void Stop() => _cronDaemon.Stop();

    public async Task ProcessAllChats()
    {
        var chatIds = await _chatSettingsService.GetChatIdsWithSummarizeEnabled();

        foreach (var chatId in chatIds)
        {
            try
            {
                await _runner.RunForChat(chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatDigest failed for chat {ChatId}", chatId);
            }
        }
    }
}
