namespace RudeBot.Services.DuplicateDetectorService
{
    public class DuplicateDetectorService : IDuplicateDetectorService
    {
        private TimeSpan _expireTime;
        private float _gain;
        private Dictionary<long, List<DuplicateDetectorMessageDescriptor>> _cache;

        public DuplicateDetectorService(TimeSpan expireTime, float gain)
        {
            _expireTime = expireTime;
            _gain = gain;

            _cache = new Dictionary<long, List<DuplicateDetectorMessageDescriptor>>();
        }

        public List<int> FindDuplicates(long chatId, int messageId, string text)
        {
            List<int> epmtyResult = new List<int>();

            if (text is null || text.Length < 40 || text.StartsWith('/'))
            {
                return epmtyResult;
            }

            // Lock for 1 running instance per time
            lock (this)
            {
                if (_cache.TryGetValue(chatId, out var descriptors))
                {
                    // Remove expired items
                    descriptors = descriptors
                        .Where(x => x.Expires > DateTime.UtcNow)
                        .ToList();

                    // Try find similar messages
                    var similarPosts = descriptors
                        .Where(x => x.Equals(text, _gain))
                        .ToList();


                    List<int> similarMessagesIds = similarPosts
                        .Select(x => x.MessageId)
                        .ToList();

                    if (!similarMessagesIds.Any())
                    {
                        // Add new message descriptor
                        // Add nerw chat and message descriptor
                        descriptors.Add(
                            new DuplicateDetectorMessageDescriptor
                            {
                                Text = text,
                                MessageId = messageId,
                                Expires = DateTime.UtcNow + _expireTime
                            });
                        };

                    _cache[chatId] = descriptors;

                    // Update expire time (not works)
                    //similarPosts
                    //    .ForEach(x => x.Expires += _expireTime);

                    return similarMessagesIds;
                }
                else
                {
                    // Add new chat and message descriptor
                    descriptors = new List<DuplicateDetectorMessageDescriptor>() {
                        new DuplicateDetectorMessageDescriptor
                        {
                            Text = text,
                            MessageId = messageId,
                            Expires = DateTime.UtcNow + _expireTime
                        }
                    };

                    _cache[chatId] = descriptors;
                }
            }

            return epmtyResult;
        }
    }
}
