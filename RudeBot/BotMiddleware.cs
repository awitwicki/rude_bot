using Autofac.Features.AttributeFilters;
using PowerBot.Lite.Middlewares;
using RudeBot.Domain;
using RudeBot.Domain.Resources;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.DuplicateDetectorService;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RudeBot;

public class BotMiddleware : BaseMiddleware
{
    private readonly IUserManager _userManager;
    private readonly TxtWordsDataset _badWordsService;
    private readonly IDuplicateDetectorService _duplicateDetectorService;

    public BotMiddleware(
        IUserManager userManager,
        [KeyFilter(Consts.BadWordsService)] TxtWordsDataset badWordsService,
        IDuplicateDetectorService duplicateDetectorService
    )
    {
        _userManager = userManager;
        _badWordsService = badWordsService;
        _duplicateDetectorService = duplicateDetectorService;
    }

    public override async Task Invoke(ITelegramBotClient bot, Update update, Func<Task> func)
    {
        if (update.Type == UpdateType.Message)
        {
            var Message = update.Message;
            var User = update.Message!.From!;
            var Chat = update.Message!.Chat;

            // Count Bad words
            var text = Message.Text ?? Message.Caption;
            var messageBadWords = 0;
            if (text != null)
            {
                var messageText = text.ToLower();

                var badWords = _badWordsService.GetWords();
                if (badWords.Any(x => messageText.Contains(x)))
                {
                    messageBadWords++;
                }
            }

            // Get UserStats
            var userStats = await _userManager.GetUserChatStats(User.Id, Chat.Id);

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

            // Try find duplicates
            if (!string.IsNullOrEmpty(Message.Text) || !string.IsNullOrEmpty(Message.Caption))
            {
                var duplicates = _duplicateDetectorService.FindDuplicates(Chat.Id, Message.MessageId, text!);

                var duplicateMessageId = duplicates.FirstOrDefault();

                if (duplicateMessageId > 0)
                {
                    var messageText = Resources.Klichko;

                    var chatId = $"{Chat.Id}"[3..];

                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl("Тут", $"https://t.me/c/{chatId}/{duplicateMessageId}")
                    });

                    await bot.SendTextMessageAsync(Chat.Id, messageText, parseMode: ParseMode.Markdown,
                        replyToMessageId: Message.MessageId, replyMarkup: keyboard);
                }
            }
        }

        // Invoke handler matched methods
        await NextMiddleware.Invoke(bot, update, func);
    }
}