using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Services
{
    internal interface ITickerService
    {
        Task<double> GetTickerPrice(string tickerName);
    }
}
