using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot
{
    public static class Consts
    {
        public const string BotVersion = "3.21";
        public const string GithubUrl = "github.com/awitwicki/rude_bot/tree/dev_dotnet";
        public const string GoogleFormForNewbies = "https://forms.gle/pY6EjJhNRosUbd9P9";
        public const string WelcomeToTheClubBuddyVideoUrl = "https://raw.githubusercontent.com/awitwicki/rude_bot/main/media/welcome.mp4";
        public const string CockmanVideoUrl = "https://raw.githubusercontent.com/awitwicki/rude_bot/main/media/sh.mp4";
        public const string SamsungUrl = "https://raw.githubusercontent.com/awitwicki/rude_bot/main/media/samsung.jpg";

        public const string BadWordsReaderService = "BadWordsReaderService";
        public const string BadWordsTxtPath = "Resources/Badwords.txt";
        public const string AdvicesReaderService = "AdvicesReaderService";
        public const string AdvicesTxtPath = "Resources/Advices.txt";

        public const string DbPath = "data/database.sqlite";

        public const string TnxWordsRegex = "\\+|спасибі|спс|сяп|tnx|дяки|дякс|благодарочка|вдячний|спасибо|дякую|благодарю|👍|😁|😂|😄|😆|хаха|хех|дзенькую|вогонь|агонь|агінь|вагінь";
    }
}
