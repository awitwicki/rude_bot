using Autofac;
using Autofac.Features.AttributeFilters;
using PowerBot.Lite.Middlewares;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot
{
    public class BotMiddleware : BaseMiddleware
    {
        private IUserManager _userManager { get; set; }
        private TxtWordsDatasetReader _badWordsReaderService { get; set; }
        public BotMiddleware(
            IUserManager userManager,
            [KeyFilter(Consts.BadWordsReaderService)] TxtWordsDatasetReader badWordsReaderService
        )
        {
            _userManager = userManager;
            _badWordsReaderService = badWordsReaderService;
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

            // Update username, nickname and messages counter
            userStats.User = TelegramUser.FromUser(User);
            userStats.TotalMessages++;

            // Count Bad words
            if (Message.Text != null)
            {
                string messageText = Message.Text.ToLower();

                var badWords = _badWordsReaderService.GetWords();
                if (badWords.Any(x => messageText.Contains(x)))
                {
                    userStats.TotalBadWords++;
                }
            }

            // Save user
            await _userManager.UpdateUserChatStats(userStats);
        }
    }
}
