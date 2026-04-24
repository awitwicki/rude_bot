using Autofac;
using Cron.NET;

namespace RudeBot.Services.ChatDigestService;

public class ChatDigestBackgroundService : IStartable
{
    private readonly IChatSettingsService _chatSettingsService;
    private readonly IChatDigestRunner _runner;
    private readonly CronDaemon _cronDaemon;

    public ChatDigestBackgroundService(
        IChatSettingsService chatSettingsService,
        IChatDigestRunner runner,
        CronDaemon cronDaemon)
    {
        _chatSettingsService = chatSettingsService;
        _runner = runner;
        _cronDaemon = cronDaemon;
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
                    Console.WriteLine($"ChatDigest error: {ex}");
                }
            });
        });

        _cronDaemon.Start();
        Console.WriteLine($"ChatDigest: cron scheduled ({ChatDigestConsts.CronExpression})");
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
                Console.WriteLine($"ChatDigest error for chat {chatId}: {ex}");
            }
        }
    }
}
