using NSubstitute;
using RudeBot.Domain.Interfaces;
using RudeBot.Handlers;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.ChatContextService;
using RudeBot.Services.UserProfileService;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace RudeBot.Tests.HandlersTests;

public class BotHandlerTests
{
    private readonly IUserManager _userManager;
    private readonly IChatSettingsService _chatSettingsService;
    private readonly ITeslaChatCounterService _teslaChatCounterService;
    private readonly ICatService _catService;
    private readonly ITxtWordsDataset _advicesService;
    private readonly IDelayService _delayService;
    private readonly IChatContextService _chatContextService;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IUserProfileService _userProfileService;
    private readonly ITelegramBotClient _telegramBotClient;

    public BotHandlerTests()
    {
        _userManager = Substitute.For<IUserManager>();
        _chatSettingsService = Substitute.For<IChatSettingsService>();
        _teslaChatCounterService = Substitute.For<ITeslaChatCounterService>();
        _catService = Substitute.For<ICatService>();
        _advicesService = Substitute.For<ITxtWordsDataset>();
        _delayService = Substitute.For<IDelayService>();
        _chatContextService = Substitute.For<IChatContextService>();
        _chatMessageRepository = Substitute.For<IChatMessageRepository>();
        _userProfileService = Substitute.For<IUserProfileService>();
        _userProfileService.ListForChatAsync(Arg.Any<long>())
            .Returns(new List<RudeBot.Models.UserChatProfile>());
        _userProfileService.GetAsync(Arg.Any<long>(), Arg.Any<long>())
            .Returns((RudeBot.Models.UserChatProfile?)null);
        _telegramBotClient = Substitute.For<ITelegramBotClient>();

        _delayService.DelaySeconds(Arg.Any<int>())
            .Returns(Task.Delay(1));
    }
    
