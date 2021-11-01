# -*- coding: utf8 -*-
#/usr/bin/python3.7

import asyncio
import codecs
from datetime import datetime, timezone
import hashlib
import os
import random

from aiogram import Bot, types, executor
from aiogram.dispatcher import Dispatcher
from aiogram.types import InlineKeyboardMarkup, InlineKeyboardButton, ParseMode
from aiogram.types.message import Message
from aiogram.dispatcher.filters import Filter

from mats_counter import count_mats
from helper import *

bot_version = '2.3.16'

bot_token = os.getenv('RUDEBOT_TELEGRAM_TOKEN')
flood_timeout = int(os.getenv('RUDEBOT_FLOOD_TIMEOUT', '10'))
destruction_timeout = int(os.getenv('RUDEBOT_DELETE_TIMEOUT', '30'))
database_filename = 'data/' + (os.getenv('RUDEBOT_DATABASE_FILENAME', 'db.json'))
whitelist_chats = os.getenv('RUDEBOT_ALLOWED_CHATS', '')

whitelist_chats: list = None if whitelist_chats == '' else [int(chat) for chat in whitelist_chats.split(',')]


increase_words = ['+', 'спасибі', 'спс', 'дяки', 'дякс', 'благодарочка', 'вдячний', 'спасибо', 'дякую', 'благодарю', '👍', '😁', '😂', '😄', '😆', 'хаха', 'ахах']
decrease_words = ['-', '👎']

users = {}
user_karma = {}
# chat_messages = {}
last_top = None

bot: Bot = Bot(token=bot_token)
dp: Dispatcher = Dispatcher(bot)

def random_bool(probability: int) -> bool:
    '''probability 0..100'''
    return random.randint(0, 100) < probability

# def is_flood_message(message: types.Message):
#     chat_id: int = message.chat.id
#     chat_last_msg: Message = chat_messages.get(chat_id)
#     if not chat_last_msg:
#         chat_messages[chat_id] = message.date
#         return False
#     else:
#         is_flood = (message.date - chat_last_msg).seconds < flood_timeout
#         chat_messages[chat_id] = message.date
#         return is_flood

class ignore_old_messages(Filter):
    async def check(self, message: types.Message):
        return (datetime.now() - message.date).seconds < destruction_timeout

class white_list_chats(Filter):
    async def check(self, message: types.Message):
        if whitelist_chats:
            return message.chat.id in whitelist_chats
        return True


async def match_warn_message(message: types.Message) -> str:
    global bot

    #check if its reply
    if not message.reply_to_message:
        reply_text = '/warn або /unwarn має бути відповіддю, на чиєсь повідомлення'
        return reply_text

    #check if user have rights
    member = await bot.get_chat_member(message.chat.id, message.from_user.id)
    is_admin = member.user.mention == '@GroupAnonymousBot' or member.status == 'creator' or (member.status == 'administrator' and member.can_delete_messages)

    if not is_admin:
        reply_text = '/warn або /unwarn дозволений тільки для адмінів'
        return reply_text

    #check if reply not to another admin
    reply_to_member = await bot.get_chat_member(message.chat.id,  message.reply_to_message.from_user.id)
    is_member_admin = reply_to_member.user.mention == '@GroupAnonymousBot' or reply_to_member.status == 'creator' or (reply_to_member.status == 'administrator' and reply_to_member.can_delete_messages)
    if is_member_admin:
        reply_text = '/warn або /unwarn не діє на адмінів'
        return reply_text

    return None


async def match_scan_message(message: types.Message) -> str:
    global bot

    #check if its reply
    if not message.reply_to_message:
        reply_text = '/scan має бути відповіддю, на чиєсь повідомлення'
        return reply_text

    #check if user have rights
    member = await bot.get_chat_member(message.chat.id, message.from_user.id)
    is_admin = member.user.mention == '@GroupAnonymousBot' or member.status == 'creator' or (member.status == 'administrator' and member.can_delete_messages)

    if not is_admin:
        reply_text = '/scan дозволений тільки для адмінів'
        return reply_text

    return None


