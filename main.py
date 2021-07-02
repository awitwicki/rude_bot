# -*- coding: utf8 -*-
#/usr/bin/python3.7

from datetime import datetime, timezone
import telegram
from telegram.ext import Updater, Filters, MessageHandler, CallbackQueryHandler, CallbackContext
from telegram import InlineKeyboardMarkup, InlineKeyboardButton, ParseMode, Message
import os
import codecs

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

def get_karma(user_id : int):
    user = users[user_id]

    replytext = f"Привіт {user['username']}, твоя карма:\n\n"
    replytext += f"Карма: `{user['karma']}`\n"
    replytext += f"Повідомлень: `{user['total_messages']}`\n"
    replytext += f"Матюків: `{user['total_mats']}`"
    replytext += ''

    replytext = replytext.replace('_', '\\_')

    return replytext


def add_or_update_user(user_id: int, username: str, mats_count: int):
    try:
        users[user_id]['total_messages'] += 1
        users[user_id]['total_mats'] += mats_count
    except:
        users[user_id] = {}
        users[user_id]['total_messages'] = 1
        users[user_id]['total_mats'] = mats_count
        users[user_id]['username'] = username
        users[user_id]['karma'] = 0

    saveToFile(users)


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
    saveToFile(users)

    return replytext


def btn_clicked(update, context):
    command = update.callback_query.data
    chat_id = update.callback_query.message.chat_id
    message_id = update.callback_query.message.message_id

    if command == 'refresh_top':
        replytext, reply_markup = getTop()
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
        context.bot.edit_message_reply_markup(chat_id = chat_id, message_id = message_id, reply_markup = reply_markup)
        if likes == 1:
            saved_messages_ids.append(message_id)
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


def getTop():
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
        matscount = usr['total_mats']
        replytext+=f'`{username}` - матюків `{matscount}`\n'

    replytext += "\nКулдаун топу - 5 хвилин"

    replytext = replytext.replace('@', '')

    keyboard = [[InlineKeyboardButton("Оновити", callback_data='refresh_top')]]
    reply_markup = InlineKeyboardMarkup(keyboard)
    return replytext, reply_markup


def saveToFile(dict):
    f = codecs.open(database_filename, "w", "utf-8")
    f.write(str(users))
    f.close()


def autodelete_message(context):
    chat_id = context.job.context[0]
    message_id = context.job.context[1]
    if message_id in saved_messages_ids:
        saved_messages_ids.remove(message_id)
        return

    context.bot.delete_message(chat_id=context.job.context[0], message_id=context.job.context[1])
    if len(context.job.context) > 2:
        try:
            context.bot.delete_message(chat_id=chat_id, message_id=message_id)
        except:
            pass


def openFile():
    if os.path.isfile(database_filename):
        global users
        users = eval(open(database_filename, 'r', encoding= 'utf-8').read())
    else:
        print ("File not exist")


def on_msg(update, context):
    global last_top
    try:
        message: Message = update.message
        if message is None:
            return

        if message.text is None and message.sticker is None:
            return

        is_old = False
        if message.date and (datetime.now(timezone.utc) - message.date).seconds > 300:
            is_old = True

        user_id = message.from_user.id
        username = message.from_user.name
        _chat_id = message.chat_id
        _message_id = message.message_id

        # chats control, you can define it in telegram bot father
        # if _chat_id != chat_id and user_id != admin_id:
            # return

        messageText = ""
        if message.text is not None:
            messageText = message.text.lower()
        elif message.sticker is not None:
            messageText = message.sticker.emoji

        # karma message
        if message.reply_to_message and message.reply_to_message.from_user.id and user_id != message.reply_to_message.from_user.id:
            karma_changed = increase_karma(message.reply_to_message.from_user.id, messageText)
            if karma_changed and not is_old:
                msg = context.bot.send_message(_chat_id, text=karma_changed)
                context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id])

        # commands
        if ("шарий" in messageText or "шарій" in messageText) and not is_old:
            msg = message.reply_video(quote=True, video=open('sh.MOV', mode='rb'))
            context.job_queue.run_once(autodelete_message, 30, context=[msg.chat_id, msg.message_id])
        if ("xiaomi" in messageText or "сяоми" in messageText) and not is_old:
            msg = context.bot.send_photo(_chat_id, reply_to_message_id=_message_id, photo=open('xiaomi.jpg', 'rb'))
            context.job_queue.run_once(autodelete_message, 30, context=[msg.chat_id, msg.message_id])
        if messageText == "гіт" and not is_old:
            reply_text = 'github.com/awitwicki/rude\\_bot'
            msg = context.bot.send_message(_chat_id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
            context.job_queue.run_once(autodelete_message, 300, context=[msg.chat_id, msg.message_id])
        if messageText == "карма" and not is_old:
            reply_text = get_karma(user_id)
            msg = context.bot.send_message(_chat_id, text=reply_text, parse_mode=ParseMode.MARKDOWN)
            context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id])
        if messageText == "топ" and not is_old:
            if not last_top or (datetime.now(timezone.utc) - last_top).seconds > 300:
                reply_text, reply_markup = getTop()
                msg = context.bot.send_message(_chat_id, text=reply_text, reply_markup=reply_markup, parse_mode=ParseMode.MARKDOWN)
                context.job_queue.run_once(autodelete_message, 300, context=[msg.chat_id, msg.message_id])
                last_top = datetime.now(timezone.utc)
        if messageText == "cat" or messageText == "кот" or messageText == "кіт" or messageText == "кицька" and not is_old:
            cat_url = get_random_cat_image_url()
            keyboard = [[InlineKeyboardButton("😻", callback_data='like_cat|0')]]
            reply_markup = InlineKeyboardMarkup(keyboard)
            msg = context.bot.send_photo(_chat_id, cat_url, reply_markup=reply_markup)
            context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id, _message_id])

        mats = count_mats(messageText)
        add_or_update_user(user_id, username, mats)

    except Exception as e:
        print(e)


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


def add_group(update, context):
    for member in update.message.new_chat_members:
        if not member.is_bot:
            gif_msg = update.message.reply_animation(animation=open("welcome.mp4", 'rb'))
            context.job_queue.run_once(autodelete_message, 60, context=[gif_msg.chat_id, gif_msg.message_id])

            keyboard = [[InlineKeyboardButton("Я обіцяю!", callback_data=member.id)]]
            reply_markup = InlineKeyboardMarkup(keyboard)
            # update.message.reply_text(f"{member.username} add group")
            message_text = f"Вітаємо {member.name} у нашому чаті! Ми не чат, а дружня, толерантна IT спільнота, яка поважає думку кожного, приєднавшись, ти згоджуєшся стати чемною частиною спільноти (та полюбити епл). I якшо не важко, пліз тут анкета на 8 питань https://forms.gle/pY6EjJhNRosUbd9P9"
            msg = update.message.reply_text(message_text, reply_markup=reply_markup)
            context.job_queue.run_once(autodelete_message, 300, context=[msg.chat_id, msg.message_id])


def main():
    global bot_id

    openFile()

    updater = Updater(bot_token, use_context=True)

    dp = updater.dispatcher
    dp.add_handler(MessageHandler(Filters.text, on_msg, edited_updates = True))
    dp.add_handler(CallbackQueryHandler(btn_clicked))
    dp.add_handler(MessageHandler(Filters.status_update.new_chat_members, add_group))

    # new videos
    j = updater.job_queue
    job_minute = j.run_repeating(callback_minute, interval=60, first=0)

    updater.start_polling()
    bot_id = updater.bot.id
    print("Bot is started.")
    updater.idle()


if __name__ == '__main__':
    main()
