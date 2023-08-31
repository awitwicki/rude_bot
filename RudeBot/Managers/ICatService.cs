namespace RudeBot.Managers;

public interface ICatService
{
    Task<string> GetRandomCatImageUrl();
}