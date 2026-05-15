using System.Diagnostics;
using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using PowerBot.Lite.Middlewares;
using RudeBot.Domain;
using RudeBot.Domain.Interfaces;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.DuplicateDetectorService;
using Serilog.Context;
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
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ILogger<BotMiddleware> _logger;

    public BotMiddleware(
        IUserManager userManager,
        [KeyFilter(Consts.BadWordsService)] TxtWordsDataset badWordsService,
        IDuplicateDetectorService duplicateDetectorService,
        IAllowedChatsService allowedChatsService,
        IChatMessageRepository chatMessageRepository,
        ILogger<BotMiddleware> logger
    )
    {
        _userManager = userManager;
        _badWordsService = badWordsService;
        _duplicateDetectorService = duplicateDetectorService;
        _allowedChatsService = allowedChatsService;
        _chatMessageRepository = chatMessageRepository;
        _logger = logger;
    }

    public override async Task Invoke(ITelegramBotClient bot, Update update, Func<Task> func)
    {
        long? chatId = update.Message?.Chat?.Id;
        long? userId = update.Message?.From?.Id;

        using (LogContext.PushProperty("ChatId", chatId))
        using (LogContext.PushProperty("UserId", userId))
        {
            var sw = Stopwatch.StartNew();

            if (update.Type == UpdateType.Message)
            {
                if (!_allowedChatsService.IsChatAllowed(update.Message!.Chat.Id))
                {
                    return;
                }

                var Message = update.Message;
                var User = update.Message!.From!;
                var Chat = update.Message!.Chat;

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

                _logger.LogDebug("Middleware bad words check: {ElapsedMs}ms", sw.ElapsedMilliseconds);

                var rows = await _userManager.IncrementUserChatStats(User.Id, Chat.Id, messageBadWords);
                _logger.LogDebug("Middleware user stats increment: {ElapsedMs}ms", sw.ElapsedMilliseconds);

                if (rows == 0)
                {
                    var newStats = UserChatStats.FromChat(Chat);
                    newStats.UserId = User.Id;
                    newStats.TotalMessages = 1;
                    newStats.TotalBadWords = messageBadWords;
                    newStats.User = TelegramUser.FromUser(User);

                    await _userManager.CreateUserChatStats(newStats);
                    _logger.LogDebug("Middleware user stats create: {ElapsedMs}ms", sw.ElapsedMilliseconds);
                }

                // Persist message for context + digest
                var userName = User.Username ?? User.FirstName ?? User.Id.ToString();
                var storedText = text;
                if (string.IsNullOrEmpty(storedText))
                {
                    if (Message.Photo != null)
                    {
                        storedText = string.IsNullOrEmpty(Message.Caption)
                            ? "[image]"
                            : $"[image] {Message.Caption}";
                    }
                    else if (Message.Video != null)
                    {
                        storedText = string.IsNullOrEmpty(Message.Caption)
                            ? "[video]"
                            : $"[video] {Message.Caption}";
                    }
                }

                if (!string.IsNullOrEmpty(storedText))
                {
                    try
                    {
                        await _chatMessageRepository.AddAsync(new ChatMessage
                        {
                            ChatId = Chat.Id,
                            UserId = User.Id,
                            UserName = userName,
                            Text = storedText,
                            MessageId = Message.MessageId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ChatMessage persist failed");
                    }
                }
            }

            _logger.LogDebug("Middleware total: {ElapsedMs}ms", sw.ElapsedMilliseconds);

            await NextMiddleware.Invoke(bot, update, func);
            _logger.LogDebug("Middleware + handler total: {ElapsedMs}ms", sw.ElapsedMilliseconds);
        }
    }
}
