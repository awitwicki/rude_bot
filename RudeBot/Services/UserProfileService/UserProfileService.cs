using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Models;

namespace RudeBot.Services.UserProfileService;

public class UserProfileService : IUserProfileService
{
    private readonly DataContext _db;

    public UserProfileService(DataContext db)
    {
        _db = db;
    }

    public Task<UserChatProfile?> GetAsync(long chatId, long userId) =>
        _db.UserChatProfiles.FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);

    public Task<List<UserChatProfile>> ListForChatAsync(long chatId) =>
        _db.UserChatProfiles
            .Where(p => p.ChatId == chatId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

    public async Task UpsertAsync(long chatId, long userId, string userName, string profile)
    {
        var existing = await _db.UserChatProfiles
            .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);

        if (existing == null)
        {
            _db.UserChatProfiles.Add(new UserChatProfile
            {
                ChatId = chatId,
                UserId = userId,
                UserName = userName,
                Profile = profile,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.UserName = userName;
            existing.Profile = profile;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
