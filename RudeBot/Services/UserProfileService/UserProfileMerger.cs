using GenerativeAI;
using Microsoft.Extensions.Logging;
using RudeBot.Domain.Resources;
using RudeBot.Models;

namespace RudeBot.Services.UserProfileService;

public class UserProfileMerger : IUserProfileMerger
{
    private readonly ILogger<UserProfileMerger> _logger;

    public UserProfileMerger(ILogger<UserProfileMerger> logger)
    {
        _logger = logger;
    }

    public async Task<string> MergeAsync(string existingProfile, string userName, List<ChatMessage> userMessages)
    {
        if (userMessages == null || userMessages.Count == 0)
            return existingProfile;

        try
        {
            var googleAi = new GoogleAi(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_API_KEY")!);
            var googleModel = googleAi.CreateGenerativeModel(Environment.GetEnvironmentVariable("RUDEBOT_GEMINI_MODEL_NAME")!);

            var messagesText = string.Join("\n",
                userMessages.Select(m => $"[{m.CreatedAt:HH:mm}] {m.Text}"));

            var prompt = Resources.UserProfileMergePrompt
                         + "\n\n"
                         + $"Наявний профіль:\n{(string.IsNullOrEmpty(existingProfile) ? "(порожній)" : existingProfile)}\n\n"
                         + $"Користувач: {userName}\n\n"
                         + $"Сьогоднішні повідомлення:\n{messagesText}";

            var response = await googleModel.GenerateContentAsync(prompt);
            var text = response.Text();

            if (string.IsNullOrWhiteSpace(text))
                return existingProfile;

            return ProfileTruncator.Truncate(text.Trim(), UserProfileConsts.MaxProfileChars);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserProfileMerger.MergeAsync failed for {UserName}", userName);
            return existingProfile;
        }
    }
}