def update_user(func):
    async def wrapper(message: Message):
        user_id = message.from_user.id
        username = message.from_user.mention
        messageText = message.text.lower()

        mats = count_mats(messageText)
        add_or_update_user(user_id, username, mats)
        return await func(message)
    return wrapper


def add_or_update_user(user_id: int, username: str, mats_count: int):
    try:
        users[user_id]['username'] = username
        users[user_id]['total_messages'] += 1
        users[user_id]['total_mats'] += mats_count
        if not users[user_id].get('rude_coins'):
            users[user_id]['rude_coins'] = 0
    except:
        users[user_id] = {}
        users[user_id]['total_messages'] = 1
        users[user_id]['total_mats'] = mats_count
        users[user_id]['username'] = username
        users[user_id]['karma'] = 0
        users[user_id]['rude_coins'] = 0
        users[user_id]['warns'] = 0

    save_to_file(users)


def change_user_warns(user_id: int, warns_count: int):
    try:
        users[user_id]['warns'] += warns_count
    except:
        users[user_id]['warns'] = warns_count

    save_to_file(users)

    return users[user_id]['warns']


def get_karma(user_id : int):
    def size(id: int):
        return (id+6) % 15 + 7

    def orientation(id: int):
        result = hashlib.md5(id.to_bytes(8, 'big', signed=True)).hexdigest()
        _orientation = int(result, 16) 
        _orientation_1 = _orientation % 3
        _orientation_2 = _orientation % 5 % 2
        return _orientation_1, _orientation_2

    user = users[user_id]

    user_size = size(user_id)
    user_name = user['username']
    karma = user['karma']
    rude_coins = user['rude_coins']
    total_messages = user['total_messages']
    total_mats = user['total_mats']
    mats_percent = 0

    if total_mats > 0 and total_messages > 0:
        mats_percent = total_mats / total_messages
        mats_percent *= 100
        mats_percent = round(mats_percent, 2)

    karma_percent = 0
    if karma and total_messages:
        karma_percent = round(karma * 100 / total_messages, 2)

    replytext = f"Привіт {user_name}, твоя карма:\n\n"
    replytext += f"Карма: `{karma} ({karma_percent}%)`\n"

    #will selffix when add sql migrations
    try:
        warns = user['warns']
        replytext += f"🚧Попереджень: `{warns}`\n" if warns > 0 else ''
    except:
        pass

    replytext += f"Повідомлень: `{total_messages}`\n"
    replytext += f"Матюків: `{total_mats} ({mats_percent}%)`\n"
    replytext += f"Rude-коїнів: `{rude_coins}`💰\n"
    replytext += f"Довжина: `{user_size}` сантиметрів, ну і гігант...\n"

    user_values = orientation(user_id)
    orientation_type = ['Латентний', 'Гендерфлюід', ''][user_values[0]]
    orientation_name = ['Android', 'Apple'][user_values[1]]
    replytext += f"Орієнтація: `{orientation_type} {orientation_name}` користувач"

    replytext = replytext.replace('_', '\\_')

    return replytext


def increase_karma(dest_user_id: int, message_text: str):
    global bot
    if dest_user_id == bot.id:
        if message_text in increase_words :
            return "дякую"

    new_karma = None
    _username = None
    is_changed = False

    replytext = "Ви "
    for increase_word in increase_words:
        if increase_word in message_text:
            users[dest_user_id]['karma'] += 1
            new_karma = users[dest_user_id]['karma']
            _username = users[dest_user_id]['username']
            replytext += 'збільшили '
            is_changed = True
            break
    if not is_changed:
        for decrease_word in decrease_words:
            if decrease_word == message_text :
                users[dest_user_id]['karma'] -= 1
                new_karma = users[dest_user_id]['karma']
                _username = users[dest_user_id]['username']
                replytext += 'зменшили '
                is_changed = True
                break
    if not is_changed:
        return

    replytext += f'карму користувача {_username}\nДо значення {new_karma}!'
    save_to_file(users)

    return replytext


