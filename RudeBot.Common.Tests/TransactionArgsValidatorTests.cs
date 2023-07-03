using RudeBot.Common.TransactionHelpers;
using RudeBot.Domain.Resources;
using Telegram.Bot.Types;

namespace RudeBot.Common.Tests;

public class TransactionArgsValidatorTests
{
    [Fact]
    public void CheckTransactionRequestMessage_WithoutReplyToMessage_ShouldReturnShouldBeReplyToMessageText()
    {
        // Arrange
        var message = new Message {Text = "/give ipsum"};
        
        // Act
        var result = TransactionArgsValidator.CheckTransactionRequestMessage(message, null!);
        
        // Assert
        Assert.Equal(Resources.ShouldBeReplyToMessage, result);
    }
    
    [Fact]
    public void CheckTransactionRequestMessage_WithoutReplyToMessage_ShouldReturnCanGiveOnlyNumbersArgText()
    {
        // Arrange
        var userSender = new User { Id = 1};
        var replyToMessageAuthor = new User();
        var replyToMessage = new Message {Text = "Lorem ipsum", From = replyToMessageAuthor};
        var message = new Message {Text = "/give ipsum", ReplyToMessage = replyToMessage, From = userSender};
        
        // Act
        var result = TransactionArgsValidator.CheckTransactionRequestMessage(message, userSender);
        
        // Assert
        Assert.Equal(Resources.CanGiveOnlyNumbersArg, result);
    }
    
    [Fact]
    public void CheckTransactionRequestMessage_WithoutReplyToMessage_ShouldReturnCantSendToYourselfText()
    {
        // Arrange
        var userSender = new User { Id = 1};
        var replyToMessage = new Message {Text = "Lorem ipsum", From = userSender};
        var message = new Message {Text = "/give 4", From = userSender, ReplyToMessage = replyToMessage};
        
        // Act
        var result = TransactionArgsValidator.CheckTransactionRequestMessage(message, userSender);
        
        // Assert
        Assert.Equal(Resources.CantSendToYourself, result);
    }
    
    [Fact]
    public void CheckTransactionRequestMessage_WithoutReplyToMessage_ShouldReturnCantSendToBotsText()
    {
        // Arrange
        var userSender = new User { Id = 1};
        var replyToMessageAuthor = new User { IsBot = true};
        var replyToMessage = new Message {Text = "Lorem ipsum", From = replyToMessageAuthor};
        var message = new Message {Text = "/give 5", ReplyToMessage = replyToMessage};
        
        // Act
        var result = TransactionArgsValidator.CheckTransactionRequestMessage(message, userSender);
        
        // Assert
        Assert.Equal(Resources.CantSendToBots, result);
    }
    
    [Fact]
    public void CheckTransactionRequestMessage_WithoutReplyToMessage_ShouldReturnEmptyString()
    {
        // Arrange
        var userSender = new User { Id = 1};
        var replyToMessageAuthor = new User { Id = 2};
        var replyToMessage = new Message {Text = "Lorem ipsum", From = replyToMessageAuthor};
        var message = new Message {Text = "/give 4", ReplyToMessage = replyToMessage};

        
        // Act
        var result = TransactionArgsValidator.CheckTransactionRequestMessage(message, userSender);
        
        // Assert
        Assert.Equal(string.Empty, result);
    }
}
