using NSubstitute;
using RudeBot.Filters;
using RudeBot.Models;
using RudeBot.Services;
using Telegram.Bot.Types;

namespace RudeBot.Tests.FiltersTests;

public class PotuzhnistEnabledFilterTests
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
            .Returns(new ChatSettings { Potuzhnist = true });

        var filter = new PotuzhnistEnabledFilter(chatSettings);

        Assert.True(await filter.FilterAsync(BuildUpdate()));
    }

    [Fact]
    public async Task FilterAsync_SettingOff_ReturnsFalse()
    {
        var chatSettings = Substitute.For<IChatSettingsService>();
        chatSettings.GetChatSettings(Arg.Any<long>())
            .Returns(new ChatSettings { Potuzhnist = false });

        var filter = new PotuzhnistEnabledFilter(chatSettings);

        Assert.False(await filter.FilterAsync(BuildUpdate()));
    }
}
