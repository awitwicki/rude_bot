using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Services.DuplicateDetectorService
{
    public interface IDuplicateDetectorService
    {
        List<int> FindDuplicates(long chatId, int messageId, string text);
    }
}
