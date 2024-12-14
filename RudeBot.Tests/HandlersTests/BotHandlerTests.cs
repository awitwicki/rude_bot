using NSubstitute;
using RudeBot.Domain.Interfaces;
using RudeBot.Handlers;
using RudeBot.Managers;
using RudeBot.Models;
using RudeBot.Services;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace RudeBot.Tests.HandlersTests;

public class BotHandlerTests
{
    private readonly IUserManager _userManager;
    private readonly IChatSettingsService _chatSettingsService;
    private readonly ITeslaChatCounterService _teslaChatCounterService;
    private readonly ITickerService _tickerService;
    private readonly ICatService _catService;
    private readonly ITxtWordsDataset _advicesService;
    private readonly IDelayService _delayService;
    private readonly ITelegramBotClient _telegramBotClient;
    
    public BotHandlerTests()
    {
        _userManager = Substitute.For<IUserManager>();
        _chatSettingsService = Substitute.For<IChatSettingsService>();
        _teslaChatCounterService = Substitute.For<ITeslaChatCounterService>();
        _tickerService = Substitute.For<ITickerService>();
        _catService = Substitute.For<ICatService>();
        _advicesService = Substitute.For<ITxtWordsDataset>();
        _delayService = Substitute.For<IDelayService>();
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
            _tickerService,
            _catService,
            _advicesService,
            _delayService)
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
            _tickerService,
            _catService,
            _advicesService,
            _delayService)
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
            _tickerService,
            _catService,
            _advicesService,
            _delayService)
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
            _tickerService,
            _catService,
            _advicesService,
            _delayService)
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
            _tickerService,
            _catService,
            _advicesService,
            _delayService)
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
}
