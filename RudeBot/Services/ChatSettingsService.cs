using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;

namespace RudeBot.Services
{
    public class ChatSettingsService : IChatSettingsService
    {
        private Dictionary<long, ChatSettings> _chatSettingsCache;
        private DataContext _dbContext;

        public ChatSettingsService()
        {
            _dbContext = new DataContext();

            Task.Run(async () => { await LoadAllChatSettings(); }).Wait();
        }
        
        public async Task LoadAllChatSettings()
        {
            _chatSettingsCache = new Dictionary<long, ChatSettings>();

            _chatSettingsCache = await _dbContext.ChatSettings.AsNoTracking()
                .ToDictionaryAsync(k => k.ChatId, v => v);
        }

        public async Task<ChatSettings> AddOrUpdateChatSettings(ChatSettings settings)
        {
            // Add or update in db
            if (_dbContext.ChatSettings.Any(e => e.ChatId == settings.ChatId))
            {
                _dbContext.ChatSettings.Update(settings);
            }
            else
            {
                _dbContext.ChatSettings.Add(settings);
            }

            await _dbContext.SaveChangesAsync();

            // Add or update in cache
            _chatSettingsCache[settings.ChatId] = settings;

            return settings;
        }

        public async Task<ChatSettings> GetChatSettings(long chatId)
        {
            _chatSettingsCache.TryGetValue(chatId, out var settings);

            if (settings == null)
            {
                settings = new ChatSettings
                {
                    ChatId = chatId
                };

                settings = await AddOrUpdateChatSettings(settings);
            }
            
            return settings;
        }
    }
}
