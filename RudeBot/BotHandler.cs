using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telegram.Bot;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using PowerBot.Lite.Attributes;
using PowerBot.Lite.Handlers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using RudeBot.Managers;
using RudeBot.Models;

namespace RudeBot 
{
    public class BotHandler : BaseHandler
    {
        private IUserManager _userManager {  get; set; }
        public BotHandler()
        {
            _userManager = new UserManager();
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("/start")]
        public async Task Start()
        {
            string messageText = $"Привіт, я Рудекіт!\n\n" +
                "Я можу дещо зробити, якшо ти скажеш чарівне слово:\n" +
                "`Карма` - покажу твою карму,\n" +
                "`Топ` - покажу топ учасників чату,\n" +
                "`Тесла` - порахую дні без згадування тесли,\n" +
                "`Кіт` - покажу котика,\n" +
                "`Шарій` - покажу півника,\n" +
                "`Зрада` - розпочну процедуру бану,\n" +
                "`/warn /unwarn` - (admins only) винесу попередження за погану поведінку,\n" +
                "`/scan` - (admins only) просканую когось,\n" +
                "`/give 25` - поділитися рудекоїнами,\n" +
                "А ще я вітаю новеньких у чаті.\n\n" +
                $"Версія `{Consts.BotVersion}`";

            var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[] { InlineKeyboardButton.WithUrl("Github", Consts.GithubUrl) });

            Message msg = await BotClient.SendTextMessageAsync(ChatId, messageText, ParseMode.Markdown, replyMarkup: keyboard);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler(".ru")]
        public async Task DotRu()
        {
            string messageText = "*Російська пропаганда не може вважатися пруфом!*\n\nВас буде додано до реєстру.";
            Message msg = await BotClient.SendTextMessageAsync(ChatId, messageText, replyToMessageId: Message.MessageId);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("(^карма$|^karma$)")]
        public async Task Karma()
        {
            long getSize(long id)
            {
                return (id + 6) % 15 + 7;
            }

            (int, int) orientation(long id)
            {
                return ((int)id % 3, (int)id % 5 % 2);
            }
          
            TelegramUser user = await _userManager.GetUser(User.Id);

            long userSize = getSize(user.Id);

            float BadWordsPercent = 0;
            if (user.TotalBadWords > 0 && user.TotalMessages > 0)
            {
                BadWordsPercent = user.TotalBadWords * 100 / user.TotalMessages;
            }

            float karmaPercent = 0;
            if (user.Karma > 0 && user.TotalMessages > 0)
            {
                karmaPercent = user.Karma * 100 / user.TotalMessages;
            }

            List<string> orientationTypes = new List<string>() { "Латентний", "Гендерфлюід", "" };
            List<string> orientationNames = new List<string>() { "Android", "Apple" };

            (int, int) orientationValues = orientation(user.Id);

            string orientationType = orientationTypes[orientationValues.Item1];
            string orientationName = orientationNames[orientationValues.Item2];

            string replyText = $"Привіт {user.UserName}, твоя карма:\n\n" +
                $"Карма: `{user.Karma} ({karmaPercent}%)`\n" +
                $"🚧Попереджень: `{user.Warns}`\n" +
                $"Повідомлень: `{user.TotalMessages}`\n" +
                $"Матюків: `{user.TotalBadWords} ({BadWordsPercent}%)`\n" +
                $"Rude-коїнів: `{user.RudeCoins}`💰\n" +
                $"Довжина: `{userSize}` сантиметрів, ну і гігант...\n" +
                $"Орієнтація: `{orientationType} {orientationName}` користувач";

            // Fix markdown
            replyText = replyText.Replace("_", "\\_");

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.UploadVideo)]
        [MessageHandler("шарий|шарій")]
        public async Task CockMan()
        {
            Message msg;

            using (FileStream stream = System.IO.File.OpenRead("data/media/sh.MOV"))
            {
                msg = await BotClient.SendVideoAsync(
                    chatId: ChatId,
                    video: stream);
            }

            await Task.Delay(30 * 1000);
            await BotClient.TryDeleteMessage(msg);
        }
    }
}
