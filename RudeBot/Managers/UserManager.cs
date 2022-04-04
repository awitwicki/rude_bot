using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Managers
{
    public class UserManager : IUserManager
    {
        private DataContext _dbContext;

        private UserChatStats? _telegramUserStats { get; set; }

        public UserManager()
        {
            _dbContext = new DataContext();
        }

        public async Task<TelegramUser> CreateUser(TelegramUser user)
        {
            var usr = await _dbContext.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            return usr.Entity;
        }

        // TODO: Make GetUser() result cache for scoped DI container?
        public async Task<UserChatStats> GetUserChatStats(long userId, long chatId)
        {
            if (_telegramUserStats != null)
            {
                return _telegramUserStats;
            }
            else
            {
                UserChatStats userStats = await _dbContext
                    .UserStats
                    .Include(x => x.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.ChatId == chatId);

                _telegramUserStats = userStats;

                return userStats;
            }
        }

        public async Task<UserChatStats> CreateChat(UserChatStats chat)
        {
            var usr = await _dbContext.UserStats.AddAsync(chat);
            await _dbContext.SaveChangesAsync();

            return usr.Entity;
        }

        public async Task<UserChatStats> UpdateUserChatStats(UserChatStats user)
        {
            _dbContext.UserStats.Update(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        public async Task<UserChatStats> CreateUserChatStats(UserChatStats userChatStats)
        {
            // If user exists then remove object to prevent crating new existing user
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userChatStats.UserId);
            if (user != null)
            {
                userChatStats.User = null;
            }

            // Create new UserStats with new user (optional) in db
            var usr = await _dbContext.AddAsync(userChatStats);
            await _dbContext.SaveChangesAsync();

            return usr.Entity;
        }
    }
}
