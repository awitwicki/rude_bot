using RudeBot.Models;

namespace RudeBot.Services.ChatDigestService;

public interface IChatDigestSummaryGenerator
{
    Task<string> GenerateSummary(List<ChatMessage> messages);
}