def get_top():
    replytext = "*Топ-10 карми чату:*\n"
    users_list = [ v for k, v in users.items()]
    sorted_users_list = sorted(users_list, key = lambda i: i['karma'], reverse = True)[:10]

    for usr in sorted_users_list:
        username = usr['username']
        karma = usr['karma']
        messages_count = usr['total_messages']

        karma_percent = 0
        if karma and messages_count:
            karma_percent = round(karma * 100 / messages_count, 2)

        replytext+=f'`{username}` - карма `{karma} ({karma_percent}%)`\n'

    replytext += "\n*Топ-10 актив чату:*\n"
    sorted_users_list = sorted(users_list, key = lambda i: i['total_messages'], reverse = True)[:10]

    for usr in sorted_users_list:
        username = usr['username']
        messages_count = usr['total_messages']
        replytext+=f'`{username}` - повідомлень `{messages_count}`\n'

    replytext += "\n*Топ-10 емоціонали чату:*\n"
    sorted_users_list = sorted(users_list, key = lambda i: i['total_mats'], reverse = True)[:10]

    for usr in sorted_users_list:
        username = usr['username']
        mats_count = usr['total_mats']

        total_messages = usr['total_messages']
        mats_percent = 0

        if mats_count > 0 and total_messages > 0:
            mats_percent = mats_count / total_messages
            mats_percent *= 100
            mats_percent = round(mats_percent, 2)

        replytext+=f'`{username}` - матюків `{mats_count} ({mats_percent}%)`\n'

    replytext += "\nКулдаун топу - 5 хвилин"

    replytext = replytext.replace('@', '')

    keyboard = types.InlineKeyboardMarkup()
    keyboard.add(types.InlineKeyboardButton(text="Оновити", callback_data="refresh_top"))
    return replytext, keyboard


def save_to_file(dict):
    f = codecs.open(database_filename, "w", "utf-8")
    f.write(str(users))
    f.close()


async def autodelete_message(chat_id: int, message_id: int, seconds=0):
    await asyncio.sleep(seconds)
    await bot.delete_message(chat_id=chat_id, message_id=message_id)


async def autodelete_messages(chat_id: int, message_ids: list, seconds=0):
    await asyncio.sleep(seconds)
    for message_id in message_ids:
        await bot.delete_message(chat_id=chat_id, message_id=message_id)


def read_users():
    if os.path.isfile(database_filename):
        global users
        with open(database_filename, 'r', encoding= 'utf-8') as f:
            users = eval(f.read())
    else:
        print("File not exist")


@dp.callback_query_handler(lambda call: call.data == "refresh_top")
async def refresh_top(call: types.CallbackQuery):
    replytext, reply_markup = get_top()
    replytext += f'\n`Оновлено UTC {datetime.now(timezone.utc)}`'
    await bot.edit_message_text(text=replytext, chat_id=call.message.chat.id, message_id=call.message.message_id, reply_markup=reply_markup, parse_mode=ParseMode.MARKDOWN)


@dp.callback_query_handler(lambda call: "counter" in call.data)
async def counter(call: types.CallbackQuery):
    like_text = call.data.split('|')[1]
    like_count = call.data.split('|')[2]
    like_count = int(like_count) + 1
    like_message_text = f'{like_text} x {like_count}'
    like_data = f'counter|{like_text}|{like_count}'

    keyboard = types.InlineKeyboardMarkup()
    keyboard.add(types.InlineKeyboardButton(text=like_message_text, callback_data=like_data))
    await bot.edit_message_reply_markup(chat_id=call.message.chat.id, message_id=call.message.message_id, reply_markup=keyboard)


