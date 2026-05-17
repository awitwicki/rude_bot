using PowerBot.Lite.Attributes;
using RudeBot.Services;

namespace RudeBot.Filters;

public class HaterussianLangEnabledFilter : BaseHandlerFilter
{
    private readonly IChatSettingsService _chatSettingsService;

    public HaterussianLangEnabledFilter(IChatSettingsService chatSettingsService)
    {
        _chatSettingsService = chatSettingsService;
    }

    protected override async Task<bool> FilterAsync()
    {
        var chatSettings = await _chatSettingsService.GetChatSettings(ChatId);
        return chatSettings.HaterussianLang;
    }
}
