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
using RudeBot.Services;
using Autofac;
using PowerBot.Lite.Utils;

namespace RudeBot
{
    public class BotHandler : BaseHandler
    {
        private IUserManager _userManager { get; set; }
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

            UserChatStats userStats = await _userManager.GetUserChatStats(User.Id, ChatId);

            long userSize = getSize(userStats.Id);

            float BadWordsPercent = 0;
            if (userStats.TotalBadWords > 0 && userStats.TotalMessages > 0)
            {
                BadWordsPercent = userStats.TotalBadWords * 100 / userStats.TotalMessages;
            }

            float karmaPercent = 0;
            if (userStats.Karma > 0 && userStats.TotalMessages > 0)
            {
                karmaPercent = userStats.Karma * 100 / userStats.TotalMessages;
            }

            List<string> orientationTypes = new List<string>() { "Латентний", "Гендерфлюід", "" };
            List<string> orientationNames = new List<string>() { "Android", "Apple" };

            (int, int) orientationValues = orientation(userStats.Id);

            string orientationType = orientationTypes[orientationValues.Item1];
            string orientationName = orientationNames[orientationValues.Item2];

            string replyText = $"Привіт {userStats.User.UserName}, твоя карма:\n\n" +
                $"Карма: `{userStats.Karma} ({karmaPercent}%)`\n" +
                $"🚧Попереджень: `{userStats.Warns}`\n" +
                $"Повідомлень: `{userStats.TotalMessages}`\n" +
                $"Матюків: `{userStats.TotalBadWords} ({BadWordsPercent}%)`\n" +
                $"Rude-коїнів: `{userStats.RudeCoins}`💰\n" +
                $"Довжина: `{userSize}` сантиметрів, ну і гігант...\n" +
                $"Орієнтація: `{orientationType} {orientationName}` користувач";

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.UploadVideo)]
        [MessageHandler("шарий|шарій")]
        public async Task CockMan()
        {
            Message msg = await BotClient.SendVideoAsync(chatId: ChatId, video: Consts.CockmanVideoUrl);

            await Task.Delay(30 * 1000);
            await BotClient.TryDeleteMessage(msg);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("ё|ъ|ы|э")]
        public async Task Palanytsa()
        {
            // Ignore message forwards
            if (Message.ForwardFrom != null || Message.ForwardFromChat != null)
                return;

            string replyText = "Ану кажи \"паляниця\" 😡";

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("tesl|тесл")]
        public async Task Tesla()
        {
            using (var scope = DIContainerInstance.Container.BeginLifetimeScope())
            {
                var tickerService = scope.Resolve<ITickerService>();

                double tickerPrice = await tickerService.GetTickerPrice("TSLA");

                string replyText = "Днів без згадування тесли: `0`\n🚗🚗🚗" +
                        $"\n\n...btw ${tickerPrice}";

                Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);
            }
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageTypeFilter(MessageType.ChatMembersAdded)]
        public async Task NewUserInChat()
        {
            // Process each new user in chat
            foreach (var newUser in Message.NewChatMembers!)
            {
                Task.Run(async () =>
                {
                    // Process each new user in chat
                    var keyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                    InlineKeyboardButton.WithUrl("Анкета", Consts.GoogleFormForNewbies),
                    InlineKeyboardButton.WithCallbackData("Я обіцяю!", $"new_user|{newUser.Id}")
                    });

                    string responseText = $"Вітаємо {newUser.GetUserMention()} у нашому чаті! " +
                        $"Ми не чат, а дружня, толерантна IT спільнота, яка поважає думку кожного, приєднавшись, " +
                        $"ти згоджуєшся стати чемною частиною спільноти (та полюбити епл)." +
                        $"\n\nI якшо не важко, пліз тут анкета на 8 питань";

                    Message helloMessage = await BotClient.SendAnimationAsync(
                            chatId: ChatId,
                            replyToMessageId: Message.MessageId,
                            caption: responseText,
                            animation: Consts.WelcomeToTheClubBuddyVideoUrl,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboardMarkup);

                    // Wait
                    await Task.Delay(90 * 1000);

                    // Try Remove Hello message
                    try
                    {
                        await BotClient.DeleteMessageAsync(helloMessage.Chat.Id, helloMessage.MessageId);
                    }
                    catch { }
                });
            }
        }

        [CallbackQueryHandler("new_user")]
        public async Task OnUserAuthorized()
        {
            long newbieUserId = long.Parse(CallbackQuery.Data!.Split('|').Last());

            // Wrong user clicked
            if (User.Id != newbieUserId)
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Ще раз і бан :)", true);
                return;
            }

            // Newbie clicked
            await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Дуже раді вас бачити! Будь ласка, ознайомтеся з Конституцією чату в закріплених повідомленнях.", true);

            // Delete captcha message
            await BotClient.DeleteMessageAsync(ChatId, Message.MessageId);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler(Consts.TnxWordsRegex)]
        public async Task IncreaseKarma()
        {
            // Ignore message forwards
            if (Message.ForwardFrom != null || Message.ForwardFromChat != null)
                return;

            // Filter only reply to other user, ignore bots
            if (Message.ReplyToMessage == null || Message.ReplyToMessage.From.Id == User.Id || Message.ReplyToMessage.From.IsBot)
                return;

            UserChatStats userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            userStats.Karma++;
            await _userManager.UpdateUserChatStats(userStats);

            string replyText = $"Ви збільшили карму {userStats.User.UserName} до значення {userStats.Karma}! 🥳";

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
        }
    }
}
