namespace RudeBot.Domain;

public static class Consts
{
    public const string BotVersion = "3.50.0";

    public const string BadWordsService = "BadWordsService";
    public const string AdvicesService = "AdvicesService";

    public const string TnxWordsRegex = "(?<=\\B)\\+(?=\\B)|спасибі|спс|сяп|tnx|дяки|дякс|благодарочка|вдячний|спасибо|дякую|благодарю|👍|😁|😂|😄|😆|хаха|хех|дзенькую|вогонь|агонь|агінь|вагінь|xd|хд";

    public static long? CreatorId { get; set; }

    public static long BotUserId { get; set; }
}
