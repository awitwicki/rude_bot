using RudeBot.Models;

namespace RudeBot.Services;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message);
    Task<List<ChatMessage>> GetLastNAsync(long chatId, int count, bool sanitizeCommands = true);
    Task<List<ChatMessage>> GetSinceAsync(long chatId, DateTime since);
    Task<List<ChatMessage>> GetLastNByUserAsync(long chatId, long userId, int count, bool sanitizeCommands = true);
    Task<List<ChatMessage>> SearchAsync(long chatId, string query, int count, bool sanitizeCommands = true);
}
