﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot
{
    public static class Consts
    {
        public const string BotVersion = "3.18.1";
        public const string GithubUrl = "github.com/awitwicki/rude_bot/tree/dev_dotnet";
        public const string GoogleFormForNewbies = "https://forms.gle/pY6EjJhNRosUbd9P9";
        public const string WelcomeToTheClubBuddyVideoUrl = "https://github.com/awitwicki/rude_bot/blob/main/media/welcome.mp4?raw=true";
        public const string CockmanVideoUrl = "https://github.com/awitwicki/rude_bot/blob/main/media/sh.mp4?raw=true";
        public const string SamsungUrl = "https://github.com/awitwicki/rude_bot/blob/main/media/samsung.jpg?raw=true";

        public const string BadWordsReaderService = "BadWordsReaderService";
        public const string BadWordsTxtPath = "Resources/Badwords.txt";
        public const string AdvicesReaderService = "AdvicesReaderService";
        public const string AdvicesTxtPath = "Resources/Advices.txt";

        public const string DbPath = "data/database.sqlite";

        public const string TnxWordsRegex = "\\+|спасибі|спс|сяп|tnx|дяки|дякс|благодарочка|вдячний|спасибо|дякую|благодарю|👍|😁|😂|😄|😆|хаха|хех|дзенькую|вогонь|агонь|агінь|вагінь";
    }
}
