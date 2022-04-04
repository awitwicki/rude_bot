using RudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Managers
{
    public interface IUserManager
    {
        Task<UserChatStats> GetUserChatStats(long userId, long chatId);
        Task<TelegramUser> CreateUser(TelegramUser user);
        Task<UserChatStats> CreateUserChatStats(UserChatStats userChatStats);
        Task<UserChatStats> UpdateUserChatStats(UserChatStats user);
    }
}
