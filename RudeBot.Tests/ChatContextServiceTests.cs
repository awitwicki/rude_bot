using NSubstitute;
using RudeBot.Models;
using RudeBot.Services;
using RudeBot.Services.ChatContextService;

namespace RudeBot.Tests;

public class ChatContextServiceTests
{
    [Fact]
    public async Task GetMessagesAsync_ReturnsRepoRowsMappedToContextMessages()
    {
        var repo = Substitute.For<IChatMessageRepository>();
        var rows = new List<ChatMessage>
        {
            new() { UserName = "a", Text = "hi", CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { UserName = "b", Text = "yo", CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
        };
        repo.GetLastNAsync(42, 10).Returns(rows);

        var service = new ChatContextService(repo);

        var result = await service.GetMessagesAsync(42);

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].UserName);
        Assert.Equal("hi", result[0].Text);
        Assert.Equal("b", result[1].UserName);
        Assert.Equal("yo", result[1].Text);
    }

    [Fact]
    public async Task GetMessagesAsync_PassesWindowSizeToRepo()
    {
        var repo = Substitute.For<IChatMessageRepository>();
        repo.GetLastNAsync(Arg.Any<long>(), Arg.Any<int>()).Returns(new List<ChatMessage>());

        var service = new ChatContextService(repo);
        await service.GetMessagesAsync(7);

        await repo.Received(1).GetLastNAsync(7, 10);
    }
}
