# -*- coding: utf8 -*-
#/usr/bin/python3.7

import codecs
from datetime import datetime, timezone
import random
from os.path import commonpath
import os
import hashlib

from telegram.ext import Updater, Filters, MessageHandler, CommandHandler, CallbackQueryHandler, CallbackContext
from telegram import InlineKeyboardMarkup, InlineKeyboardButton, ParseMode, Message
from telegram.update import Update


from Config import Config
from mats_counter import count_mats
from youtube_parser import *
from helper import *

conf = Config('config.ini', ['telegram_token', 'destruction_timeout', 'database_filename'])

# https://github.com/python-telegram-bot/python-telegram-bot/wiki/Transition-guide-to-Version-12.0
bot_token = conf.Data['telegram_token']

#bot will delete his owm nessage after defined time
destruction_timeout = int(conf.Data['destruction_timeout'])

database_filename = conf.Data['database_filename']

increase_words = ['+', 'спасибі', 'спс', 'дяки', 'дякс', 'благодарочка', 'вдячний', 'спасибо', 'дякую', 'благодарю', '👍', '😁', '😂', '😄', '😆', 'хаха', 'ахах']
decrease_words = ['-', '👎']

users = {}
user_karma = {}

bot_id = None
last_top = None
url_video_list_dima = None
url_video_list_asado = None

saved_messages_ids = []


#Todo:
#ignore karmaspam from users
# def check_user_for_karma(user_id: int, dest_user_id: int):
#     try:
#         usr_ch = user_karma[user_id]
#     except:
#         return True


def check_message_is_old(message: Message):
    return (datetime.now(timezone.utc) - message.date).seconds > 300


def ignore_old_message(func):
    def wrapper(*args, **kwargs):
        update, context = args
        message: Message = update.message

        is_old = check_message_is_old(message)

        if not is_old:
            func(*args, **kwargs)

    return wrapper


def get_karma(user_id : int):
    def size(id: int):
        result = hashlib.md5(id.to_bytes(8, 'big', signed=True)).hexdigest()
        size = int(result, 16) 
        size = size % 15 + 7
        return size

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

    replytext = f"Привіт {user_name}, твоя карма:\n\n"
    replytext += f"Карма: `{karma}`\n"
    replytext += f"Повідомлень: `{total_messages}`\n"
    replytext += f"Матюків: `{total_mats} ({mats_percent}%)`\n"
    replytext += f"Rude-коїнів: `{rude_coins}`💰\n"
    replytext += f"Довжина: `{user_size}` сантиметрів, ну і гігант..."

    replytext = replytext.replace('_', '\\_')

    return replytext


def add_or_update_user(user_id: int, username: str, mats_count: int):
    try:
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

    save_to_file(users)


def increase_karma(dest_user_id: int, message_text: str):
    if dest_user_id == bot_id:
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

    replytext += f'карму користувача {_username} до значення {new_karma}!'
    save_to_file(users)

    return replytext


def btn_clicked(update: Update, context: CallbackContext):
    command = update.callback_query.data
    chat_id = update.callback_query.message.chat_id
    message_id = update.callback_query.message.message_id
    callback_query_id = update.callback_query.id

    if command == 'refresh_top':
        replytext, reply_markup = get_top()
        replytext += f'\n`Оновлено UTC {datetime.now(timezone.utc)}`'
        query = update.callback_query
        query.edit_message_text(text=replytext, reply_markup=reply_markup, parse_mode=ParseMode.MARKDOWN)
        return
    elif 'like_cat' in command:
        likes = command.split('|')[1]
        likes = int(likes) + 1
        like_text = f'😻 x {likes}'
        keyboard = [[InlineKeyboardButton(like_text, callback_data=f'like_cat|{likes}')]]
        reply_markup = InlineKeyboardMarkup(keyboard)
        context.bot.edit_message_reply_markup(chat_id=chat_id, message_id=message_id, reply_markup=reply_markup)
        if likes == 1:
            saved_messages_ids.append(message_id)
    elif 'zrada' in command:
        likes = command.split('|')[1]
        likes = int(likes) + 1
        like_text = f'🚓 x {likes}'
        keyboard = [[InlineKeyboardButton(like_text, callback_data=f'zrada|{likes}')]]
        reply_markup = InlineKeyboardMarkup(keyboard)
        context.bot.edit_message_reply_markup(chat_id=chat_id, message_id=message_id, reply_markup=reply_markup)
        if likes == 1:
            saved_messages_ids.append(message_id)
    elif 'game' in command:
        clicked_variant = command.split('|')[1]
        response = "Правильно! :)" if clicked_variant == str(True) else "Не правильно! :("
        context.bot.answerCallbackQuery(callback_query_id, text=response, show_alert=True)

    else: #new user clicked
        user_id = int(command)
        user_clicked_id = update.callback_query.from_user.id

        if user_id == user_clicked_id:
            try:
                context.bot.delete_message(chat_id=chat_id, message_id=message_id)

            except:
                pass
        else:
            context.bot.answer_callback_query(callback_query_id=update.callback_query.id, text='Ще раз і бан :)', show_alert=True)


