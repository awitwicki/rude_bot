// See https://aka.ms/new-console-template for more information
using PowerBot.Lite;

Console.WriteLine("Starting RudeBot");

string botToken = Environment.GetEnvironmentVariable("RUDEBOT_TELEGRAM_TOKEN")!;

CoreBot botClient = new CoreBot(botToken);

// Wait for eternity
await Task.Delay(Int32.MaxValue);
