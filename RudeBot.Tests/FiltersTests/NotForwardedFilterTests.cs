using RudeBot.Filters;
using Telegram.Bot.Types;

namespace RudeBot.Tests.FiltersTests;

public class NotForwardedFilterTests
{
    private static Update MakeUpdate(MessageOrigin? origin) => new()
    {
        Id = 1,
        Message = new Message
        {
            Id = 1,
            Chat = new Chat { Id = 1 },
            From = new User { Id = 1 },
            ForwardOrigin = origin!
        }
    };

    [Fact]
    public async Task FilterAsync_NotForwarded_ReturnsTrue()
    {
        var filter = new NotForwardedFilter();
        Assert.True(await filter.FilterAsync(MakeUpdate(null)));
    }

    [Fact]
    public async Task FilterAsync_ForwardedFromUser_ReturnsFalse()
    {
        var filter = new NotForwardedFilter();
        var update = MakeUpdate(new MessageOriginUser { SenderUser = new User { Id = 2 } });
        Assert.False(await filter.FilterAsync(update));
    }

    [Fact]
    public async Task FilterAsync_ForwardedFromChannel_ReturnsFalse()
    {
        var filter = new NotForwardedFilter();
        var update = MakeUpdate(new MessageOriginChannel { Chat = new Chat { Id = 999 }, MessageId = 5 });
        Assert.False(await filter.FilterAsync(update));
    }
}
