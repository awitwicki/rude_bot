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
using RudeBot.Extensions;
using RudeBot.Keyboards;

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
        [MessageHandler("[\\w\\-]+\\.ru")]
        public async Task DotRu()
        {
            string messageText = "*Російська пропаганда не може вважатися пруфом!*\n\nВас буде додано до реєстру.";
            Message msg = await BotClient.SendTextMessageAsync(ChatId, messageText, replyToMessageId: Message.MessageId);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("(^карма$|^karma$)")]
        public async Task Karma()
        {
            UserChatStats userStats = await _userManager.GetUserChatStats(User.Id, ChatId);

            string replyText = userStats.BuildInfoString();

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

        [MessageReaction(ChatAction.UploadPhoto)]
        [MessageHandler("samsung|самсунг|сасунг")]
        public async Task Samsung()
        {
            Message msg = await BotClient.SendPhotoAsync(chatId: ChatId, photo: Consts.SamsungUrl, replyToMessageId: Message.MessageId);
            
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

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^-$")]
        public async Task DecreaseKarma()
        {
            // OOh, look at here, is this code dUpLiCatIOn???
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

            userStats.Karma--;
            await _userManager.UpdateUserChatStats(userStats);

            string replyText = $"Ви зменщили карму {userStats.User.UserName} до значення {userStats.Karma}!";

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
        }

        private async Task<bool> _processWarnRights(Message message, Chat chat, User user)
        {
            // Ignore message forwards
            if (message.ForwardFrom != null || message.ForwardFromChat != null)
                return false;

            Message msg = null;

            // Filter only reply to other user, ignore bots
            if (message.ReplyToMessage == null || message.ReplyToMessage.From.Id == user.Id || message.ReplyToMessage.From.IsBot)
            {
                msg = await BotClient.SendTextMessageAsync(chat.Id, "/warn або /unwarn має бути відповіддю, на чиєсь повідомлення", replyToMessageId: message.MessageId);

                await Task.Delay(30 * 1000);

                await BotClient.TryDeleteMessage(msg);
                await BotClient.TryDeleteMessage(message);
                return false;
            }

            // Filter for only admins
            ChatMember usrSenderRights = await BotClient.GetChatMemberAsync(chat.Id, user.Id);
            if (!(usrSenderRights.Status == ChatMemberStatus.Administrator || usrSenderRights.Status == ChatMemberStatus.Creator))
            {
                msg = await BotClient.SendTextMessageAsync(ChatId, "/warn або /unwarn дозволений тільки для адмінів", replyToMessageId: message.MessageId);

                await Task.Delay(30 * 1000);

                await BotClient.TryDeleteMessage(msg);
                await BotClient.TryDeleteMessage(message);
                return false;
            }

            // Admin cant warn other admins
            ChatMember usrReceiverRights = await BotClient.GetChatMemberAsync(chat.Id, message.ReplyToMessage.From.Id);
            if (usrReceiverRights.Status == ChatMemberStatus.Administrator || usrReceiverRights.Status == ChatMemberStatus.Creator)
            {
                msg = await BotClient.SendTextMessageAsync(chat.Id, "/warn або /unwarn не діє на адмінів", replyToMessageId: message.MessageId);

                await Task.Delay(30 * 1000);

                await BotClient.TryDeleteMessage(msg);
                await BotClient.TryDeleteMessage(message);
                return false;
            }

            return true;
        }

        [CallbackQueryHandler("^manage_")]
        public async Task ManageUserRights()
        {
            // Filter for only admins
            ChatMember usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, User.Id);
            if (!(usrSenderRights.Status == ChatMemberStatus.Administrator || usrSenderRights.Status == ChatMemberStatus.Creator))
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Це кнопка для адмінів", true);
                return;
            }

            // Parse user id
            long userId = long.Parse(CallbackQuery.Data!.Split('|').Last());
            
            // Parse command
            string command = CallbackQuery.Data
                .Split('|')
                .First()
                .Replace("manage_", "")
                .Replace("_user", "");

            string actionResult = null;

            switch (command)
            {
                case "ban_media":
                    await BotClient.RestrictChatMemberAsync(ChatId, userId, new ChatPermissions() { CanSendMessages = true, CanSendMediaMessages = false }, DateTime.UtcNow.AddDays(1));
                    actionResult = "\nЗабанено медіа";
                    break;
                case "mute_day":
                    await BotClient.RestrictChatMemberAsync(ChatId, userId, new ChatPermissions() { CanSendMessages = false }, DateTime.UtcNow.AddDays(1));
                    actionResult = "\nМут на день";
                    break;
                case "kick":
                    await BotClient.KickChatMemberAsync(ChatId, userId, DateTime.UtcNow.AddMinutes(1));
                    actionResult = "\nВикинуто з чату";
                    break;
                case "ban":
                    await BotClient.KickChatMemberAsync(ChatId, userId, DateTime.UtcNow.AddYears(1000));
                    actionResult = "\nВідправився за кораблем";
                    break;
                case "amnesty":
                    // Unban all restrictions
                    ChatPermissions permissions = new ChatPermissions
                    {
                        CanSendMessages = true,
                        CanSendMediaMessages = true,
                        CanSendPolls = true,
                        CanSendOtherMessages = true,
                        CanAddWebPagePreviews = true,
                        CanChangeInfo = true,
                        CanInviteUsers = true,
                        CanPinMessages = true,
                    };
                    await BotClient.RestrictChatMemberAsync(ChatId, userId, permissions);

                    UserChatStats userStats = await _userManager.GetUserChatStats(userId, ChatId);

                    // If user not exists in db then ignore
                    if (userStats == null)
                        return;

                    userStats.Warns = 0;
                    await _userManager.UpdateUserChatStats(userStats);
                    actionResult = "\nАмністован";
                    break;
            }

            if (actionResult == null)
                actionResult = "\n Error :(";

            var keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(Message.ReplyToMessage.From.Id);
            await BotClient.EditMessageTextAsync(ChatId, CallbackQuery.Message.MessageId, CallbackQuery.Message.Text + actionResult, replyMarkup: keyboardMarkup);

            await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Виконано", true);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/warn")]
        public async Task Warn()
        {
            bool isWarnLegit = await _processWarnRights(Message, Chat, User);

            if (!isWarnLegit)
                return;

            UserChatStats userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            userStats.Warns++;
            await _userManager.UpdateUserChatStats(userStats);

            string replyText = $"{userStats.User.UserName} , вам винесено попередження адміна!\n" +
                $"Треба думати що ви пишете, \n" +
                $"ви маєте вже {userStats.Warns} попередження!\n\n" +
                $"1 попередження - будь-який адмін може заборонити медіа/стікери/ввести ліміт повідомлень!\n" +
                $"2 попередження - мют на день (або тиждень, на розсуд адміна)!\n" +
                $"3 попередження - бан!\n\n" +
                $"Адміни вирішать твою долю:";

            var keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(Message.ReplyToMessage.From.Id);
              
            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.ReplyToMessage.MessageId, replyMarkup: keyboardMarkup, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/unwarn")]
        public async Task Unwarn()
        {
            bool isWarnLegit = await _processWarnRights(Message, Chat, User);

            if (!isWarnLegit)
                return;

            UserChatStats userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            userStats.Warns--;
            await _userManager.UpdateUserChatStats(userStats);

            string replyText = $"{userStats.User.UserName}, попередження анульовано!";

            if (userStats.Warns > 0)
            {
                replyText += $"\n\n На балансі ще {userStats.Warns} попередженнь";
            }

            Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.ReplyToMessage.MessageId, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/scan$")]
        public async Task Scan()
        {
            Message msg = null;
            string replyText = null;

            // =================Govnocode begin=================
            // Check message is reply, ignore bots
            if (Message.ReplyToMessage == null || User.IsBot && Message.ReplyToMessage == null || Message.ReplyToMessage.From.Id == User.Id || Message.ReplyToMessage.From.IsBot)
                replyText = "/scan має бути відповіддю, на чиєсь повідомлення (боти не рахуються)";
            else
            {
                // Сheck if user have rights to scan
                ChatMember usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From.Id);
                if (usrSenderRights.Status != ChatMemberStatus.Administrator && usrSenderRights.Status != ChatMemberStatus.Creator)
                {
                    replyText = "/scan дозволений тільки для адмінів";
                }
            }
               
            if (replyText != null)
            {
                msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId);

                await Task.Delay(30 * 1000);

                await BotClient.TryDeleteMessage(msg);
                await BotClient.TryDeleteMessage(Message);
                return;
            }

            UserChatStats userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            // =================Govnocode end=================

            replyText = userStats.BuildInfoString();

            InlineKeyboardMarkup keyboardMarkup = null;

            // If this user is admin then dont pin manage keyboard
            ChatMember usrReceiverRights = await BotClient.GetChatMemberAsync(ChatId, Message.ReplyToMessage.From.Id);
            if (usrReceiverRights.Status != ChatMemberStatus.Administrator && usrReceiverRights.Status != ChatMemberStatus.Creator)
            {
                keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(Message.ReplyToMessage.From.Id);
            }

            msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyMarkup: keyboardMarkup, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageTypeFilter(MessageType.Text)]
        public async Task MessageTrigger()
        {
            if (Message.Text != null)
            {
                string replyText = null;
                Random random = new Random();

                if ((Message?.ReplyToMessage?.From?.Id == BotClient.BotId) || (random.Next(1, 1000) > 985))
                {
                    using (var scope = DIContainerInstance.Container.BeginLifetimeScope())
                    {
                        TxtWordsDatasetReader advicesTxtReader = scope.ResolveNamed<TxtWordsDatasetReader>(Consts.AdvicesReaderService);

                        string messageText = Message.Text.ToLower();

                        var advices = advicesTxtReader.GetWords();
                        replyText = advices.PickRandom();
                    }
                }

                if (replyText != null)
                {
                    bool isReply = (random.Next(100) > 50);
                    Message msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: isReply ? Message.MessageId : null);
                }
            }
        }
    }
}
