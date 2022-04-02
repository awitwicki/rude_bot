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
        Task<TelegramUser> GetUser(long userId);
        Task<TelegramUser> CreateUser(TelegramUser user);
        Task<TelegramUser> UpdateUser(TelegramUser user);
    }
}
