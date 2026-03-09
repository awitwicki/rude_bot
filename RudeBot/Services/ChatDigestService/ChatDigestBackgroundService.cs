using Autofac;
using Cron.NET;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RudeBot.Services.ChatDigestService;

public class ChatDigestBackgroundService : IStartable
{
    private readonly IChatDigestService _chatDigestService;
    private readonly IChatSettingsService _chatSettingsService;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatDigestSummaryGenerator _summaryGenerator;
    private readonly CronDaemon _cronDaemon;

    public ChatDigestBackgroundService(
        IChatDigestService chatDigestService,
        IChatSettingsService chatSettingsService,
        ITelegramBotClient botClient,
        IChatDigestSummaryGenerator summaryGenerator,
        CronDaemon cronDaemon)
    {
        _chatDigestService = chatDigestService;
        _chatSettingsService = chatSettingsService;
        _botClient = botClient;
        _summaryGenerator = summaryGenerator;
        _cronDaemon = cronDaemon;
    }

    public void Start()
    {
        //_cronDaemon.AddJob("0 0,12 * * *", () =>
        _cronDaemon.AddJob("0 * * * *", () =>
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
        Console.WriteLine("ChatDigest: cron scheduled for 00:00 and 12:00 UTC");
    }

    public void Stop()
    {
        _cronDaemon.Stop();
    }

    public async Task ProcessAllChats()
    {
        var activeChatIds = _chatDigestService.GetActiveChatIds();

        foreach (var chatId in activeChatIds)
        {
            try
            {
                var settings = await _chatSettingsService.GetChatSettings(chatId);
                if (settings == null || !settings.SummarizeMessages)
                {
                    // Clear cache to prevent memory growth
                    _chatDigestService.GetAndClearMessages(chatId);
                    continue;
                }

                var messages = _chatDigestService.GetAndClearMessages(chatId);
                if (messages.Count == 0)
                    continue;

                var summary = await _summaryGenerator.GenerateSummary(messages);
                if (!string.IsNullOrEmpty(summary))
                {
                    await _botClient.SendMessage(chatId: chatId, text: summary, parseMode: ParseMode.MarkdownV2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChatDigest error for chat {chatId}: {ex}");
            }
        }
    }
}
