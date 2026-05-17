using PowerBot.Lite.Attributes;

namespace RudeBot.Filters;

public class NotForwardedFromOthersFilter : BaseHandlerFilter
{
    protected override Task<bool> FilterAsync()
    {
        if (Message.ForwardFromChat != null)
            return Task.FromResult(false);

        if (Message.ForwardFrom != null && Message.ForwardFrom.Id != User.Id)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}