def get_top():
    replytext = "*Топ-10 карми чату:*\n"
    users_list = [ v for k, v in users.items()]
    sorted_users_list = sorted(users_list, key = lambda i: i['karma'], reverse = True)[:10]

    for usr in sorted_users_list:
        username = usr['username']
        karma = usr['karma']
        replytext+=f'`{username}` - карма `{karma}`\n'

    replytext += "\n*Топ-10 актив чату:*\n"
    sorted_users_list = sorted(users_list, key = lambda i: i['total_messages'], reverse = True)[:10]

    for usr in sorted_users_list:
        username = usr['username']
        messagescount = usr['total_messages']
        replytext+=f'`{username}` - повідомлень `{messagescount}`\n'

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

    keyboard = [[InlineKeyboardButton("Оновити", callback_data='refresh_top')]]
    reply_markup = InlineKeyboardMarkup(keyboard)
    return replytext, reply_markup


def save_to_file(dict):
    f = codecs.open(database_filename, "w", "utf-8")
    f.write(str(users))
    f.close()


def autodelete_message(context):
    chat_id = context.job.context[0]
    message_id = context.job.context[1]
    if message_id in saved_messages_ids:
        saved_messages_ids.remove(message_id)
        return

    context.bot.delete_message(chat_id=chat_id, message_id=message_id)
    if len(context.job.context) > 2:
        try:
            context.bot.delete_message(chat_id=chat_id, message_id=context.job.context[2])
        except:
            pass


def read_users():
    if os.path.isfile(database_filename):
        global users
        with open(database_filename, 'r', encoding= 'utf-8') as f:
            users = eval(f.read())
    else:
        print ("File not exist")


def on_msg(update: Update, context: CallbackContext):
    try:
        message: Message = update.message
        is_old = check_message_is_old(message)

        user_id = message.from_user.id
        username = message.from_user.name
        _chat_id = message.chat_id
        _message_id = message.message_id

        messageText = ""
        if message.sticker is not None:
            messageText = message.sticker.emoji
        else:
            messageText = message.text.lower()

        mats = count_mats(messageText)
        add_or_update_user(user_id, username, mats)

        # update karma message
        if message.reply_to_message and message.reply_to_message.from_user.id and user_id != message.reply_to_message.from_user.id:
            karma_changed = increase_karma(message.reply_to_message.from_user.id, messageText)
            if karma_changed and not is_old:
                msg = context.bot.send_message(_chat_id, text=karma_changed)
                context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id])

    except Exception as e:
        print(e)


@ignore_old_message
def give(update: Update, context: CallbackContext):
    try:
        message: Message = update.message

        user_id = message.from_user.id
        _chat_id = message.chat_id
        _message_id = message.message_id

        if message.reply_to_message and user_id != message.reply_to_message.from_user.id:
            username = message.reply_to_message.from_user.name

            if not 'rude_coins' in users[message.reply_to_message.from_user.id]:
                users[message.reply_to_message.from_user.id]['rude_coins'] = 100

            #get user coins
            user_coins = users[user_id]['rude_coins']

            #parse coins amount
            if context.args:
                amount = int(context.args[0])
                if amount > user_coins:
                    msg = context.bot.send_message(_chat_id, reply_to_message_id=_message_id, text=f"Недостатньо коїнів, вы маєте тільки {user_coins}💰")
                    context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])
                    return

                if amount <= 0:
                    msg = context.bot.send_message(_chat_id, reply_to_message_id=_message_id, text=f"Самий умний?")
                    context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])
                    return

                users[message.reply_to_message.from_user.id]['rude_coins'] +=amount
                users[user_id]['rude_coins'] -= amount

                msg = context.bot.send_message(_chat_id, reply_to_message_id=_message_id, text=f"Ви переказали {username} {amount} коїнів 💰")
                context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])
                return
            else:
                msg = context.bot.send_message(_chat_id, reply_to_message_id=_message_id, text=f"/give 1..{user_coins}")
                context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])
                return
        else:
            msg = context.bot.send_message(_chat_id, reply_to_message_id=_message_id, text=f'Щоб поділитися коїнами, вы маєте зробити реплай на повідомлення особи отримувача, текст має бути таким:\n\n"/give 25" (переказ 25 коїнів)', parse_mode=ParseMode.MARKDOWN)
            context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])
            return
    except Exception as e:
        print(e)


