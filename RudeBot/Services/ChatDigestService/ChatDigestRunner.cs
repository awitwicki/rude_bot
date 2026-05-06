using Autofac;
using Telegram.Bot;

namespace RudeBot.Services.ChatDigestService;

public class ChatDigestRunner : IChatDigestRunner
{
    private readonly ILifetimeScope _rootScope;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatDigestSummaryGenerator _summaryGenerator;

    public ChatDigestRunner(
        ILifetimeScope rootScope,
        ITelegramBotClient botClient,
        IChatDigestSummaryGenerator summaryGenerator)
    {
        _rootScope = rootScope;
        _botClient = botClient;
        _summaryGenerator = summaryGenerator;
    }

    public async Task<ChatDigestResult> RunForChat(long chatId)
    {
        var since = DateTime.UtcNow - ChatDigestConsts.Interval;

        await using var scope = _rootScope.BeginLifetimeScope();
        var repo = scope.Resolve<IChatMessageRepository>();

        var messages = await repo.GetSinceAsync(chatId, since);
        if (messages.Count == 0) return ChatDigestResult.Empty;

        var summary = await _summaryGenerator.GenerateSummary(messages);
        if (string.IsNullOrEmpty(summary)) return ChatDigestResult.GenerationFailed;

        var sent = await _botClient.SendMessage(
            chatId: chatId,
            text: summary);
        await repo.PersistBotSentAsync(sent, summary);

        return ChatDigestResult.Posted;
    }
}
