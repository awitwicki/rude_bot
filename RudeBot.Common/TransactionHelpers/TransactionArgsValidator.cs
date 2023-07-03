
using Telegram.Bot.Types;
using RudeBot.Domain.Resources;

namespace RudeBot.Common.TransactionHelpers;

public static class TransactionArgsValidator
{
    public static string CheckTransactionRequestMessage(Message message, User userSender)
    {
        // Parse args
        var amountStr = message!.Text!.Split(" ").LastOrDefault();
            
        if (message.ReplyToMessage == null)
        {
            return Resources.ShouldBeReplyToMessage;
        }
            
        if (message.ReplyToMessage!.From!.Id == userSender.Id)
        {
            return Resources.CantSendToYourself;
        }
            
        if (message.ReplyToMessage.From.IsBot)
        {
            return Resources.CantSendToBots;
        }

        if (!int.TryParse(amountStr, out var amount))
        {
            return Resources.CanGiveOnlyNumbersArg;
        }

        return string.Empty;
    }
}
