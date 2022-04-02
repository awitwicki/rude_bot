// See https://aka.ms/new-console-template for more information
using PowerBot.Lite;
using RudeBot.Database;

Console.WriteLine("Starting RudeBot");

string botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;

// Create database if not exists
DataContext _dbContext = new DataContext();
_dbContext.Database.EnsureCreated();
_dbContext.Dispose();

// Run bot
CoreBot botClient = new CoreBot(botToken);

// Wait for eternity
await Task.Delay(Int32.MaxValue);
