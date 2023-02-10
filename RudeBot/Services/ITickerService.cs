namespace RudeBot.Services
{
    public interface ITickerService
    {
        Task<double> GetTickerPrice(string tickerName);
    }
}
