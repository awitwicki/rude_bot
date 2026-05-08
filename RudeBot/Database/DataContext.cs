using Microsoft.EntityFrameworkCore;
using RudeBot.Models;

namespace RudeBot.Database;

public class DataContext : DbContext
{
    public DbSet<TelegramUser> Users { get; set; }
    public DbSet<UserChatStats> UserStats { get; set; }
    public DbSet<ChatSettings> ChatSettings { get; set; }
    public DbSet<TeslaChatCounter> TeslaChatCounters { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<UserChatProfile> UserChatProfiles { get; set; }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }
    
    // Uncomment if you want to create a EF migration
    // public DataContext()
    // {
    //
    // }
    //
    // protected override void OnConfiguring(DbContextOptionsBuilder options)
    // {
    //     options.UseNpgsql("Host=postgres;Username=postgres;Password=rudebotdb;Database=rudebotdb");
    // }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSettings>()
            .Property(p => p.SendRandomMessages)
            .HasDefaultValue(true);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => new { m.ChatId, m.CreatedAt })
            .IsDescending(false, true);

        modelBuilder.Entity<UserChatProfile>()
            .HasIndex(p => new { p.ChatId, p.UserId })
            .IsUnique();

        modelBuilder.Entity<UserChatProfile>()
            .HasIndex(p => p.ChatId);
    }
}
