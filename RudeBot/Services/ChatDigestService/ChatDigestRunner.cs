using Autofac;
using Microsoft.Extensions.Logging;
using RudeBot.Domain;
using RudeBot.Models;
using RudeBot.Services.UserProfileService;
using Telegram.Bot;

namespace RudeBot.Services.ChatDigestService;

public class ChatDigestRunner : IChatDigestRunner
{
    private readonly ILifetimeScope _rootScope;
    private readonly ITelegramBotClient _botClient;
    private readonly IChatDigestSummaryGenerator _summaryGenerator;
    private readonly ILogger<ChatDigestRunner> _logger;

    public ChatDigestRunner(
        ILifetimeScope rootScope,
        ITelegramBotClient botClient,
        IChatDigestSummaryGenerator summaryGenerator,
        ILogger<ChatDigestRunner> logger)
    {
        _rootScope = rootScope;
        _botClient = botClient;
        _summaryGenerator = summaryGenerator;
        _logger = logger;
    }

    public async Task<ChatDigestResult> RunForChat(long chatId)
    {
        var since = DateTime.UtcNow - ChatDigestConsts.Interval;

        await using var scope = _rootScope.BeginLifetimeScope();
        var repo = scope.Resolve<IChatMessageRepository>();
        var profileService = scope.Resolve<IUserProfileService>();
        var profileMerger = scope.Resolve<IUserProfileMerger>();

        var messages = await repo.GetSinceAsync(chatId, since);
        if (messages.Count == 0) return ChatDigestResult.Empty;

        var summary = await _summaryGenerator.GenerateSummary(messages);
        if (string.IsNullOrEmpty(summary)) return ChatDigestResult.GenerationFailed;

        var sent = await _botClient.SendMessage(
            chatId: chatId,
            text: summary);
        await repo.PersistBotSentAsync(sent, summary);

        try
        {
            await UpdateProfilesForActiveUsers(chatId, messages, profileService, profileMerger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatDigestRunner profile stage failed for chat {ChatId}", chatId);
        }

        return ChatDigestResult.Posted;
    }

    private async Task UpdateProfilesForActiveUsers(
        long chatId,
        List<ChatMessage> messages,
        IUserProfileService profileService,
        IUserProfileMerger profileMerger)
    {
        var grouped = messages
            .Where(m => m.UserId != 0
                        && m.UserId != Consts.BotUserId
                        && !string.IsNullOrWhiteSpace(m.Text))
            .GroupBy(m => m.UserId);

        foreach (var group in grouped)
        {
            var userMessages = group.ToList();
            var latest = userMessages.OrderByDescending(m => m.CreatedAt).First();
            var userName = latest.UserName;

            try
            {
                var existing = await profileService.GetAsync(chatId, group.Key);
                var merged = await profileMerger.MergeAsync(existing?.Profile ?? "", userName, userMessages);

                if (string.IsNullOrWhiteSpace(merged)) continue;
                if (existing != null && merged == existing.Profile) continue;

                await profileService.UpsertAsync(chatId, group.Key, userName, merged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatDigestRunner profile update failed for chat {ChatId} user {UserId}", chatId, group.Key);
            }
        }
    }
}
