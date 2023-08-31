using RudeBot.Models;

namespace RudeBot.Services;

public interface IChatSettingsService
{
    Task LoadAllChatSettings();
    Task<ChatSettings> AddOrUpdateChatSettings(ChatSettings settings);
    Task<ChatSettings> GetChatSettings(long chatId);
}