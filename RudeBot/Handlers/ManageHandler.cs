using Telegram.Bot;
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
using RudeBot.Domain.Resources;

namespace RudeBot.Handlers
{
    public class ManageHandler : BaseHandler
    {
        private IUserManager UserManager { get; set; }
        private IChatSettingsService ChatSettingsService{ get; set; }
        
        public ManageHandler(IUserManager userManager, IChatSettingsService chatSettingsService)
        {
            UserManager = userManager;
            ChatSettingsService = chatSettingsService;
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
                        InlineKeyboardButton.WithUrl(Resources.Form, Resources.GoogleFormForNewbiesURL),
                        InlineKeyboardButton.WithCallbackData(Resources.IAmPromise, $"new_user|{newUser.Id}")
                    });

                    var responseText = string.Format(Resources.HelloMessage, newUser.GetUserMention());

                    var helloMessage = await BotClient.SendAnimationAsync(
                            chatId: ChatId,
                            replyToMessageId: Message.MessageId,
                            caption: responseText,
                            animation: Resources.WelcomeToTheClubBuddyVideoUrl,
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
            var newbieUserId = long.Parse(CallbackQuery.Data!.Split('|').Last());

            // Wrong user clicked
            if (User.Id != newbieUserId)
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, Resources.OnceAgaInAndGetBaned, true);
                return;
            }

            // Newbie clicked
            await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, Resources.NewbieClicked, true);

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
            if (message.ReplyToMessage == null || message.ReplyToMessage.From!.Id == user.Id || message.ReplyToMessage.From.IsBot)
            {
                msg = await BotClient.SendTextMessageAsync(chat.Id, Resources.ShouldBeReplyToMessage, replyToMessageId: message.MessageId);

                await Task.Delay(30 * 1000);

                await BotClient.TryDeleteMessage(msg);
                await BotClient.TryDeleteMessage(message);
                return false;
            }

            // Filter for only admins
            var usrSenderRights = await BotClient.GetChatMemberAsync(chat.Id, user.Id);
            if (!(usrSenderRights.IsHaveAdminRights()))
            {
                msg = await BotClient.SendTextMessageAsync(ChatId, Resources.WarnOrUnwarnIsOnlyForAdmins, replyToMessageId: message.MessageId);

                await Task.Delay(30 * 1000);

                await BotClient.TryDeleteMessage(msg);
                await BotClient.TryDeleteMessage(message);
                return false;
            }

            // Admin cant warn other admins
            var usrReceiverRights = await BotClient.GetChatMemberAsync(chat.Id, message.ReplyToMessage.From.Id);
            if (usrReceiverRights.IsHaveAdminRights())
            {
                msg = await BotClient.SendTextMessageAsync(chat.Id, Resources.WarnOrUnwarnNotWorksOnAdmins, replyToMessageId: message.MessageId);

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
            var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, User.Id);
            if (!usrSenderRights.IsHaveAdminRights())
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, Resources.ButtonOnlyForAdmins, true);
                return;
            }

            await BotClient.EditMessageReplyMarkupAsync(ChatId, MessageId, null);
        }

        [CallbackQueryHandler("^manage_")]
        public async Task ManageUserRights()
        {
            // Filter for only admins
            var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, User.Id);
            if (!usrSenderRights.IsHaveAdminRights())
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, Resources.ButtonOnlyForAdmins, true);
                return;
            }

            // Parse user id
            var userId = long.Parse(CallbackQuery.Data!.Split('|').Last());

            // Parse command
            var command = CallbackQuery.Data
                .Split('|')
                .First()
                .Replace("manage_", "")
                .Replace("_user", "");

            var userStats = await UserManager.GetUserChatStats(userId, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
            {
                await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, Resources.NotDone, true);
                return;
            }

            string actionResult = null;

            switch (command)
            {
                case "ban_media":
                    await BotClient.RestrictChatMemberAsync(ChatId, userId, new ChatPermissions() { CanSendMessages = true, CanSendMediaMessages = false }, DateTime.UtcNow.AddDays(1));
                    actionResult = $"\n{Resources.BannedMedia}";
                    break;
                case "mute_day":
                    await BotClient.RestrictChatMemberAsync(ChatId, userId, new ChatPermissions() { CanSendMessages = false }, DateTime.UtcNow.AddDays(1));
                    actionResult = $"\n{Resources.Muted}";
                    break;
                case "kick":
                    await BotClient.KickChatMemberAsync(ChatId, userId, DateTime.UtcNow.AddMinutes(1));
                    actionResult = $"\n{Resources.Kicked}";
                    break;
                case "ban":
                    await BotClient.KickChatMemberAsync(ChatId, userId, DateTime.UtcNow.AddYears(1000));
                    actionResult = $"\n{Resources.Banned}";
                    break;
                case "add_warn":
                    userStats.Warns++;
                    await UserManager.UpdateUserChatStats(userStats);
                    actionResult = $"\n+1 {Resources.Warned} ({userStats.Warns})";
                    break;
                case "amnesty":
                    // Unban all restrictions
                    var permissions = new ChatPermissions
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
                    await UserManager.UpdateUserChatStats(userStats);
                    actionResult = $"\n{Resources.Amnestied}";
                    break;
            }

            if (actionResult == null)
                actionResult = "\n Error :(";

            var replyText = userStats.BuildWarnMessage();

            var logs = $"\n\n{Resources.Logs}" + CallbackQuery.Message.Text
                .Split($"\n\n{Resources.Logs}")
                .Skip(1)
                .FirstOrDefault();

            logs += actionResult;

            var keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(userId);
            await BotClient.EditMessageTextAsync(ChatId, CallbackQuery.Message.MessageId, replyText + logs, replyMarkup: keyboardMarkup, parseMode: ParseMode.Markdown);

            await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, Resources.Done, true);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/warn$")]
        public async Task Warn()
        {
            var isWarnLegit = await _processWarnRights(Message, Chat, User);

            if (!isWarnLegit)
                return;

            var userStats = await UserManager.GetUserChatStats(Message!.ReplyToMessage!.From!.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            userStats.Warns++;
            await UserManager.UpdateUserChatStats(userStats);

            var replyText = userStats.BuildWarnMessage();

            var keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(Message.ReplyToMessage.From.Id);

            var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.ReplyToMessage.MessageId, replyMarkup: keyboardMarkup, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/unwarn")]
        public async Task Unwarn()
        {
            var isWarnLegit = await _processWarnRights(Message, Chat, User);

            if (!isWarnLegit)
                return;

            var userStats = await UserManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            userStats.Warns--;
            await UserManager.UpdateUserChatStats(userStats);

            var replyText = $"{userStats.User.UserMention}, {Resources.WarnCancelled}";

            if (userStats.Warns > 0)
            {
                replyText += $"\n\n " + string.Format(Resources.WarnBalance, userStats.Warns);
            }

            var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.ReplyToMessage.MessageId, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/scan$")]
        public async Task Scan()
        {
            Message msg;
            string replyText = null;

            // =================Govnocode begin=================
            // Check message is reply, ignore bots
            if (Message.ReplyToMessage == null || User.IsBot && Message.ReplyToMessage == null || Message.ReplyToMessage.From!.Id == User.Id || Message.ReplyToMessage.From.IsBot)
                replyText = Resources.ScanNeedsToBeReplyToMessage;
            else
            {
                // Сheck if user have rights to scan
                var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
                if (!usrSenderRights.IsHaveAdminRights())
                {
                    replyText = Resources.ScanIsOnlyForAdmins;
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

            var userStats = await UserManager.GetUserChatStats(Message.ReplyToMessage!.From.Id, ChatId);

            // If user not exists in db then ignore
            if (userStats == null)
                return;

            // =================Govnocode end=================

            replyText = userStats.BuildInfoString();

            InlineKeyboardMarkup keyboardMarkup = null;

            // If this user is admin then dont pin manage keyboard
            var usrReceiverRights = await BotClient.GetChatMemberAsync(ChatId, Message.ReplyToMessage.From.Id);
            if (!usrReceiverRights.IsHaveAdminRights())
            {
                // TODO: bug - clicks on this keyboard makes bot change message text like it was /warn command
                keyboardMarkup = KeyboardBuilder.BuildUserRightsManagementKeyboard(Message.ReplyToMessage.From.Id);
            }

            await BotClient.SendTextMessageAsync(ChatId, replyText, replyMarkup: keyboardMarkup, parseMode: ParseMode.Markdown);
            await BotClient.TryDeleteMessage(Message);
        }

        [CallbackQueryHandler("^print|")]
        public async Task PrintMessageCallback()
        {
            var message = CallbackQuery!.Data!.Replace("print|", "");
            await BotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, message, true);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/settings$")]
        public async Task GetSettings()
        {
            Message msg = null;
            string replyText;

            // Сheck if user have rights to scan
            var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
            if (!usrSenderRights.IsHaveAdminRights())
            {
                replyText = Resources.CommandIsOnlyForAdmins;
            }
            else
            {
                var chatSettings = await ChatSettingsService.GetChatSettings(ChatId);
                if (chatSettings == null)
                {
                    replyText =$"{Resources.Error} 🤷🏻‍♂️";
                }
                else
                {
                    replyText = $"{Resources.ChatSettings}\n\n"
                        + $"{Resources.russianLangHate} `{chatSettings.HaterussianLang}`\n"
                        + $"{Resources.UseChatGPT} `{chatSettings.UseChatGpt}`\n"
                        + $"{Resources.SendRandomMessages} `{chatSettings.SendRandomMessages}`\n"
                        + $"\n"
                        + $"{Resources.russianLangHateCommandDescription}\n"
                        + $"{Resources.UseChatGPTCommandDescription}\n"
                        + $"{Resources.SendRandomMessagesDescription}\n";
                }
            }

            msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);
         
            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }

        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/haterusianlang")]
        public async Task ChangeHaterusianLang()
        {
            Message msg = null;
            string replyText;

            // Сheck if user have rights to change settings
            var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
            if (!usrSenderRights.IsHaveAdminRights())
            {
                replyText = Resources.CommandIsOnlyForAdmins;
            }
            else
            {
                var chatSettings = await ChatSettingsService.GetChatSettings(ChatId);
                if (chatSettings == null)
                {
                    replyText = $"{Resources.Error} 🤷🏻‍♂️";
                }
                else
                {
                    chatSettings.HaterussianLang = !chatSettings.HaterussianLang;
                    await ChatSettingsService.AddOrUpdateChatSettings(chatSettings);

                    replyText = chatSettings.HaterussianLang ? Resources.russianLangHateOn : Resources.russianLangHateOff;
                }
            }

            msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);
            
            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }
        
        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/sendrandommessages")]
        public async Task ChangeSendRandomMessages()
        {
            Message msg = null;
            string replyText;

            // Check if user have rights to change settings
            var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
            if (!usrSenderRights.IsHaveAdminRights())
            {
                replyText = Resources.CommandIsOnlyForAdmins;
            }
            else
            {
                var chatSettings = await ChatSettingsService.GetChatSettings(ChatId);
                if (chatSettings == null)
                {
                    replyText = $"{Resources.Error} 🤷🏻‍♂️";
                }
                else
                {
                    chatSettings.SendRandomMessages = !chatSettings.SendRandomMessages;
                    await ChatSettingsService.AddOrUpdateChatSettings(chatSettings);

                    replyText = chatSettings.SendRandomMessages ? Resources.SendRandomMessagesOn : Resources.SendRandomMessagesOff;
                }
            }

            msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);
            
            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }
        
        [MessageReaction(ChatAction.Typing)]
        [MessageHandler("^/usechatgpt")]
        public async Task ChangeUseChatGpt()
        {
            Message msg = null;
            string replyText;

            // Сheck if user have rights to change settings
            var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
            if (!usrSenderRights.IsHaveAdminRights())
            {
                replyText = Resources.CommandIsOnlyForAdmins;
            }
            else
            {
                var chatSettings = await ChatSettingsService.GetChatSettings(ChatId);
                if (chatSettings == null)
                {
                    replyText = $"{Resources.Error} 🤷🏻‍♂️";
                }
                else
                {
                    chatSettings.UseChatGpt = !chatSettings.UseChatGpt;
                    await ChatSettingsService.AddOrUpdateChatSettings(chatSettings);

                    replyText = chatSettings.UseChatGpt ? Resources.UseChatGPTOn : Resources.UseChatGPTOff;
                }
            }

            msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);
            
            await Task.Delay(30 * 1000);

            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);
        }
    }
}
