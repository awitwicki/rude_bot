using System.Text.RegularExpressions;

namespace RudeBot.Managers
{
    internal class CatService : ICatService
    {
        public async Task<string> GetRandomCatImageUrl()
        {
            // Use cat api to get random cat image url
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync("https://api.thecatapi.com/v1/images/search?mime_types=jpg,png");
                var data = await result.Content.ReadAsStringAsync();

                //regex match
                var m = Regex.Match(data, "https:\\/\\/[\\w.,@?^=%&:/~+#-]*", RegexOptions.IgnoreCase);
                if (m.Success)
                    return m.Value;

                return null;
            }
        }
    }
}
