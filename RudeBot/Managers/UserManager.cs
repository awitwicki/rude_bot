using Microsoft.EntityFrameworkCore;
using RudeBot.Database;
using RudeBot.Domain.Resources;
using RudeBot.Models;

namespace RudeBot.Managers;

public class UserManager : IUserManager
{
    private DataContext _dbContext;

    public UserManager(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TelegramUser> CreateUser(TelegramUser user)
    {
        var usr = await _dbContext.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return usr.Entity;
    }

    // TODO: Make GetUser() result cache for scoped DI container?
    public Task<UserChatStats> GetUserChatStats(long userId, long chatId)
    {
        return _dbContext
            .UserStats
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ChatId == chatId)!;
    }

    public async Task<IEnumerable<UserChatStats>> GetAllUsersChatStats(long chatId)
    {
        List<UserChatStats> users = await _dbContext
            .UserStats
            .Include(x => x.User)
            .AsNoTracking()
            .Where(x => x.ChatId == chatId)
            .ToListAsync();

        return users;
    }

    public async Task<UserChatStats> CreateChat(UserChatStats chat)
    {
        var usr = await _dbContext.UserStats.AddAsync(chat);
        await _dbContext.SaveChangesAsync();

        return usr.Entity;
    }

    public async Task<UserChatStats> UpdateUserChatStats(UserChatStats user)
    {
        _dbContext.UserStats.Update(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }
        
    public async Task<string> RudeCoinsTransaction(UserChatStats userSender, UserChatStats userReceiver, int amount)
    {
        if (userSender.RudeCoins < amount)
        {
            return Resources.NotEnoughRudeCoins;
        }
            
        userSender.RudeCoins -= amount;
        userReceiver.RudeCoins += amount;
            
        _dbContext.UserStats.Update(userSender);
        _dbContext.UserStats.Update(userReceiver);

        await _dbContext.SaveChangesAsync();
            
        return string.Format(Resources.RudeCoinsTransactionSuccess, amount, userSender.RudeCoins);
    }
        
    public async Task<UserChatStats> CreateUserChatStats(UserChatStats userChatStats)
    {
        // If user exists then remove object to prevent crating new existing user
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userChatStats.UserId);
        if (user != null)
        {
            userChatStats.User = null;
        }

        // Create new UserStats with new user (optional) in db
        var usr = await _dbContext.AddAsync(userChatStats);
        await _dbContext.SaveChangesAsync();

        return usr.Entity;
    }
}