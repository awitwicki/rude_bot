namespace RudeBot.Services.DuplicateDetectorService
{
    public class DuplicateDetectorMessageDescriptor
    {
        public DateTime Expires;
        public int MessageId;
        public string Text;

        public bool IsEquals(string text)
        {
            return Text == text;
        }
    }
}
