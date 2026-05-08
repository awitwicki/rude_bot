using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Services.UserProfileService;

namespace RudeBot.Tests;

public class UserProfileServiceTests
{
    private static DataContext NewContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DataContext(options);
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        await using var ctx = NewContext();
        var svc = new UserProfileService(ctx);

        var result = await svc.GetAsync(1, 42);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_NewRow_Inserts()
    {
        await using var ctx = NewContext();
        var svc = new UserProfileService(ctx);

        await svc.UpsertAsync(1, 42, "alice", "Має пса.");

        var stored = await svc.GetAsync(1, 42);
        Assert.NotNull(stored);
        Assert.Equal("alice", stored!.UserName);
        Assert.Equal("Має пса.", stored.Profile);
        Assert.True((DateTime.UtcNow - stored.UpdatedAt).TotalSeconds < 5);
    }

    [Fact]
    public async Task UpsertAsync_ExistingRow_UpdatesAndBumpsUpdatedAt()
    {
        await using var ctx = NewContext();
        var svc = new UserProfileService(ctx);

        await svc.UpsertAsync(1, 42, "alice", "v1");
        var first = await svc.GetAsync(1, 42);
        var firstUpdated = first!.UpdatedAt;

        await Task.Delay(10);
        await svc.UpsertAsync(1, 42, "alice_new", "v2");

        var second = await svc.GetAsync(1, 42);
        Assert.Equal("alice_new", second!.UserName);
        Assert.Equal("v2", second.Profile);
        Assert.True(second.UpdatedAt > firstUpdated);

        // Still only one row for this (chatId, userId).
        Assert.Equal(1, await ctx.UserChatProfiles.CountAsync(p => p.ChatId == 1 && p.UserId == 42));
    }

    [Fact]
    public async Task ListForChatAsync_ReturnsOnlyMatchingChat_OrderedByUpdatedAtDesc()
    {
        await using var ctx = NewContext();
        var svc = new UserProfileService(ctx);

        await svc.UpsertAsync(1, 10, "older", "p1");
        await Task.Delay(10);
        await svc.UpsertAsync(1, 20, "newer", "p2");
        await svc.UpsertAsync(2, 99, "otherchat", "p3");

        var result = await svc.ListForChatAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Equal(20, result[0].UserId);
        Assert.Equal(10, result[1].UserId);
    }
}
