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

        private TelegramUser? _telegramUser { get; set; }

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
        public async Task<TelegramUser> GetUser(long userId)
        {
            if (_telegramUser != null)
            {
                return _telegramUser;
            }
            else
            {
                TelegramUser user = await _dbContext
                    .Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == userId);

                _telegramUser = user;

                return user;
            }
        }

        public async Task<TelegramUser> UpdateUser(TelegramUser user)
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }
    }
}