@dp.callback_query_handler(lambda call: "print" in call.data)
async def print(call: types.CallbackQuery):
    print_value = call.data.split('|')[1]
    await call.answer(print_value, show_alert=True)


@dp.callback_query_handler(lambda call: "new_user" in call.data)
async def new_user(call: types.CallbackQuery):
    user_id = call.data.split('|')[1]
    user_id = int(user_id)
    user_clicked_id = call.from_user.id

    if user_id == user_clicked_id:
        await call.answer("Дуже раді вас бачити! Будь ласка, ознайомтеся з Конституцією чату в закріплених повідомленнях.", show_alert=True)
        await bot.delete_message(message_id=call.message.message_id, chat_id=call.message.chat.id)
    else:
        await call.answer("Ще раз і бан :)", show_alert=True)


@dp.message_handler(white_list_chats(), ignore_old_messages(), content_types=['new_chat_members'])
async def add_group(message: types.Message):
    keyboard = types.InlineKeyboardMarkup()
    keyboard.add(types.InlineKeyboardButton(text="Я обіцяю!", callback_data=f'new_user|{message.from_user.id}'))


    message_text = f"Вітаємо {message.from_user.mention} у нашому чаті! Ми не чат, а дружня, толерантна IT спільнота, яка поважає думку кожного, приєднавшись, ти згоджуєшся стати чемною частиною спільноти (та полюбити епл). I якшо не важко, пліз тут анкета на 8 питань https://forms.gle/pY6EjJhNRosUbd9P9"
    msg = await bot.send_animation(chat_id = message.chat.id, reply_to_message_id = message.message_id, animation = open("data/media/welcome.mp4", 'rb'), caption = message_text, reply_markup = keyboard)
    await autodelete_message(msg.chat.id, msg.message_id, destruction_timeout * 5)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='(^карма$|^karma$)')
@update_user
async def on_msg_karma(message: types.Message):
    user_id = message.from_user.id
    chat_id = message.chat.id

    reply_text = get_karma(user_id)
    msg = await bot.send_message(chat_id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    await autodelete_message(msg.chat.id, msg.message_id, destruction_timeout)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='(^топ$|^top$)')
@update_user
async def top_list(message: types.Message):
    chat_id = message.chat.id

    global last_top
    top_list_destruction_timeout = 300
    if not last_top or (datetime.now(timezone.utc) - last_top).seconds > top_list_destruction_timeout:
        reply_text, inline_kb = get_top()
        msg: types.Message = await bot.send_message(chat_id, text=reply_text, reply_markup=inline_kb, parse_mode=ParseMode.MARKDOWN)
        last_top = datetime.now(timezone.utc)
        await autodelete_message(msg.chat.id, msg.message_id, top_list_destruction_timeout)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='(^git$|^гіт$)')
@update_user
async def git(message: types.Message):
    reply_text = 'github.com/awitwicki/rude_bot'
    msg = await bot.send_message(message.chat.id, reply_to_message_id=message.message_id, text=reply_text, disable_web_page_preview=True)
    await autodelete_message(msg.chat.id, msg.message_id, destruction_timeout)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='(^cat$|^кот$|^кіт$|^кицька$)')
@update_user
async def cat(message: types.Message):
    cat_url = get_random_cat_image_url()
    cat_gender = bool(random.getrandbits(1))
    variant_1, variant_2 = ("Правильно", "Не правильно :(") if cat_gender else ("Не правильно :(", "Правильно")

    keyboard = types.InlineKeyboardMarkup()
    # keyboard.add(types.InlineKeyboardButton(text="😻", callback_data="counter|😻|0"))
    keyboard.add(types.InlineKeyboardButton(text="Кіт", callback_data=f'print|{variant_1}'))
    keyboard.add(types.InlineKeyboardButton(text="Кітесса", callback_data=f'print|{variant_2}'))
    await bot.send_photo(chat_id=message.chat.id, reply_to_message_id=message.message_id, caption='Кіт чи кітесса?', photo=cat_url, reply_markup=keyboard)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='(^зрада$|^/report$)')
