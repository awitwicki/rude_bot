using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace RudeBot.Keyboards
{
    internal static class KeyboardBuilder
    {
        public static InlineKeyboardMarkup BuildUserRightsManagementKeyboard(long userId)
        {
            var keyboardMarkup = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>> {
                new List<InlineKeyboardButton> {
                     InlineKeyboardButton.WithCallbackData("Заборона медіа", $"manage_ban_media_user|{userId}"),
                     InlineKeyboardButton.WithCallbackData("Мют на день", $"manage_mute_day_user|{userId}"),
                },
                new List<InlineKeyboardButton> {
                     InlineKeyboardButton.WithCallbackData("Стратити лагідно", $"manage_kick_user|{userId}"),
                     InlineKeyboardButton.WithCallbackData("Стратити назавжди", $"manage_ban_user|{userId}"),
                },
                new List<InlineKeyboardButton> {
                    InlineKeyboardButton.WithCallbackData("Амністувати", $"manage_amnesty_user|{userId}")
                }
            });

            return keyboardMarkup;
        }
    }
}
