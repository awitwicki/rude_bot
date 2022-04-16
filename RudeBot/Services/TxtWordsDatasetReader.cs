using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Services
{
    public class TxtWordsDatasetReader
    {
        private List<string> _words;
        public TxtWordsDatasetReader(string path)
        {
            _words = File.ReadAllLines(path, Encoding.UTF8).ToList();
        }

        public List<string> GetWords()
        {
            return _words;
        }
    }
}
