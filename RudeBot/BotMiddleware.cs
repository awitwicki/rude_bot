using Autofac;
using Autofac.Features.AttributeFilters;
using PowerBot.Lite.Middlewares;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.DuplicateDetectorService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RudeBot
{
    public class BotMiddleware : BaseMiddleware
    {
        private IUserManager _userManager { get; set; }
        private TxtWordsDatasetReader _badWordsReaderService { get; set; }
        private IDuplicateDetectorService _duplicateDetectorService { get; set; }

        public BotMiddleware(
            IUserManager userManager,
            [KeyFilter(Consts.BadWordsReaderService)] TxtWordsDatasetReader badWordsReaderService,
            IDuplicateDetectorService duplicateDetectorService
        )
        {
            _userManager = userManager;
            _badWordsReaderService = badWordsReaderService;
            _duplicateDetectorService = duplicateDetectorService;
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

            // Try find dublicates
            if ((Message.Text != null || Message.Caption != null) && Message.ForwardFrom != null)
            {
                string text = Message.Text ?? Message.Caption;

                var duplicates = _duplicateDetectorService.FindDuplicates(ChatId, MessageId, text);

                int duplicateMessageId = duplicates.FirstOrDefault();

                if (duplicateMessageId > 0)
                {
                    string messageText = $"Про це вже писали";

                    string chatId = $"{ChatId}"[3..];

                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { InlineKeyboardButton.WithUrl("Тут", $"https://t.me/c/{chatId}/{duplicateMessageId}") }); 

                    await BotClient.SendTextMessageAsync(ChatId, messageText, ParseMode.Markdown, replyToMessageId: MessageId, replyMarkup: keyboard);
                }
            }

            // Save user
            await _userManager.UpdateUserChatStats(userStats);
        }
    }
}
