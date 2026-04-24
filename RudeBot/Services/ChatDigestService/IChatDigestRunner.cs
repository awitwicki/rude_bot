namespace RudeBot.Services.ChatDigestService;

public enum ChatDigestResult
{
    Posted,
    Empty,
    GenerationFailed
}

public interface IChatDigestRunner
{
    Task<ChatDigestResult> RunForChat(long chatId);
}
