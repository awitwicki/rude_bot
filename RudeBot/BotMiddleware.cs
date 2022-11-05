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
using Telegram.Bot.Types;
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

        public override async Task Invoke(ITelegramBotClient bot, Update update, Func<Task> func)
        {
            if (update.Type != UpdateType.Message)
            {
                return;   
            }

            var Message = update.Message;
            var User = update.Message!.From!;
            var Chat = update.Message!.Chat;

            // Count Bad words
            string text = Message.Text ?? Message.Caption;
            int messageBadWords = 0;
            if (text != null)
            {
                string messageText = text.ToLower();

                var badWords = _badWordsReaderService.GetWords();
                if (badWords.Any(x => messageText.Contains(x)))
                {
                    messageBadWords++;
                }
            }

            // Get UserStats
            UserChatStats userStats = await _userManager.GetUserChatStats(User.Id, Chat.Id);

            // Register new user stats
            if (userStats is null)
            {
                // Create User
                userStats = UserChatStats.FromChat(Chat);
                userStats.UserId = User.Id;
                userStats.TotalMessages = 1;
                userStats.TotalBadWords = messageBadWords;
                userStats.User = TelegramUser.FromUser(User);

                await _userManager.CreateUserChatStats(userStats);
            }
            else
            {
                // Update username, nickname and messages counter
                var user = TelegramUser.FromUser(User);
                userStats.User = TelegramUser.FromUser(User);
                userStats.TotalMessages++;
                userStats.TotalBadWords += messageBadWords;

                // Save user
                await _userManager.UpdateUserChatStats(userStats);
            }

            // Try find dublicates
            if ((Message.Text != null || Message.Caption != null) && Message.ForwardFrom != null)
            {
                var duplicates = _duplicateDetectorService.FindDuplicates(Chat.Id, Message.MessageId, text!);

                int duplicateMessageId = duplicates.FirstOrDefault();

                if (duplicateMessageId > 0)
                {
                    string messageText = $"Про це вже писали";

                    string chatId = $"{Chat.Id}"[3..];

                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { InlineKeyboardButton.WithUrl("Тут", $"https://t.me/c/{chatId}/{duplicateMessageId}") }); 

                    await bot.SendTextMessageAsync(Chat.Id, messageText, ParseMode.Markdown, replyToMessageId: Message.MessageId, replyMarkup: keyboard);
                }
            }

            // Invoke handler matched methods
            await _nextMiddleware.Invoke(bot, update, func);
        }
    }
}