@update_user
async def zrada(message: types.Message):
    global bot
    if message.reply_to_message and message.from_user.id != message.reply_to_message.from_user.id and message.reply_to_message.from_user.id != bot.id:
        user_name = message.reply_to_message.from_user.mention

        text = f'Ви оголосили зраду!\n' \
            f'{user_name}, слід подумати над своєю поведінкою!\n' \
            'Адміни вирішать твою долю (тюрма або бан)'

        keyboard = types.InlineKeyboardMarkup()
        keyboard.add(types.InlineKeyboardButton(text="🍾", callback_data=f'counter|🍾|0'))

        await bot.send_message(message.chat.id, text, reply_markup=keyboard)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='samsung|самсунг|сасунг')
@update_user
async def samsung(message: types.Message):
    if random_bool(10):
        msg = await bot.send_photo(message.chat.id, reply_to_message_id=message.message_id, photo=open('data/media/samsung.jpg', 'rb'))
        await autodelete_message(msg.chat.id, msg.message_id, destruction_timeout)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='шарий|шарій')
@update_user
async def сockman(message: types.Message):
    msg = await bot.send_video(message.chat.id, video=open('data/media/sh.MOV', mode='rb'))
    await autodelete_message(msg.chat.id, msg.message_id, destruction_timeout)


