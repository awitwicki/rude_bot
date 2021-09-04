import asyncio
from datetime import datetime

from aiogram import Bot, types, executor
from aiogram.dispatcher import Dispatcher
from aiogram.dispatcher.filters import Filter
from aiogram.types import ParseMode

from const import days_without, bot_start_text, Vag, Tesla
from src.const import bot_token, destruction_timeout, whitelist_chats

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
