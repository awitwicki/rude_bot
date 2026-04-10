using System.Diagnostics;
using Autofac.Features.AttributeFilters;
using PowerBot.Lite.Middlewares;
using RudeBot.Domain;
using RudeBot.Domain.Interfaces;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.ChatContextService;
using RudeBot.Services.ChatDigestService;
using RudeBot.Services.DuplicateDetectorService;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RudeBot;

public class BotMiddleware : BaseMiddleware
{
    private readonly IUserManager _userManager;
    private readonly TxtWordsDataset _badWordsService;
    private readonly IDuplicateDetectorService _duplicateDetectorService;
    private readonly IAllowedChatsService _allowedChatsService;
    private readonly IChatContextService _chatContextService;
    private readonly IChatDigestService _chatDigestService;

    public BotMiddleware(
        IUserManager userManager,
        [KeyFilter(Consts.BadWordsService)] TxtWordsDataset badWordsService,
        IDuplicateDetectorService duplicateDetectorService,
        IAllowedChatsService allowedChatsService,
        IChatContextService chatContextService,
        IChatDigestService chatDigestService
    )
    {
        _userManager = userManager;
        _badWordsService = badWordsService;
        _duplicateDetectorService = duplicateDetectorService;
        _allowedChatsService = allowedChatsService;
        _chatContextService = chatContextService;
        _chatDigestService = chatDigestService;
    }

    public override async Task Invoke(ITelegramBotClient bot, Update update, Func<Task> func)
    {
        var sw = Stopwatch.StartNew();

        if (update.Type == UpdateType.Message)
        {
            if (!_allowedChatsService.IsChatAllowed(update.Message.Chat.Id))
            { 
                return;
            }
            
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
            Console.WriteLine($"[PERF] Middleware bad words check: {sw.ElapsedMilliseconds}ms");
            var userStats = await _userManager.GetUserChatStats(User.Id, Chat.Id);
            Console.WriteLine($"[PERF] Middleware GetUserChatStats: {sw.ElapsedMilliseconds}ms");

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
                var updatedUser = TelegramUser.FromUser(User);
                userStats.User.UserMention = updatedUser.UserMention;
                userStats.User.UserName = updatedUser.UserName;
                userStats.TotalMessages++;
                userStats.TotalBadWords += messageBadWords;

                // Save user
                await _userManager.UpdateUserChatStats(userStats);
            }
            Console.WriteLine($"[PERF] Middleware user stats update: {sw.ElapsedMilliseconds}ms");

            // Record message to chat context cache
            var userName = User.Username ?? User.FirstName ?? User.Id.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                _chatContextService.AddMessage(Chat.Id, userName, text);
            }

            // Record message to chat digest cache
            if (!string.IsNullOrEmpty(text))
            {
                _chatDigestService.AddMessage(Chat.Id, userName, text);
            }
            else if (Message.Photo != null)
            {
                var digestText = string.IsNullOrEmpty(Message.Caption)
                    ? "[image]"
                    : $"[image] {Message.Caption}";
                _chatDigestService.AddMessage(Chat.Id, userName, digestText);
            }
            else if (Message.Video != null)
            {
                var digestText = string.IsNullOrEmpty(Message.Caption)
                    ? "[video]"
                    : $"[video] {Message.Caption}";
                _chatDigestService.AddMessage(Chat.Id, userName, digestText);
            }

            // Try find duplicates
            // TODO need fix to ignore spam from newbies
            // if (!string.IsNullOrEmpty(Message.Text) || !string.IsNullOrEmpty(Message.Caption))
            // {
            //     var duplicates = _duplicateDetectorService.FindDuplicates(Chat.Id, Message.MessageId, text!);
            //
            //     var duplicateMessageId = duplicates.FirstOrDefault();
            //
            //     if (duplicateMessageId > 0)
            //     {
            //         var messageText = Resources.Klichko;
            //
            //         var chatId = $"{Chat.Id}"[3..];
            //
            //         var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            //         {
            //             InlineKeyboardButton.WithUrl("Тут", $"https://t.me/c/{chatId}/{duplicateMessageId}")
            //         });
            //
            //         await bot.SendTextMessageAsync(Chat.Id, messageText, parseMode: ParseMode.Markdown,
            //             replyParameters: new ReplyParameters
            //             {
            //                 MessageId = Message.MessageId
            //             }, replyMarkup: keyboard);
            //     }
            // }
        }

        Console.WriteLine($"[PERF] Middleware total: {sw.ElapsedMilliseconds}ms");

        // Invoke handler matched methods
        await NextMiddleware.Invoke(bot, update, func);
        Console.WriteLine($"[PERF] Middleware + handler total: {sw.ElapsedMilliseconds}ms");
    }
}
