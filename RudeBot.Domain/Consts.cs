namespace RudeBot.Domain;

public static class Consts
{
    public const string BotVersion = "3.52.1";

    public const string BadWordsService = "BadWordsService";
    public const string AdvicesService = "AdvicesService";

    public const string TnxWordsRegex = "(?<=\\B)\\+(?=\\B)|спасибі|спс|сяп|tnx|дяки|дякс|благодарочка|вдячний|спасибо|дякую|благодарю|👍|😁|😂|😄|😆|хаха|хех|дзенькую|вогонь|агонь|агінь|вагінь|xd|хд";

    // \b after "ru" is required: without it, the pattern matches as soon as it finds ".ru"
    // anywhere, so words like "manual.rules", "a.rug" or "vodka.ru3" false-positive since
    // nothing stops the match once ".ru" is found mid-word.
    public const string DotRuRegex = "[\\w\\-]+\\.ru\\b";

    public static long? CreatorId { get; set; }

    public static long BotUserId { get; set; }
}
