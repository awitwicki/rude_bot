﻿namespace RudeBot.Services.DuplicateDetectorService;

public class DuplicateDetectorService : IDuplicateDetectorService
{
    private readonly TimeSpan _expireTime;
    private readonly Dictionary<long, List<DuplicateDetectorMessageDescriptor>> _cache;
        
    private readonly object _lock = new object();

    public DuplicateDetectorService(TimeSpan expireTime)
    {
        _expireTime = expireTime;
        _cache = new Dictionary<long, List<DuplicateDetectorMessageDescriptor>>();
    }

    public List<int> FindDuplicates(long chatId, int messageId, string? text)
    {
        var emptyResult = new List<int>();

        if (text is null || text.Length < 40 || text.StartsWith('/'))
        {
            return emptyResult;
        }

        // Lock for 1 running instance per time
        lock (_lock)
        {
            if (_cache.TryGetValue(chatId, out var descriptors))
            {
                // Remove expired items
                descriptors = descriptors
                    .Where(x => x.Expires > DateTime.UtcNow)
                    .ToList();

                // Try find similar messages
                var similarPosts = descriptors
                    .Where(x => x.IsEquals(text))
                    .ToList();

                var similarMessagesIds = similarPosts
                    .Select(x => x.MessageId)
                    .ToList();

                if (!similarMessagesIds.Any())
                {
                    // Add new chat and message descriptor
                    descriptors.Add(
                        new DuplicateDetectorMessageDescriptor
                        {
                            Text = text,
                            MessageId = messageId,
                            Expires = DateTime.UtcNow + _expireTime
                        }
                    );
                }

                _cache[chatId] = descriptors;

                // Update expire time (not works)
                //similarPosts
                //    .ForEach(x => x.Expires += _expireTime);

                return similarMessagesIds;
            }

            // Add new chat and message descriptor
            descriptors = new List<DuplicateDetectorMessageDescriptor>()
            {
                new()
                {
                    Text = text,
                    MessageId = messageId,
                    Expires = DateTime.UtcNow + _expireTime
                }
            };

            _cache[chatId] = descriptors;
        }

        return emptyResult;
    }
}