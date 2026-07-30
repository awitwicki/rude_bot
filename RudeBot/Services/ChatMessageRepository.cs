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

        query = ExcludeCommands(query, sanitizeCommands);

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

    public async Task<List<ChatMessage>> GetLastNByUserAsync(long chatId, long userId, int count, bool sanitizeCommands = true)
    {
        var query = _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId && m.UserId == userId);

        query = ExcludeCommands(query, sanitizeCommands);

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ChatMessage>> SearchAsync(long chatId, string query, int count, bool sanitizeCommands = true)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<ChatMessage>();
        }

        // % і _ — вайлдкарди LIKE; прибираємо їх, щоб запит моделі не перетворився на "знайди все"
        var needle = query.Trim().Replace("%", "").Replace("_", "").ToLower();

        if (needle.Length == 0)
        {
            return new List<ChatMessage>();
        }

        var pattern = $"%{needle}%";

        var rows = _dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .Where(m => EF.Functions.Like(m.Text.ToLower(), pattern));

        rows = ExcludeCommands(rows, sanitizeCommands);

        return await rows
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    private static IQueryable<ChatMessage> ExcludeCommands(IQueryable<ChatMessage> query, bool sanitizeCommands)
    {
        return sanitizeCommands
            ? query.Where(m => !EF.Functions.Like(m.Text, "/%"))
            : query;
    }
}
