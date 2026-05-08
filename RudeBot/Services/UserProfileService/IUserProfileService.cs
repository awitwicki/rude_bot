using RudeBot.Models;

namespace RudeBot.Services.UserProfileService;

public interface IUserProfileService
{
    Task<UserChatProfile?> GetAsync(long chatId, long userId);
    Task<List<UserChatProfile>> ListForChatAsync(long chatId);
    Task UpsertAsync(long chatId, long userId, string userName, string profile);
}
