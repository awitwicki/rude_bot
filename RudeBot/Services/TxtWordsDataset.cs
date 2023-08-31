namespace RudeBot.Services;

public class TxtWordsDataset
{
    private readonly List<string> _words;
        
    public TxtWordsDataset(IEnumerable<string> data)
    {
        _words =  data.ToList();
    }

    public List<string> GetWords()
    {
        return _words;
    }
}