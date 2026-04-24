using RudeBot.Models;

namespace RudeBot.Services;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message);
    Task<List<ChatMessage>> GetLastNAsync(long chatId, int count);
    Task<List<ChatMessage>> GetSinceAsync(long chatId, DateTime since);
}
