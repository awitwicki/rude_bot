using RudeBot.Models;

namespace RudeBot.Services;

public interface ITeslaChatCounterService
{
    Task<TeslaChatCounter> GetTeslaInChatDate(long chatId);
    Task AddOrUpdateTeslaInChatDate(TeslaChatCounter teslaChatCounter);
}
