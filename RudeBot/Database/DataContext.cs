using Microsoft.EntityFrameworkCore;
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

        public DataContext()
        {

        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={Consts.DbPath}");
    }
}
