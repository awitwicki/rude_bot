namespace RudeBot.Services.ChatDigestService;

public interface IChatDigestSummaryGenerator
{
    Task<string> GenerateSummary(List<ChatDigestMessage> messages);
}
