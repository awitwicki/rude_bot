using RudeBot.Models;

namespace RudeBot.Services.UserProfileService;

public interface IUserProfileMerger
{
    /// Returns the new profile text (already truncated). On failure or empty Gemini response, returns existingProfile unchanged.
    Task<string> MergeAsync(string existingProfile, string userName, List<ChatMessage> userMessages);
}