@ignore_old_message
def git(update: Update, context: CallbackContext):
    _chat_id = update.message.chat_id

    reply_text = 'github.com/awitwicki/rude\\_bot'
    msg = context.bot.send_message(_chat_id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    context.job_queue.run_once(autodelete_message, 300, context=[msg.chat_id, msg.message_id])


@ignore_old_message
def top_list(update: Update, context: CallbackContext):
    global last_top

    _chat_id = update.message.chat_id

    if not last_top or (datetime.now(timezone.utc) - last_top).seconds > 300:
        reply_text, reply_markup = get_top()
        msg = context.bot.send_message(_chat_id, text=reply_text, reply_markup=reply_markup, parse_mode=ParseMode.MARKDOWN)
        context.job_queue.run_once(autodelete_message, 300, context=[msg.chat_id, msg.message_id])
        last_top = datetime.now(timezone.utc)


@ignore_old_message
def cat(update: Update, context: CallbackContext):
    _chat_id = update.message.chat_id
    _message_id = update.message.message_id

    cat_url = get_random_cat_image_url()
    keyboard = [[InlineKeyboardButton("😻", callback_data='like_cat|0')]]
    reply_markup = InlineKeyboardMarkup(keyboard)
    msg = context.bot.send_photo(_chat_id, cat_url, reply_markup=reply_markup)
    context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])


@ignore_old_message
def game(update: Update, context: CallbackContext):
    _chat_id = update.message.chat_id
    _message_id = update.message.message_id

    cat_url = get_random_cat_image_url()
    cat_gender = bool(random.getrandbits(1))
    variant_1, variant_2 = (True, False) if cat_gender else (False, True)
    keyboard = [[InlineKeyboardButton("Кіт", callback_data=f'game|{variant_1}'), InlineKeyboardButton("Кітесса", callback_data=f'game|{variant_2}')]]
    reply_markup = InlineKeyboardMarkup(keyboard)
    msg = context.bot.send_photo(_chat_id, cat_url, caption='Кіт чи кітесса?', reply_markup=reply_markup)
    context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])


@ignore_old_message
def zrada(update: Update, context: CallbackContext):
    if update.message.reply_to_message and update.message.from_user.id != update.message.reply_to_message.from_user.id and update.message.reply_to_message.from_user.id != bot_id:
        chat_id = update.message.chat_id
        reply_to_message_id = update.message.reply_to_message.message_id

        user_name = update.message.reply_to_message.from_user.name

        text = f'Ви оголосили зраду {user_name}!\n' \
            f'{user_name}, слід подумати над своєю поведінкою!\n' \
            'Адміни вирішать твою долю (тюрма або бан)'

        keyboard = [[InlineKeyboardButton("🚓", callback_data='zrada|0')]]
        reply_markup = InlineKeyboardMarkup(keyboard)
        context.bot.send_message(chat_id, text, reply_to_message_id=reply_to_message_id, reply_markup=reply_markup)


@ignore_old_message
def xiaomi(update: Update, context: CallbackContext):
    _chat_id = update.message.chat_id
    _message_id = update.message.message_id

    msg = context.bot.send_photo(_chat_id, reply_to_message_id=_message_id, photo=open('xiaomi.jpg', 'rb'))
    context.job_queue.run_once(autodelete_message, 30, context=[msg.chat_id, msg.message_id])


@ignore_old_message
def karma(update: Update, context: CallbackContext):
    user_id = update.message.from_user.id
    _chat_id = update.message.chat_id

    reply_text = get_karma(user_id)
    msg = context.bot.send_message(_chat_id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id])


