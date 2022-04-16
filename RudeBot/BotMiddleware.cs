using PowerBot.Lite.Middlewares;
using RudeBot.Managers;
using RudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot
{
    public class BotMiddleware : BaseMiddleware
    {
        // TODO: make _userManager scoped DI contaier
        private IUserManager _userManager { get; set; }
        public BotMiddleware()
        {
            _userManager = new UserManager();
        }

        public override async Task Invoke()
        {
            // Get UserStats
            UserChatStats userStats = await _userManager.GetUserChatStats(User.Id, ChatId);

            // Register new user stats
            if (userStats == null)
            {
                userStats = UserChatStats.FromChat(Chat);
                userStats.UserId = User.Id;
                userStats.User = TelegramUser.FromUser(User);

                await _userManager.CreateUserChatStats(userStats);
            }

            userStats.TotalMessages++;

            // Bad words
            // ...

            // Save user
            await _userManager.UpdateUserChatStats(userStats);
        }
    }
}
