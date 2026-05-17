using PowerBot.Lite.Attributes;

namespace RudeBot.Filters;

public class NotForwardedFilter : BaseHandlerFilter
{
    protected override Task<bool> FilterAsync()
    {
        var notForwarded = Message.ForwardFrom == null && Message.ForwardFromChat == null;
        return Task.FromResult(notForwarded);
    }
}
