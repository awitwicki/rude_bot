using RudeBot.Domain.Interfaces;

namespace RudeBot.Services;

public class AllowedChatsService : IAllowedChatsService
{
    private readonly HashSet<long> _allowedChats;
        
    public AllowedChatsService(string input)
    {
        _allowedChats = new HashSet<long>();
        
        if (!string.IsNullOrEmpty(input))
        {
            var chatIdList = input.Split(",");
            foreach (var chatIdStr in chatIdList)
            {
                var chatId = long.Parse(chatIdStr);
                _allowedChats.Add(chatId);
            }
        }
    }

    public bool IsChatAllowed(long chatId)
    {
        return _allowedChats.Count == 0 || _allowedChats.Contains(chatId);
    }
}
