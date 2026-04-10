using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;

namespace RudeBot.Services;

public class ChatSettingsService : IChatSettingsService
{
    private readonly ConcurrentDictionary<long, ChatSettings> _chatSettingsCache = new();
    private readonly DbContextOptions<DataContext> _dbContextOptions;

    public ChatSettingsService(DbContextOptions<DataContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions;
    }

    public async Task LoadAllChatSettings()
    {
        await using var dbContext = new DataContext(_dbContextOptions);

        var settings = await dbContext.ChatSettings.AsNoTracking()
            .ToDictionaryAsync(k => k.ChatId, v => v);

        _chatSettingsCache.Clear();
        foreach (var kvp in settings)
        {
            _chatSettingsCache[kvp.Key] = kvp.Value;
        }
    }

    public async Task<ChatSettings> AddOrUpdateChatSettings(ChatSettings settings)
    {
        await using var dbContext = new DataContext(_dbContextOptions);

        if (await dbContext.ChatSettings.AnyAsync(e => e.ChatId == settings.ChatId))
        {
            dbContext.ChatSettings.Update(settings);
        }
        else
        {
            dbContext.ChatSettings.Add(settings);
        }

        await dbContext.SaveChangesAsync();

        // Add or update in cache
        _chatSettingsCache[settings.ChatId] = settings;

        return settings;
    }

    public async Task<ChatSettings> GetChatSettings(long chatId)
    {
        if (_chatSettingsCache.TryGetValue(chatId, out var settings))
        {
            return settings;
        }

        settings = new ChatSettings
        {
            ChatId = chatId
        };

        settings = await AddOrUpdateChatSettings(settings);

        return settings;
    }
}