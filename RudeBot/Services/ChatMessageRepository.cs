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

    public async Task<List<ChatMessage>> GetLastNAsync(long chatId, int count, bool sanitizeCommands = true)
    {
        var query = _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId);

        if (sanitizeCommands)
        {
            query = query.Where(m => !EF.Functions.Like(m.Text, "/%"));
        }

        var rows = await query
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
