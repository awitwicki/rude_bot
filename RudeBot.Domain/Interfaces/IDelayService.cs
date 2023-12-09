namespace RudeBot.Domain.Interfaces;

public interface IDelayService
{
    Task DelaySeconds(int seconds);
}
