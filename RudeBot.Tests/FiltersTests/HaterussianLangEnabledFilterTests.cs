using NSubstitute;
using RudeBot.Filters;
using RudeBot.Models;
using RudeBot.Services;
using Telegram.Bot.Types;

namespace RudeBot.Tests.FiltersTests;

public class HaterussianLangEnabledFilterTests
{
    private static Update BuildUpdate() => new()
    {
        Id = 1,
        Message = new Message { Id = 1, Chat = new Chat { Id = 1 }, From = new User { Id = 1 } }
    };

    [Fact]
    public async Task FilterAsync_SettingOn_ReturnsTrue()
    {
        var chatSettings = Substitute.For<IChatSettingsService>();
        chatSettings.GetChatSettings(Arg.Any<long>())
            .Returns(new ChatSettings { HaterussianLang = true });

        var filter = new HaterussianLangEnabledFilter(chatSettings);

        Assert.True(await filter.FilterAsync(BuildUpdate()));
    }

    [Fact]
    public async Task FilterAsync_SettingOff_ReturnsFalse()
    {
        var chatSettings = Substitute.For<IChatSettingsService>();
        chatSettings.GetChatSettings(Arg.Any<long>())
            .Returns(new ChatSettings { HaterussianLang = false });

        var filter = new HaterussianLangEnabledFilter(chatSettings);

        Assert.False(await filter.FilterAsync(BuildUpdate()));
    }
}
