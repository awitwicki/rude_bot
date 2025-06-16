namespace RudeBot.Domain.Interfaces;

public interface IAllowedChatsService
{
    public bool IsChatAllowed(long chatId);
}
