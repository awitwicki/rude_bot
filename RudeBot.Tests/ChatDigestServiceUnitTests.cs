using RudeBot.Services.ChatDigestService;

namespace RudeBot.Tests;

public class ChatDigestServiceUnitTests
{
    [Fact]
    public void AddMessage_SingleMessage_ShouldBeRetrievable()
    {
        // Arrange
        var service = new ChatDigestService();

        // Act
        service.AddMessage(1, "user1", "hello");
        var messages = service.GetAndClearMessages(1);

        // Assert
        Assert.Single(messages);
        Assert.Equal("user1", messages[0].UserName);
        Assert.Equal("hello", messages[0].Text);
    }

    [Fact]
    public void AddMessage_MultipleMessages_ShouldPreserveOrder()
    {
        // Arrange
        var service = new ChatDigestService();

        // Act
        service.AddMessage(1, "user1", "first");
        service.AddMessage(1, "user2", "second");
        service.AddMessage(1, "user1", "third");
        var messages = service.GetAndClearMessages(1);

        // Assert
        Assert.Equal(3, messages.Count);
        Assert.Equal("first", messages[0].Text);
        Assert.Equal("second", messages[1].Text);
        Assert.Equal("third", messages[2].Text);
    }

    [Fact]
    public void AddMessage_DifferentChats_ShouldBeIsolated()
    {
        // Arrange
        var service = new ChatDigestService();

        // Act
        service.AddMessage(1, "user1", "chat1 message");
        service.AddMessage(2, "user2", "chat2 message");
        var messages1 = service.GetAndClearMessages(1);
        var messages2 = service.GetAndClearMessages(2);

        // Assert
        Assert.Single(messages1);
        Assert.Equal("chat1 message", messages1[0].Text);
        Assert.Single(messages2);
        Assert.Equal("chat2 message", messages2[0].Text);
    }

    [Fact]
    public void GetAndClearMessages_ShouldClearAfterRetrieval()
    {
        // Arrange
        var service = new ChatDigestService();
        service.AddMessage(1, "user1", "hello");

        // Act
        var first = service.GetAndClearMessages(1);
        var second = service.GetAndClearMessages(1);

        // Assert
        Assert.Single(first);
        Assert.Empty(second);
    }

    [Fact]
    public void GetAndClearMessages_NonExistentChat_ShouldReturnEmptyList()
    {
        // Arrange
        var service = new ChatDigestService();

        // Act
        var messages = service.GetAndClearMessages(999);

        // Assert
        Assert.Empty(messages);
    }

    [Fact]
    public void GetAndClearMessages_ShouldNotAffectOtherChats()
    {
        // Arrange
        var service = new ChatDigestService();
        service.AddMessage(1, "user1", "chat1");
        service.AddMessage(2, "user2", "chat2");

        // Act
        service.GetAndClearMessages(1);
        var messages2 = service.GetAndClearMessages(2);

        // Assert
        Assert.Single(messages2);
        Assert.Equal("chat2", messages2[0].Text);
    }

    [Fact]
    public void GetActiveChatIds_ShouldReturnAllChatsWithMessages()
    {
        // Arrange
        var service = new ChatDigestService();
        service.AddMessage(10, "user1", "msg");
        service.AddMessage(20, "user2", "msg");
        service.AddMessage(30, "user3", "msg");

        // Act
        var chatIds = service.GetActiveChatIds().ToList();

        // Assert
        Assert.Equal(3, chatIds.Count);
        Assert.Contains(10L, chatIds);
        Assert.Contains(20L, chatIds);
        Assert.Contains(30L, chatIds);
    }

    [Fact]
    public void GetActiveChatIds_EmptyService_ShouldReturnEmptyList()
    {
        // Arrange
        var service = new ChatDigestService();

        // Act
        var chatIds = service.GetActiveChatIds().ToList();

        // Assert
        Assert.Empty(chatIds);
    }

    [Fact]
    public void GetActiveChatIds_AfterClear_ShouldStillContainChatId()
    {
        // Arrange
        var service = new ChatDigestService();
        service.AddMessage(1, "user1", "msg");

        // Act
        service.GetAndClearMessages(1);
        var chatIds = service.GetActiveChatIds().ToList();

        // Assert
        // Chat ID remains in cache (with empty list) after clear
        Assert.Contains(1L, chatIds);
    }

    [Fact]
    public void AddMessage_ShouldSetTimestamp()
    {
        // Arrange
        var service = new ChatDigestService();
        var before = DateTime.UtcNow;

        // Act
        service.AddMessage(1, "user1", "hello");
        var after = DateTime.UtcNow;
        var messages = service.GetAndClearMessages(1);

        // Assert
        Assert.InRange(messages[0].Timestamp, before, after);
    }

    [Fact]
    public void AddMessage_NoMessageLimit_ShouldAcceptManyMessages()
    {
        // Arrange
        var service = new ChatDigestService();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            service.AddMessage(1, "user1", $"message {i}");
        }

        var messages = service.GetAndClearMessages(1);

        // Assert
        Assert.Equal(1000, messages.Count);
    }
}