@dp.message_handler(white_list_chats(), ignore_old_messages(), regexp='tesl|тесл')
@update_user
async def tesla(message: types.Message):
    reply_text = "Днів без згадування тесли: `0`\n🚗🚗🚗"
    reply_text += '\n\n...btw ' + get_tesla_stock()

    msg = await bot.send_message(message.chat.id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    await autodelete_message(msg.chat.id, msg.message_id, destruction_timeout)



@dp.message_handler(white_list_chats(), ignore_old_messages(), commands=['scan'])
@update_user
async def scan(message: types.Message):
    global bot
    match_message_result = await match_scan_message(message)

    if match_message_result:
        msg = await bot.send_message(message.chat.id, text=match_message_result, parse_mode=ParseMode.MARKDOWN)
        await autodelete_messages(msg.chat.id, [msg.message_id, message.message_id], destruction_timeout)
        return

    try:
        user_id = message.reply_to_message.from_user.id
        chat_id = message.chat.id

        reply_text = get_karma(user_id)
    except:
        reply_text = "Я зломався :("

    msg = await bot.send_message(chat_id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    await autodelete_messages(msg.chat.id, [msg.message_id, message.message_id], destruction_timeout)


@dp.message_handler(white_list_chats(), ignore_old_messages(), commands=['start', 'help'])
@update_user
async def start(message: types.Message):
    reply_text = "Привіт, я Рудекіт!\n\n" \
                    "Я можу дещо зробити, якшо ти скажеш чарівне слово:\n" \
                    "`Карма` - покажу твою карму,\n" \
                    "`Топ` - покажу топ учасників чату,\n" \
                    "`Тесла` - порахую дні без згадування тесли,\n" \
                    "`Кіт` - покажу котика,\n" \
                    "`Шарій` - покажу півника,\n" \
                    "`Зрада` - розпочну процедуру бану,\n" \
                    "`гіт/git` - дам посилання на github, де можна мене вдосконалити,\n" \
                    "`/warn /unwarn` - (admins only) винесу попередження за погану поведінку,\n" \
                    "`/scan` - (admins only) просканую когось,\n" \
                    "А ще я вітаю новеньких у чаті.\n\n" \
                    f"Версія `{bot_version}`"

    msg = await bot.send_message(message.chat.id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    await autodelete_messages(msg.chat.id, [msg.message_id, message.message_id], destruction_timeout)


@dp.message_handler(white_list_chats(), ignore_old_messages(), commands=['warn'])
async def warn(message: types.Message):
    global bot
    match_message_result = await match_warn_message(message)

    if match_message_result:
        msg = await bot.send_message(message.chat.id, text=match_message_result, parse_mode=ParseMode.MARKDOWN)
        await autodelete_messages(msg.chat.id, [msg.message_id, message.message_id], destruction_timeout)
        return

    user_total_warns = change_user_warns(message.reply_to_message.from_user.id, 1)

    user_name = message.reply_to_message.from_user.mention

    reply_text = f'{user_name}, вам винесено попередження адміна! \n' \
        'Треба думати що ви пишете, \n' \
        f'ви маєте вже {user_total_warns} попередження!\n\n' \
        '1 попередження - будь-який адмін може заборонити медіа/стікери/ввести ліміт повідомлень!\n' \
        '2 попередження - мют на день (або тиждень, на розсуд адміна)!\n' \
        '3 попередження - бан!\n' \
        'Адміни вирішать твою долю'

    reply_text = reply_text.replace('_', '\\_')

    msg = await bot.send_message(message.chat.id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    await autodelete_message(msg.chat.id,  message.message_id, 0)


@dp.message_handler(white_list_chats(), ignore_old_messages(), commands=['unwarn'])
async def unwarn(message: types.Message):
    match_message_result = await match_warn_message(message)

    if match_message_result:
        msg = await bot.send_message(message.chat.id, text=match_message_result, parse_mode=ParseMode.MARKDOWN)
        await autodelete_messages(msg.chat.id, [msg.message_id, message.message_id], destruction_timeout)
        return

    user_total_warns = change_user_warns(message.reply_to_message.from_user.id, -1)

    user_name = message.reply_to_message.from_user.mention

    reply_text = f'{user_name}, ваше попередження анульовано!'
    reply_text = reply_text.replace('_', '\\_')

    if user_total_warns > 0:
        reply_text += f'\nНа балансі ще {user_total_warns} попередженнь!\n\n' 

    msg = await bot.send_message(message.chat.id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    await autodelete_message(msg.chat.id,  message.message_id, 0)


@dp.message_handler(white_list_chats(), ignore_old_messages())
@update_user
async def on_msg(message: types.Message):
    user_id = message.from_user.id
    chat_id = message.chat.id
    message_id = message.message_id

    messageText = ""
    if message.sticker is not None:
        messageText = message.sticker.emoji
    else:
        messageText = message.text.lower()

    # karma message
    if message.reply_to_message and message.reply_to_message.from_user.id and user_id != message.reply_to_message.from_user.id:
        # check user on karmaspam
        # if not is_flood_message(message):
        karma_changed = increase_karma(message.reply_to_message.from_user.id, messageText)
        if karma_changed:
            msg = await bot.send_message(chat_id, text=karma_changed, reply_to_message_id=message_id)
            await autodelete_message(msg.chat.id, message_id=msg.message_id, seconds=destruction_timeout)

    #ru filter
    if '.ru' in messageText:
        reply_mesage = "*Російська пропаганда не може вважатися пруфом!*\n\nВас буде додано до реєстру."
        msg = await bot.send_message(chat_id, text=reply_mesage, reply_to_message_id=message_id)

    #random advice
    if random_bool(1):
        reply_mesage = get_random_better_advice()
        reply_to_message_id = None if random.randint(0, 100) < 50 else message_id
        msg = await bot.send_message(chat_id, text=reply_mesage, reply_to_message_id=reply_to_message_id)
        # await autodelete_message(msg.chat.id, message_id=msg.message_id, seconds=destruction_timeout)


if __name__ == '__main__':
    read_users()
    dp.bind_filter(white_list_chats)
    dp.bind_filter(ignore_old_messages)
    executor.start_polling(dp)
