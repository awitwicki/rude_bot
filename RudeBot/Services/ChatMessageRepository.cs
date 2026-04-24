using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;

namespace RudeBot.Services;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly DataContext _dbContext;

    public ChatMessageRepository(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ChatMessage message)
    {
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> GetLastNAsync(long chatId, int count)
    {
        var rows = await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();

        rows.Reverse();
        return rows;
    }

    public async Task<List<ChatMessage>> GetSinceAsync(long chatId, DateTime since)
    {
        return await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId && m.CreatedAt > since)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
