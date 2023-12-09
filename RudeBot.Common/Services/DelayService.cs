using RudeBot.Domain.Interfaces;

namespace RudeBot.Common.Services;

public class DelayService : IDelayService
{
    public Task DelaySeconds(int seconds)
    {
        return Task.Delay(seconds * 1000);
    }
}
