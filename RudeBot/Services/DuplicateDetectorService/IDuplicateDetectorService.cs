namespace RudeBot.Services.DuplicateDetectorService
{
    public interface IDuplicateDetectorService
    {
        List<int> FindDuplicates(long chatId, int messageId, string text);
    }
}
