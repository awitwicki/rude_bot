using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace RudeBot.Extensions
{
    internal static class TelegramExtensions
    {
        public static async Task<bool> TryDeleteMessage(this ITelegramBotClient client, Message message)
        {
            try
            {
                await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHaveAdminRights(this ChatMember user)
        {
            if (user.Status == ChatMemberStatus.Administrator || user.Status == ChatMemberStatus.Creator || user.User.Username == "@GroupAnonymousBot")
                return true;

            return false;
        }
    }
}
