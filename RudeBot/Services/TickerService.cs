using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace RudeBot.Services
{
    internal class TickerService : ITickerService
    {
        private DateTime _lastRequest { get; set; } = DateTime.MinValue;
        private double _lastValue { get; set; }

        public async Task<double> GetTickerPrice(string tickerName)
        {
            if ((DateTime.UtcNow - _lastRequest).TotalMinutes >= 5)
            {
                try
                {
                    var securities = await Yahoo.Symbols(tickerName)
                        .Fields(Field.Symbol, Field.RegularMarketPrice)
                        .QueryAsync();

                    var ticker = securities[tickerName];
                    var price = ticker.RegularMarketPrice;

                    _lastValue = price;
                    _lastRequest = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _lastValue = 0;
                }
            }
         
            return _lastValue;
        }
    }
}