@ignore_old_message
def сockman(update: Update, context: CallbackContext):
    msg = update.message.reply_video(quote=True, video=open('sh.MOV', mode='rb'))
    context.job_queue.run_once(autodelete_message, 30, context=[msg.chat_id, msg.message_id])


@ignore_old_message
def tesla(update: Update, context: CallbackContext):
    _chat_id = update.message.chat_id
    reply_text = "Днів без згадування тесли: `0`\n🚗🚗🚗"
    msg = context.bot.send_message(_chat_id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
    context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id])


def callback_minute(context: CallbackContext):
    global url_video_list_dima
    global url_video_list_asado

    new_video_list_dima = get_urls('https://www.youtube.com/feeds/videos.xml?channel_id=UC20M3T-H-Pv0FPOEfeQJtNQ')
    new_video_list_asado = get_urls('https://www.youtube.com/feeds/videos.xml?channel_id=UCfkPlh5dfjbw8hc1s-yJQdw')

    # get new url list
    if url_video_list_dima is None:
        url_video_list_dima = new_video_list_dima
        return

    if url_video_list_asado is None:
        url_video_list_asado = new_video_list_asado
        return

    # look for new videos
    new_videos_dima = get_new_urls(url_video_list_dima, new_video_list_dima)
    new_videos_asado = get_new_urls(url_video_list_asado, new_video_list_asado)

    if len(new_videos_dima) > 0:
        url_video_list_dima = new_video_list_dima

        for new_video in new_videos_dima:
            context.bot.send_message(chat_id='@rude_chat', text=new_video)

    if len(new_videos_asado) > 0:
        url_video_list_asado = new_video_list_asado

        for new_video in new_videos_asado:
            context.bot.send_message(chat_id='@rude_chat', text=new_video)

def add_group(update: Update, context: CallbackContext):
    for member in update.message.new_chat_members:
        if not member.is_bot:
            chat_id = update.message.chat_id
            message_id = update.message.message_id

            keyboard = [[InlineKeyboardButton("Я обіцяю!", callback_data=member.id)]]
            reply_markup = InlineKeyboardMarkup(keyboard)
            message_text = f"Вітаємо {member.name} у нашому чаті! Ми не чат, а дружня, толерантна IT спільнота, яка поважає думку кожного, приєднавшись, ти згоджуєшся стати чемною частиною спільноти (та полюбити епл). I якшо не важко, пліз тут анкета на 8 питань https://forms.gle/pY6EjJhNRosUbd9P9"
            msg = context.bot.sendAnimation(chat_id = chat_id, reply_to_message_id = message_id, animation = open("welcome.mp4", 'rb'), caption = message_text, reply_markup = reply_markup)
            context.job_queue.run_once(autodelete_message, 300, context=[msg.chat_id, msg.message_id])


def main():
    global bot_id

    read_users()

    updater = Updater(bot_token, use_context=True)

    dp = updater.dispatcher
    dp.add_handler(CommandHandler('give', give, pass_args=True))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'^гіт$', re.IGNORECASE)), git))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'^топ$', re.IGNORECASE)), top_list))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'(^cat$|^кот$|^кіт$|^кицька$)', re.IGNORECASE)), cat))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'^гра$', re.IGNORECASE)), game))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'(^зрада|/report$)', re.IGNORECASE)), zrada))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'(^xiaomi|сяоми$)', re.IGNORECASE)), xiaomi))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'^карма$', re.IGNORECASE)), karma))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'(^шарий|шарій$)', re.IGNORECASE)), сockman))
    dp.add_handler(MessageHandler(Filters.regex(re.compile(r'tesl|тесл', re.IGNORECASE)), tesla))
    dp.add_handler(MessageHandler(Filters.text | Filters.sticker, on_msg, edited_updates = True))
    dp.add_handler(CallbackQueryHandler(btn_clicked))
    dp.add_handler(MessageHandler(Filters.status_update.new_chat_members, add_group))

    # new videos
    j = updater.job_queue
    job_minute = j.run_repeating(callback_minute, interval=60, first=0)

    updater.start_polling()
    bot_id = updater.bot.id
    bot_name = updater.bot.name
    print(f"{bot_name} is started.")
    updater.idle()


if __name__ == '__main__':
    main()
