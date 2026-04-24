namespace RudeBot.Services.ChatDigestService;

public static class ChatDigestConsts
{
    public const string CronExpression = "0 18 * * *";
    public static readonly TimeSpan Interval = TimeSpan.FromDays(1);
}
