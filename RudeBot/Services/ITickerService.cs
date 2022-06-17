using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Services
{
    public interface ITickerService
    {
        Task<double> GetTickerPrice(string tickerName);
    }
}
