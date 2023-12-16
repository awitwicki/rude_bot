using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using PowerBot.Lite.Attributes;
using PowerBot.Lite.Handlers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Extensions;
using Autofac.Features.AttributeFilters;
using OpenAI_API;
using RudeBot.Common.TransactionHelpers;
using RudeBot.Domain;
using RudeBot.Domain.Interfaces;
using RudeBot.Domain.Resources;

namespace RudeBot.Handlers;

public class BotHandler : BaseHandler
{
    private IUserManager _userManager { get; set; }
    private ITickerService _tickerService { get; set; }
    private ICatService _catService { get; set; }
    private ITxtWordsDataset AdvicesService { get; set; }
    private static Object _topLocked { get; set; } = new Object();
    private IChatSettingsService _chatSettingsService { get; set; }
    private readonly IDelayService _delayService;
        
    private ITeslaChatCounterService _teslaChatCounterService { get; set; }

    public BotHandler(
        IUserManager userManager,
        IChatSettingsService chatSettingsService,
        ITeslaChatCounterService teslaChatCounterService,
        ITickerService tickerService,
        ICatService catService,
        [KeyFilter(Consts.AdvicesService)] ITxtWordsDataset advicesService,
        IDelayService delayService
    )
    {
        _userManager = userManager;
        _chatSettingsService = chatSettingsService;
        _teslaChatCounterService = teslaChatCounterService;
        _tickerService = tickerService;
        _catService = catService;
        AdvicesService = advicesService;
        _delayService = delayService;
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("(^/start|^/help)")]
    public async Task Start()
    {
        var messageText = string.Format(Resources.InfoText, Consts.BotVersion);

        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithUrl(Resources.Page, Resources.ProjectUrl)
        });

        var msg = await BotClient.SendTextMessageAsync(ChatId, messageText,  parseMode: ParseMode.Markdown, replyMarkup: keyboard);

        await _delayService.DelaySeconds(60);

        await BotClient.TryDeleteMessage(msg);
        await BotClient.TryDeleteMessage(Message);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("[\\w\\-]+\\.ru")]
    public async Task DotRu()
    {
        var messageText = Resources.ruPropaganda;
        await BotClient.SendTextMessageAsync(ChatId, messageText, replyToMessageId: Message.MessageId);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("(^карма$|^karma$)")]
    public async Task Karma()
    {
        var userStats = await _userManager.GetUserChatStats(User.Id, ChatId);

        var replyText = userStats.BuildInfoString();

        var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

        await _delayService.DelaySeconds(30);

        await BotClient.TryDeleteMessage(msg);
        await BotClient.TryDeleteMessage(Message);
    }

    [MessageReaction(ChatAction.UploadVideo)]
    [MessageHandler("шарий|шарій")]
    public async Task CockMan()
    {
        var msg = await BotClient.SendVideoAsync(chatId: ChatId, video: InputFile.FromUri(Resources.CockmanVideoUrl));

        await _delayService.DelaySeconds(30);
        await BotClient.TryDeleteMessage(msg);
    }

    [MessageReaction(ChatAction.UploadPhoto)]
    [MessageHandler("samsung|самсунг|сасунг")]
    public async Task Samsung()
    {
        var msg = await BotClient.SendPhotoAsync(chatId: ChatId, photo: InputFile.FromUri(Resources.SamsungUrl), replyToMessageId: Message.MessageId);

        await _delayService.DelaySeconds(30);
        await BotClient.TryDeleteMessage(msg);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("ё|ъ|ы|э")]
    public async Task Palanytsa()
    {
        var chatSettings = await _chatSettingsService.GetChatSettings(ChatId);

        // Ignore message forwards except self forwards or if hate settings turned off
        if (!chatSettings.HaterussianLang)
            return;
            
        if (Message.ForwardFrom != null && Message.ForwardFrom.Id != User.Id)
            return;
        
        if (Message.ForwardFromChat != null)
            return;

        var replyText = Resources.Palanytsia;

        var msg = await BotClient.SendAnimationAsync(
            chatId: ChatId,
            replyToMessageId: Message.MessageId,
            caption: replyText,
            animation: InputFile.FromUri(Resources.ItsUaChatVideoUrl),
            parseMode: ParseMode.Markdown);

        await _delayService.DelaySeconds(30);
        await BotClient.TryDeleteMessage(msg);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("tesl|тесл")]
    public async Task Tesla()
    {
        var lastTeslaInChat = await _teslaChatCounterService.GetTeslaInChatDate(ChatId);

        if (lastTeslaInChat == null)
        {
            lastTeslaInChat = new TeslaChatCounter
            {
                ChatId = ChatId,
                Date = DateTimeOffset.UtcNow
            };
        }
        else
        {
            var timeFromLastTesla = (DateTimeOffset.UtcNow - lastTeslaInChat.Date).ToString("dd\\.hh\\:mm\\:ss");
            var replyText = string.Format(Resources.TeslaAgain, timeFromLastTesla);

            await BotClient.SendTextMessageAsync(ChatId, replyText,
                replyToMessageId: Message.MessageId, parseMode: ParseMode.Html);
        }

        lastTeslaInChat.Date = DateTimeOffset.UtcNow;
        await _teslaChatCounterService.AddOrUpdateTeslaInChatDate(lastTeslaInChat);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler(Consts.TnxWordsRegex)]
    public async Task IncreaseKarma()
    {
        // Ignore message forwards
        if (Message.ForwardFrom != null || Message.ForwardFromChat != null)
            return;

        // Filter only reply to other user, ignore bots
        if (Message.ReplyToMessage == null || Message.ReplyToMessage.From!.Id == User.Id || Message.ReplyToMessage.From.IsBot)
            return;

        var userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

        // If user not exists in db then ignore
        if (userStats == null)
            return;

        userStats.Karma++;
        await _userManager.UpdateUserChatStats(userStats);

        var replyText = string.Format(Resources.KarmaIncrease, userStats.User.UserMention, userStats.Karma);

        var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

        await _delayService.DelaySeconds(30);
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
        if (Message.ReplyToMessage == null || Message.ReplyToMessage.From!.Id == User.Id || Message.ReplyToMessage.From.IsBot)
            return;

        var userStats = await _userManager.GetUserChatStats(Message.ReplyToMessage.From.Id, ChatId);

        // If user not exists in db then ignore
        if (userStats == null)
            return;

        userStats.Karma--;
        await _userManager.UpdateUserChatStats(userStats);

        var replyText = string.Format(Resources.KarmaDecrease, userStats.User.UserMention, userStats.Karma);

        var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

        await _delayService.DelaySeconds(30);
        await BotClient.TryDeleteMessage(msg);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("(^топ$|^top$)")]
    public async Task Top()
    {
        // Prevent for top spamming (1 top message per all chats, needs to rework)
        var timeout = TimeSpan.FromMilliseconds(50);
        var lockTaken = false;

        try
        {
            Monitor.TryEnter(_topLocked, timeout, ref lockTaken);
            if (lockTaken)
            {
                // Get all users
                var users = await _userManager.GetAllUsersChatStats(ChatId);

                var replyText = $"*{Resources.AccountsInTheChat} {users.Count()}*\n\n";
                replyText += $"{Resources.TopChatKarma}\n";

                users.OrderByDescending(x => x.Karma)
                    .Take(5)
                    .ToList()
                    .ForEach(x =>
                    {
                        var karmaPercent = "0";
                        if (x.Karma > 0 && x.TotalMessages > 0)
                        {
                            karmaPercent = ((float)x.Karma * 100 / x.TotalMessages).ToString("0.00",
                                new NumberFormatInfo{NumberDecimalSeparator = "."});
                        }

                        replyText += $"`{x.User.UserName}` - {Resources.Karma} `{x.Karma} ({karmaPercent}%)`\n";
                    });

                var topMinus3Users = users.OrderBy(x => x.Karma)
                    .Where(x => x.Karma < 0)
                    .Take(3)
                    .OrderByDescending(x => x.Karma)
                    .ToList();

                if (topMinus3Users.Any())
                {
                    replyText += $"\n{Resources.TopChatNegativeKarma}\n";

                    topMinus3Users.ForEach(x =>
                    {
                        var karmaPercent = "0";
                        if (x.Karma > 0 && x.TotalMessages > 0)
                        {
                            karmaPercent = ((float)x.Karma * 100 / x.TotalMessages).ToString("0.00",
                                new NumberFormatInfo{NumberDecimalSeparator = "."});
                        }
                            
                        replyText += $"`{x.User.UserName}` - {Resources.Karma} `{x.Karma} ({karmaPercent}%)`\n";
                    });
                }

                replyText += $"\n{Resources.TopChatActive}\n";

                users.OrderByDescending(x => x.TotalMessages)
                    .Take(5)
                    .ToList()
                    .ForEach(x =>
                    {
                        replyText += $"`{x.User.UserName}` - {Resources.Messages} `{x.TotalMessages}`\n";
                    });

                replyText += $"\n{Resources.TopChatEmotionals}\n";

                users.OrderByDescending(x => x.TotalBadWords)
                    .Take(5)
                    .ToList()
                    .ForEach(x =>
                    {
                        var BadWordsPercent = "0";
                        if (x.TotalBadWords > 0 && x.TotalMessages > 0)
                        {
                            BadWordsPercent = ((float)x.TotalBadWords * 100 / x.TotalMessages).ToString("0.00",
                                new NumberFormatInfo{NumberDecimalSeparator = "."});
                        }

                        replyText += $"`{x.User.UserName}` - {Resources.BadWords} `{x.TotalBadWords} ({BadWordsPercent}%)`\n";
                    });

                var topWarnsUsers = users.OrderByDescending(x => x.Warns)
                    .Where(x => x.Warns > 0)
                    .Take(5)
                    .ToList();

                if (topWarnsUsers.Any())
                {
                    replyText += $"\n{Resources.TopChatWarns}\n";

                    topWarnsUsers.ForEach(x =>
                    {
                        replyText += $"`{x.User.UserName}` - {Resources.Warns} `{x.Warns}`\n";
                    });
                }

                var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: Message.MessageId, parseMode: ParseMode.Markdown);

                await _delayService.DelaySeconds(300);

                BotClient.TryDeleteMessage(msg).Wait();
                BotClient.TryDeleteMessage(Message).Wait();
            }
            else // Top list is already exists, just remove top command message
            {
                await BotClient.TryDeleteMessage(Message);
            }
        }
        finally
        {
            // Ensure that the lock is released.
            if (lockTaken)
            {
                Monitor.Exit(_topLocked);
            }
        }
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("^/tickets$")]
    public async Task TicketList()
    {
        var replyText = "";

        // Сheck if user have rights to scan
        var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
        if (usrSenderRights.Status != ChatMemberStatus.Administrator && usrSenderRights.Status != ChatMemberStatus.Creator)
        {
            replyText = Resources.OnlyAdminsAreAllowed;
        }
        else
        {
            var ticketManager = new TicketManager();
            replyText = await ticketManager.GetChatTickets(ChatId);
        }

        var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, parseMode: ParseMode.Markdown);
        await BotClient.TryDeleteMessage(Message);

        await _delayService.DelaySeconds(30);
        await BotClient.TryDeleteMessage(msg);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("^/addticket")]
    public async Task AddTicket()
    {
        var replyText = "";

        // Сheck if user have rights to scan
        var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
        if (usrSenderRights.Status != ChatMemberStatus.Administrator && usrSenderRights.Status != ChatMemberStatus.Creator)
        {
            replyText = Resources.OnlyAdminsAreAllowed;
        }
        else
        {
            // Parse message
            var ticketDescription = Message!.Text!
                .Replace("/addticket", "")
                .Trim();

            if (ticketDescription != "")
            {
                var ticketManager = new TicketManager();
                await ticketManager.AddTicket(ChatId, ticketDescription);
                replyText = string.Format(Resources.TicketAdded, ticketDescription);
            }
            else
            {
                replyText = Resources.NeedToDefineTicket;
            }
        }

        var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, parseMode: ParseMode.Markdown);

        await _delayService.DelaySeconds(30);
        await BotClient.TryDeleteMessage(Message);
        await BotClient.TryDeleteMessage(msg);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("^/removeticket")]
    public async Task RemoveTicket()
    {
        var replyText = "";

        // Сheck if user have rights to scan
        var usrSenderRights = await BotClient.GetChatMemberAsync(ChatId, Message.From!.Id);
        if (usrSenderRights.Status != ChatMemberStatus.Administrator && usrSenderRights.Status != ChatMemberStatus.Creator)
        {
            replyText = Resources.OnlyAdminsAreAllowed;
        }
        else
        {
            // Parse message
            var ticketIdString = Message!.Text!
                .Replace("/removeticket", "")
                .Trim();

            if (ticketIdString == "")
            {
                replyText = Resources.WhereIsTicketNumber;
            }
            else
            {
                if (long.TryParse(ticketIdString, out var ticketId))
                {
                    var ticketManager = new TicketManager();

                    var removeResult = await ticketManager.RemoveTicket(ChatId, ticketId);

                    if (removeResult)
                        replyText = string.Format(Resources.TicketDeleted, ticketId);
                    else
                        replyText = Resources.HackerInTheChat;
                }
                else
                {
                    replyText = Resources.AreYouThinkImThatDumb;
                }
            }
        }

        var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, parseMode: ParseMode.Markdown);

        await _delayService.DelaySeconds(30);
        await BotClient.TryDeleteMessage(Message);
        await BotClient.TryDeleteMessage(msg);
    }

    [MessageReaction(ChatAction.UploadPhoto)]
    [MessageHandler("(^/cat$|^cat$|^кіт$|^кицька$)")]
    public async Task Cat()
    {
        var carUrl = await _catService.GetRandomCatImageUrl();

        if (carUrl == null)
        {
            var msg = await BotClient.SendTextMessageAsync(chatId: ChatId, text: Resources.GoneAway, replyToMessageId: Message.MessageId);

            await _delayService.DelaySeconds(30);
            await BotClient.TryDeleteMessage(msg);
            await BotClient.TryDeleteMessage(Message);

            return;
        }

        // Random cat gender
        List<string> variants = Resources.RandomCatGenders
            .Split("|")
            .PickRandom(2)
            .ToList();

        var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("Кіт", $"print|{variants[0]}"),
            InlineKeyboardButton.WithCallbackData("Кітесса", $"print|{variants[1]}"),
        });

        await BotClient.SendPhotoAsync(chatId: ChatId, photo: InputFile.FromUri(carUrl), replyToMessageId: Message.MessageId, replyMarkup: keyboard);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("^кіт ")]
    public async Task ChatGptAsk()
    {
        var chatSettings = await _chatSettingsService.GetChatSettings(ChatId);
        if (chatSettings == null || !chatSettings!.UseChatGpt)
        {
            return;
        }
            
        var inputMessageTest = Message!.Text!.Replace("кіт ", "").Replace("Кіт ", "");
        var returnMessage = ":)";

        if (String.IsNullOrEmpty(inputMessageTest))
        {
            returnMessage = Resources.Empty;
        }

        try
        {
            var api = new OpenAIAPI(new APIAuthentication(Environment.GetEnvironmentVariable("RUDEBOT_OPENAI_API_KEY")!));
            var result = await api.Completions.CreateCompletionAsync(inputMessageTest, max_tokens: 50, temperature: 0.0);
            returnMessage = result.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            returnMessage = Resources.OopsIDidntAgain;
        }

        await BotClient.SendTextMessageAsync(ChatId, returnMessage, replyToMessageId: Message.MessageId);
    }

    [MessageReaction(ChatAction.Typing)]
    [MessageHandler("^/give")]
    public async Task Give()
    {
        var replyText = "";
            
        var transactionRequestError = TransactionArgsValidator.CheckTransactionRequestMessage(Message, User);
           
        if (!string.IsNullOrWhiteSpace(transactionRequestError))
        {
            replyText = transactionRequestError;
        }
        else
        {
            // Parse args
            var amountStr = Message!.Text!.Split(" ").Last();
            var amount = int.Parse(amountStr);

            var userSender = await _userManager.GetUserChatStats(User.Id, ChatId);
            var userReceiver = await _userManager.GetUserChatStats(Message.ReplyToMessage!.From!.Id, ChatId);

            replyText = await _userManager.RudeCoinsTransaction(userSender, userReceiver, amount);
        }

        var msg = await BotClient.SendTextMessageAsync(ChatId, replyText, parseMode: ParseMode.Markdown);

        await _delayService.DelaySeconds(30);

        await BotClient.TryDeleteMessage(Message);
        await BotClient.TryDeleteMessage(msg);
    }
        
    [MessageTypeFilter(MessageType.Text)]
    public async Task MessageTrigger()
    {
        if (Message.Text != null)
        {
            var replyText = "";
            var random = new Random();
            var sendRandomMessage = (random.Next(1, 1000) > 985);
                
            var chatSettings = await _chatSettingsService.GetChatSettings(ChatId);
                
            if (!Message.IsCommand() && 
                (Message?.ReplyToMessage?.From?.Id == BotClient.BotId ||
                 (sendRandomMessage && chatSettings.SendRandomMessages)))
            {
                var advices = AdvicesService.GetWords();
                replyText = advices.PickRandom();
            }

            if (!string.IsNullOrEmpty(replyText))
            {
                var isReply = (random.Next(100) > 50);
                await BotClient.SendTextMessageAsync(ChatId, replyText, replyToMessageId: isReply ? Message!.MessageId : null);
            }
        }
    }
}