    [Fact]
    public async Task HandlePalanytsa_WithSettingsHaterussianLangAndForwardOtherMessage_ShouldDoNothing()
    {
        // Arrange
        _chatSettingsService.GetChatSettings(Arg.Any<long>())
            .Returns(Task.FromResult(new ChatSettings { HaterussianLang = true }));

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 1, Chat = new Chat { Id = 1 },
                    From = new User { Id = 1 },
                    ForwardOrigin = new MessageOriginUser { SenderUser = new User { Id = 2 }}
                }
            }
        };

        // Act
        await handler.Palanytsa();

        // Assert
        await _telegramBotClient.DidNotReceive().SendRequest(
            Arg.Any<SendAnimationRequest>()
        );
    }
    
    [Fact]
    public async Task HandlePalanytsa_WithSettingsHaterussianLangAndForwardSelfMessage_ShouldNotifyAboutChatRules()
    {
        // Arrange
        _chatSettingsService.GetChatSettings(Arg.Any<long>())
            .Returns(Task.FromResult(new ChatSettings { HaterussianLang = true }));

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 1, Chat = new Chat { Id = 1 },
                    From = new User { Id = 1 },
                    ForwardOrigin = new MessageOriginUser { SenderUser = new User { Id = 1 }}
                }
            }
        };

        // Act
        await handler.Palanytsa();

        // Assert
        await _telegramBotClient.Received().SendRequest(
            Arg.Any<SendAnimationRequest>()
        );
    }
    
    [Fact]
    public async Task HandlePalanytsa_WithSettingsHaterussianLangAndForwardFromOtherChannelOrChat_ShouldDoNothing()
    {
        // Arrange
        _chatSettingsService.GetChatSettings(Arg.Any<long>())
            .Returns(Task.FromResult(new ChatSettings { HaterussianLang = true }));

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 1, Chat = new Chat { Id = 1 },
                    From = new User { Id = 1 },
                    ForwardOrigin = new MessageOriginUser { SenderUser = new User { Id = 2 }}
                }
            }
        };

        // Act
        await handler.Palanytsa();

        // Assert
        await _telegramBotClient.DidNotReceive().SendRequest(
            Arg.Any<SendAnimationRequest>()
        );
    }
    
    [Fact]
    public async Task HandlePalanytsa_WithSettingsHaterussianLang_ShouldNotifyAboutChatRules()
    {
        // Arrange
        _chatSettingsService.GetChatSettings(Arg.Any<long>())
            .Returns(Task.FromResult(new ChatSettings { HaterussianLang = true }));

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 1, Chat = new Chat { Id = 1 },
                    From = new User { Id = 1 }
                }
            }
        };

        // Act
        await handler.Palanytsa();

        // Assert
        await _telegramBotClient.Received().SendRequest(
            Arg.Any<SendAnimationRequest>()
        );
    }
    
    [Fact]
    public async Task HandlePalanytsa_WithoutSettingsHaterussianLang_ShouldDoNothing()
    {
        // Arrange
        _chatSettingsService.GetChatSettings(Arg.Any<long>())
            .Returns(Task.FromResult(new ChatSettings { HaterussianLang = false }));

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 1, Chat = new Chat { Id = 1 },
                    From = new User { Id = 1 }
                }
            }
        };

        // Act
        await handler.Palanytsa();

        // Assert
        await _telegramBotClient.DidNotReceive().SendRequest(
            Arg.Any<SendAnimationRequest>()
        );
    }

    [Fact]
    public async Task Karma_NoProfileExists_RepliesWithStatsOnly()
    {
        _userManager.GetUserChatStats(42, 1).Returns(new UserChatStats
        {
            Id = 42,
            ChatId = 1,
            UserId = 42,
            User = new TelegramUser { Id = 42, UserMention = "@alice", UserName = "alice" },
            Karma = 5,
            Warns = 0,
            TotalMessages = 100,
            TotalBadWords = 3,
        });
        _userProfileService.GetAsync(1, 42).Returns((RudeBot.Models.UserChatProfile?)null);

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 7, Chat = new Chat { Id = 1 },
                    From = new User { Id = 42 },
                    Text = "карма"
                }
            }
        };

        await handler.Karma();

        await _telegramBotClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r =>
                r.Text.Contains("@alice") &&
                r.Text.Contains("Карма") &&
                !r.Text.Contains(RudeBot.Domain.Resources.Resources.MyProfileEmpty)));
    }

    [Fact]
    public async Task Karma_ProfileExists_RepliesWithStatsAndProfile()
    {
        _userManager.GetUserChatStats(42, 1).Returns(new UserChatStats
        {
            Id = 42,
            ChatId = 1,
            UserId = 42,
            User = new TelegramUser { Id = 42, UserMention = "@alice", UserName = "alice" },
            Karma = 5,
            Warns = 0,
            TotalMessages = 100,
            TotalBadWords = 3,
        });
        _userProfileService.GetAsync(1, 42).Returns(new RudeBot.Models.UserChatProfile
        {
            ChatId = 1,
            UserId = 42,
            UserName = "alice",
            Profile = "Має пса Бобіка.",
            UpdatedAt = new DateTime(2026, 5, 7),
        });

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 7, Chat = new Chat { Id = 1 },
                    From = new User { Id = 42 },
                    Text = "карма"
                }
            }
        };

        await handler.Karma();

        await _telegramBotClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r =>
                r.Text.Contains("@alice") &&
                r.Text.Contains("Карма") &&
                r.Text.Contains("Має пса Бобіка.") &&
                r.Text.Contains("07.05.2026")));
    }

    [Fact]
    public async Task Karma_ProfileWithMarkdownChars_EscapesThemBeforeSending()
    {
        _userManager.GetUserChatStats(42, 1).Returns(new UserChatStats
        {
            Id = 42,
            ChatId = 1,
            UserId = 42,
            User = new TelegramUser { Id = 42, UserMention = "@alice", UserName = "alice" },
            Karma = 0,
            Warns = 0,
            TotalMessages = 1,
            TotalBadWords = 0,
        });
        _userProfileService.GetAsync(1, 42).Returns(new RudeBot.Models.UserChatProfile
        {
            ChatId = 1,
            UserId = 42,
            UserName = "alice",
            Profile = "Любить _підкреслення_ і *зірочки* та `код` і [дужки].",
            UpdatedAt = new DateTime(2026, 5, 7),
        });

        var handler = new BotHandler(_userManager,
            _chatSettingsService,
            _teslaChatCounterService,
            _catService,
            _advicesService,
            _delayService,
            _chatContextService,
            _chatMessageRepository,
            _userProfileService)
        {
            BotClient = _telegramBotClient,
            Update = new Update {
                Id = 1,
                Message = new Message {
                    Id = 7, Chat = new Chat { Id = 1 },
                    From = new User { Id = 42 },
                    Text = "карма"
                }
            }
        };

        await handler.Karma();

        await _telegramBotClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r =>
                r.Text.Contains("\\_підкреслення\\_") &&
                r.Text.Contains("\\*зірочки\\*") &&
                r.Text.Contains("\\`код\\`") &&
                r.Text.Contains("\\[дужки]")));
    }
}
