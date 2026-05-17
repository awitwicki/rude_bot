using RudeBot.Filters;
using Telegram.Bot.Types;

namespace RudeBot.Tests.FiltersTests;

public class ReplyToOtherUserFilterTests
{
    private static Update MakeUpdate(Message? replyTo) => new()
    {
        Id = 1,
        Message = new Message
        {
            Id = 1,
            Chat = new Chat { Id = 1 },
            From = new User { Id = 1 },
            ReplyToMessage = replyTo!
        }
    };

    [Fact]
    public async Task FilterAsync_NoReply_ReturnsFalse()
    {
        var filter = new ReplyToOtherUserFilter();
        Assert.False(await filter.FilterAsync(MakeUpdate(null)));
    }

    [Fact]
    public async Task FilterAsync_ReplyToSelf_ReturnsFalse()
    {
        var filter = new ReplyToOtherUserFilter();
        var update = MakeUpdate(new Message { Id = 99, Chat = new Chat { Id = 1 }, From = new User { Id = 1 } });
        Assert.False(await filter.FilterAsync(update));
    }

    [Fact]
    public async Task FilterAsync_ReplyToBot_ReturnsFalse()
    {
        var filter = new ReplyToOtherUserFilter();
        var update = MakeUpdate(new Message { Id = 99, Chat = new Chat { Id = 1 }, From = new User { Id = 7, IsBot = true } });
        Assert.False(await filter.FilterAsync(update));
    }

    [Fact]
    public async Task FilterAsync_ReplyToOtherHuman_ReturnsTrue()
    {
        var filter = new ReplyToOtherUserFilter();
        var update = MakeUpdate(new Message { Id = 99, Chat = new Chat { Id = 1 }, From = new User { Id = 2 } });
        Assert.True(await filter.FilterAsync(update));
    }
}
