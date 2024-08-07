using RudeBot.Models;

namespace RudeBot.Managers;

public interface IUserManager
{
    Task<UserChatStats> GetUserChatStats(long userId, long chatId);
    Task<IEnumerable<UserChatStats>> GetAllUsersChatStats(long chatId);
    Task<TelegramUser> CreateUser(TelegramUser user);
    Task<UserChatStats> CreateUserChatStats(UserChatStats userChatStats);
    Task<UserChatStats> UpdateUserChatStats(UserChatStats user);
    Task<string> RudeCoinsTransaction(UserChatStats userSender, UserChatStats userReceiver, int amount);
}