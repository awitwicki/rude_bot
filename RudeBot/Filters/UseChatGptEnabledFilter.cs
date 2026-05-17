using PowerBot.Lite.Attributes;
using RudeBot.Services;

namespace RudeBot.Filters;

public class UseChatGptEnabledFilter : BaseHandlerFilter
{
    private readonly IChatSettingsService _chatSettingsService;

    public UseChatGptEnabledFilter(IChatSettingsService chatSettingsService)
    {
        _chatSettingsService = chatSettingsService;
    }

    protected override async Task<bool> FilterAsync()
    {
        var chatSettings = await _chatSettingsService.GetChatSettings(ChatId);
        return chatSettings != null && chatSettings.UseChatGpt;
    }
}
