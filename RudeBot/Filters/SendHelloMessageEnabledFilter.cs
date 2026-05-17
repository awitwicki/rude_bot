using PowerBot.Lite.Attributes;
using RudeBot.Services;

namespace RudeBot.Filters;

public class SendHelloMessageEnabledFilter : BaseHandlerFilter
{
    private readonly IChatSettingsService _chatSettingsService;

    public SendHelloMessageEnabledFilter(IChatSettingsService chatSettingsService)
    {
        _chatSettingsService = chatSettingsService;
    }

    protected override async Task<bool> FilterAsync()
    {
        var chatSettings = await _chatSettingsService.GetChatSettings(ChatId);
        return chatSettings.SendHelloMessage;
    }
}
