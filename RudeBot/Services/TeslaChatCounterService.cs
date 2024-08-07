using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;

namespace RudeBot.Services;

public class TeslaChatCounterService : ITeslaChatCounterService
{
    private Dictionary<long, TeslaChatCounter> _chatTeslaCountersCache;
    private DataContext _dbContext;
    
    public TeslaChatCounterService()
    {
        _dbContext = new DataContext();

        Task.Run(async () => { await LoadAllTeslaCounters(); }).Wait();
    }
    
    private async Task LoadAllTeslaCounters()
    {
        _chatTeslaCountersCache = new Dictionary<long, TeslaChatCounter>();

        _chatTeslaCountersCache = await _dbContext.TeslaChatCounters.AsNoTracking()
            .ToDictionaryAsync(k => k.ChatId, v => v);
    }

    public async Task<TeslaChatCounter> GetTeslaInChatDate(long chatId)
    {
        _chatTeslaCountersCache.TryGetValue(chatId, out var counter);

        return counter!;
    }

    public async Task AddOrUpdateTeslaInChatDate(TeslaChatCounter teslaChatCounter)
    {
        // Add or update in db
        if (_dbContext.TeslaChatCounters.Any(e => e.ChatId == teslaChatCounter.ChatId))
        {
            _dbContext.TeslaChatCounters.Update(teslaChatCounter);
        }
        else
        {
            _dbContext.TeslaChatCounters.Add(teslaChatCounter);
        }

        await _dbContext.SaveChangesAsync();

        // Add or update in cache
        _chatTeslaCountersCache[teslaChatCounter.ChatId] = teslaChatCounter;
    }
}
