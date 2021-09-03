import asyncio
import os
from datetime import datetime

from aiogram import Bot, types, executor
from aiogram.dispatcher import Dispatcher
from aiogram.dispatcher.filters import Filter
from aiogram.types import ParseMode

from const import days_without, bot_start_text, Skoda, Vag, Tesla

bot_token = os.getenv("RUDEBOT_TELEGRAM_TOKEN", "1324451199:AAHLbRQZ70lAnPAKHM9YPaxS_DuoUK26cy4")
destruction_timeout = int(os.getenv("RUDEBOT_DELETE_TIMEOUT", "30"))
whitelist_chats = os.getenv("RUDEBOT_ALLOWED_CHATS", "")

whitelist_chats = (
    None
    if whitelist_chats == ""
    else [int(chat) for chat in whitelist_chats.split(",")]
)

bot: Bot = Bot(token=bot_token)
dp: Dispatcher = Dispatcher(bot)


class IgnoreOldMessages(Filter):
    async def check(self, message: types.Message):
        return (datetime.now() - message.date).seconds < destruction_timeout


class WhiteListChats(Filter):
    async def check(self, message: types.Message):
        if whitelist_chats:
            return message.chat.id in whitelist_chats
        return True


async def auto_delete_message(chat_id: int, message_id: int, seconds=0):
    await asyncio.sleep(seconds)
    await bot.delete_message(chat_id=chat_id, message_id=message_id)


# @dp.callback_query_handler(lambda call: "new_user" in call.data)
# async def new_user(call: types.CallbackQuery):
#     user_id = call.data.split('|')[1]
#     user_id = int(user_id)
#     user_clicked_id = call.from_user.id
#
#     if user_id == user_clicked_id:
#         await call.answer(new_user_greeting, show_alert=True)
#         await bot.delete_message(message_id=call.message.message_id, chat_id=call.message.chat.id)
#     else:
#         await call.answer(new_user_warning, show_alert=True)


# @dp.message_handler(WhiteListChats(), IgnoreOldMessages(), content_types=['new_chat_members'])
# async def add_group(message: types.Message):
#     keyboard = types.InlineKeyboardMarkup()
#     keyboard.add(types.InlineKeyboardButton(text=new_member_button_text,
#                                             callback_data=f'new_user|{message.from_user.id}'))
#
#     msg = await bot.send_animation(chat_id=message.chat.id, reply_to_message_id=message.message_id,
#                                    animation=open("data/media/welcome.mp4", 'rb'),
#                                    caption=new_member_greeting.format(
#                                        user=message.from_user.mention), reply_markup=keyboard)
#     await auto_delete_message(msg.chat.id, msg.message_id, destruction_timeout * 5)


@dp.message_handler(WhiteListChats(), IgnoreOldMessages(), regexp=Tesla.regexp)
async def tesla(message: types.Message):
    msg = await bot.send_message(
        message.chat.id,
        text=days_without.format(name=Tesla.name_cyrillic, emoji=Tesla.emoji),
        parse_mode=ParseMode.MARKDOWN,
    )
    await auto_delete_message(msg.chat.id, msg.message_id, destruction_timeout)


@dp.message_handler(WhiteListChats(), IgnoreOldMessages(), regexp=Vag.regexp)
async def vag(message: types.Message):
    msg = await bot.send_message(
        message.chat.id,
        text=days_without.format(name=Vag.name_cyrillic, emoji=Vag.emoji),
        parse_mode=ParseMode.MARKDOWN,
    )
    await auto_delete_message(msg.chat.id, msg.message_id, destruction_timeout)


@dp.message_handler(WhiteListChats(), IgnoreOldMessages(), regexp=Skoda.regexp)
async def skoda(message: types.Message):
    msg = await bot.send_message(
        message.chat.id,
        text=days_without.format(name=Skoda.name_cyrillic, emoji=Skoda.emoji),
        parse_mode=ParseMode.MARKDOWN,
    )
    await auto_delete_message(msg.chat.id, msg.message_id, destruction_timeout)


@dp.message_handler(WhiteListChats(), IgnoreOldMessages(), commands=["start", "help"])
async def start(message: types.Message):
    msg = await bot.send_message(
        message.chat.id, text=bot_start_text, parse_mode=ParseMode.MARKDOWN
    )
    await auto_delete_message(msg.chat.id, msg.message_id, destruction_timeout)


if __name__ == "__main__":
    dp.bind_filter(WhiteListChats)
    dp.bind_filter(IgnoreOldMessages)
    executor.start_polling(dp)
