using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RudeBot.Models;

namespace RudeBot.Database;

public class DataContext : DbContext
{
    public DbSet<TelegramUser> Users { get; set; }
    public DbSet<UserChatStats> UserStats { get; set; }
    public DbSet<ChatTicket> Tickets { get; set; }
    public DbSet<ChatSettings> ChatSettings { get; set; }
    public DbSet<TeslaChatCounter> TeslaChatCounters { get; set; }

    public DataContext()
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(Environment.GetEnvironmentVariable("RUDEBOT_DB_CONNECTION_STRING")!);
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSettings>()
            .Property(p => p.SendRandomMessages)
            .HasDefaultValue(true);
    }
}