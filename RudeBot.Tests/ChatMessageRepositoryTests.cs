using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;
using RudeBot.Services;

namespace RudeBot.Tests;

public class ChatMessageRepositoryTests
{
    private static DataContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new DataContext(options);
    }

    [Fact]
    public async Task AddAsync_PersistsMessage()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);

        var msg = new ChatMessage
        {
            ChatId = 1,
            UserId = 42,
            UserName = "alice",
            Text = "hi",
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(msg);

        Assert.Single(ctx.ChatMessages);
    }

    [Fact]
    public async Task GetLastNAsync_ReturnsLastNInChronologicalOrder()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < 15; i++)
        {
            ctx.ChatMessages.Add(new ChatMessage
            {
                ChatId = 1, UserId = 1, UserName = "u", Text = $"m{i}",
                CreatedAt = t0.AddSeconds(i)
            });
        }
        await ctx.SaveChangesAsync();

        var result = await repo.GetLastNAsync(1, 10);

        Assert.Equal(10, result.Count);
        Assert.Equal("m5", result[0].Text);
        Assert.Equal("m14", result[9].Text);
    }

    [Fact]
    public async Task GetLastNAsync_IgnoresOtherChats()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = DateTime.UtcNow;

        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserName = "u", Text = "a", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 2, UserName = "u", Text = "b", CreatedAt = t0 });
        await ctx.SaveChangesAsync();

        var result = await repo.GetLastNAsync(1, 10);

        Assert.Single(result);
        Assert.Equal("a", result[0].Text);
    }

    [Fact]
    public async Task GetSinceAsync_ReturnsOnlyMessagesAfterCutoff()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserName = "u", Text = "old", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserName = "u", Text = "new1", CreatedAt = t0.AddHours(2) });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserName = "u", Text = "new2", CreatedAt = t0.AddHours(3) });
        await ctx.SaveChangesAsync();

        var result = await repo.GetSinceAsync(1, t0.AddHours(1));

        Assert.Equal(2, result.Count);
        Assert.Equal("new1", result[0].Text);
        Assert.Equal("new2", result[1].Text);
    }

    [Fact]
    public async Task GetLastNByUserAsync_ReturnsOnlyThatUsersMessages_NewestFirst()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "a1", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 8, UserName = "b", Text = "b1", CreatedAt = t0.AddMinutes(1) });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "a2", CreatedAt = t0.AddMinutes(2) });
        await ctx.SaveChangesAsync();

        var result = await repo.GetLastNByUserAsync(1, 7, 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("a2", result[0].Text);
        Assert.Equal("a1", result[1].Text);
    }

    [Fact]
    public async Task GetLastNByUserAsync_SkipsCommandsAndOtherChats()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "/karma", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 2, UserId = 7, UserName = "a", Text = "інший чат", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "нормальне", CreatedAt = t0 });
        await ctx.SaveChangesAsync();

        var result = await repo.GetLastNByUserAsync(1, 7, 10);

        Assert.Single(result);
        Assert.Equal("нормальне", result[0].Text);
    }

    [Fact]
    public async Task GetLastNByUserAsync_RespectsCount()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < 10; i++)
        {
            ctx.ChatMessages.Add(new ChatMessage
            {
                ChatId = 1, UserId = 7, UserName = "a", Text = $"m{i}", CreatedAt = t0.AddSeconds(i)
            });
        }
        await ctx.SaveChangesAsync();

        var result = await repo.GetLastNByUserAsync(1, 7, 3);

        Assert.Equal(3, result.Count);
        Assert.Equal("m9", result[0].Text);
        Assert.Equal("m7", result[2].Text);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive_AndNewestFirst()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "Купив ТЕСЛУ", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 8, UserName = "b", Text = "тесла зарядилась", CreatedAt = t0.AddMinutes(1) });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 9, UserName = "c", Text = "нічого спільного", CreatedAt = t0.AddMinutes(2) });
        await ctx.SaveChangesAsync();

        var result = await repo.SearchAsync(1, "ТеСл", 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("тесла зарядилась", result[0].Text);
        Assert.Equal("Купив ТЕСЛУ", result[1].Text);
    }

    [Fact]
    public async Task SearchAsync_IsScopedToChat_AndSkipsCommands()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.ChatMessages.Add(new ChatMessage { ChatId = 2, UserId = 7, UserName = "a", Text = "тесла в чужому чаті", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "/тесла", CreatedAt = t0 });
        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "тесла тут", CreatedAt = t0 });
        await ctx.SaveChangesAsync();

        var result = await repo.SearchAsync(1, "тесла", 10);

        Assert.Single(result);
        Assert.Equal("тесла тут", result[0].Text);
    }

    [Fact]
    public async Task SearchAsync_TreatsWildcardCharactersLiterally()
    {
        await using var ctx = CreateInMemoryContext();
        var repo = new ChatMessageRepository(ctx);

        ctx.ChatMessages.Add(new ChatMessage { ChatId = 1, UserId = 7, UserName = "a", Text = "будь-що", CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        Assert.Empty(await repo.SearchAsync(1, "%", 10));
        Assert.Empty(await repo.SearchAsync(1, "_", 10));
        Assert.Empty(await repo.SearchAsync(1, "   ", 10));
    }
}
