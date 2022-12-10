using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Database
{
    public class DataContext : DbContext
    {
        public DbSet<TelegramUser> Users { get; set; }
        public DbSet<UserChatStats> UserStats { get; set; }
        public DbSet<ChatTicket> Tickets { get; set; }
        public DbSet<ChatSettings> ChatSettings { get; set; }

        public DataContext()
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
             => options.UseNpgsql(Environment.GetEnvironmentVariable("RUDEBOT_DB_CONNECTION_STRING")!);
    }
}
