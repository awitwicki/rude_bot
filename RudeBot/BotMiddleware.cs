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
            TelegramUser user = await _userManager.GetUser(User.Id);

            // Register new user
            if (user == null)
            {
                user = TelegramUser.FromUser(User);

                await _userManager.CreateUser(user);
            }

            user.TotalMessages++;

            // Bad words
            // ...

            // Handle karma
            // ...

            // Save user
            await _userManager.UpdateUser(user);
        }
    }
}
