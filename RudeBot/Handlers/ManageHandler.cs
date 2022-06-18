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
using PowerBot.Lite.Utils;
using RudeBot.Extensions;
using RudeBot.Keyboards;

namespace RudeBot.Handlers
{
    public class ManageHandler : BaseHandler
    {
        private IUserManager _userManager { get; set; }
        
        public ManageHandler(IUserManager userManager)
        {
            _userManager = userManager;
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

        [CallbackQueryHandler("^manage_hide_keyboard$")]
        public async Task ManageHideKeyboard()
        {
            // Filter for only admins
            ChatMember usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, User.Id);
            if (!(usrSenderRights.Status == ChatMemberStatus.Administrator || usrSenderRights.Status == ChatMemberStatus.Creator))
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Це кнопка для адмінів", true);
                return;
            }

            await BotClient.EditMessageReplyMarkupAsync(ChatId, MessageId, null);
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

            UserChatStats userStats = await _userManager.GetUserChatStats(userId, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Не виконано", true);
                return;
            }

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
                case "add_warn":
                    userStats.Warns++;
                    await _userManager.UpdateUserChatStats(userStats);
                    actionResult = $"\n+1 варн ({userStats.Warns})";
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

                    userStats.Warns = 0;
                    await _userManager.UpdateUserChatStats(userStats);
                    actionResult = "\nАмністован";
                    break;
            }

            if (actionResult == null)
                actionResult = "\n Error :(";

            string replyText = userStats.BuildWarnMessage();

            string logs = "\n\nЛоги:" + CallbackQuery.Message.Text
                .Split("\n\nЛоги:")
                .Skip(1)
                .FirstOrDefault();

            logs += actionResult;

            var keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(userId);
            await BotClient.EditMessageTextAsync(ChatId, CallbackQuery.Message.MessageId, replyText + logs, replyMarkup: keyboardMarkup, parseMode: ParseMode.Markdown);

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

            string replyText = userStats.BuildWarnMessage();

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

            string replyText = $"{userStats.User.UserMention}, попередження анульовано!";

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
                // TODO: bug - clicks on this keyboard makes bot change message text like it was /warn command
                keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(Message.ReplyToMessage.From.Id);
            }

            msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyMarkup: keyboardMarkup, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);
        }

        [CallbackQueryHandler("^print|")]
        public async Task PrintMessageCallback()
        {
            string message = CallbackQuery!.Data!.Replace("print|", "");
            await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, message, true);
        }
    }
}
