using PowerBot.Lite.Attributes;

namespace RudeBot.Filters;

public class ReplyToOtherUserFilter : BaseHandlerFilter
{
    protected override Task<bool> FilterAsync()
    {
        var reply = Message.ReplyToMessage;
        if (reply?.From == null)
            return Task.FromResult(false);

        if (reply.From.Id == User.Id || reply.From.IsBot)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}
