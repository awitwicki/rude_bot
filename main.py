# -*- coding: utf8 -*-
#/usr/bin/python3.7

import datetime
from telegram import bot
from telegram.ext import Updater, Filters, MessageHandler, CallbackQueryHandler
from telegram import InlineKeyboardMarkup, InlineKeyboardButton, ParseMode
import os
import codecs

# https://github.com/python-telegram-bot/python-telegram-bot/wiki/Transition-guide-to-Version-12.0
bot_token = 'TOKEN'
bot_id = None

#bot will delete his owm nessage after defined time
destruction_timeout = 20

database_filename = 'users.json'

stopwords = ["залупа","суха","сиха","сри","срали","срать","д@ун", "даун","хуи","хуё","хуе","хуй", "бля","Cyка", "nahui","говн","pizd","pidar","cunt","Пизд","хер","Пидарас","Пидор","Бляд","Блят","Гандон","Выеби","Еби","Блядство","Выебон","Выебать","Вхуюжить","Гомосек","Долбоёб","Далбоёб","Ебло","Еблище","Ебать","Ебическая сила","Ебунок","Еблан","Ёбнуть","Ёболызнуть","Ебош","Заебал","Заебатый","Злаебучий","Заёб","Иди на хуй","Колдоебина","Манда","Мандовошка","Мокрощелка","Наебка","Наебал","Наебаловка","Напиздеть","Отъебись","Охуеть","Отхуевертить","Опизденеть","Охуевший","Отебукать","Пизда","Пидарас","Пиздатый","Пиздец","Пизданутый","Поебать","Поебустика","Проебать","Подзалупный","Пизденыш","Припиздак","Разъебать","Распиздяй","Разъебанный","Сука","Сучка","Трахать","Уебок","Уебать","Угондошить","Уебан","Хитровыебанный","Хуй","Хуйня","Хуета","Хуево","Хуесос","Хуеть","Хуевертить","Хуеглот","Хуистика","Членосос","Членоплет","Шлюха"]
increase_words = ['+','спасибо','дякую','благодарю', '👍', '😁', '😂', '😄', '😆', 'хаха']
decrease_words = ['-', '👎']

users = {}
user_karma = {}

regex_letters = {
    'а' : ['а', 'a', '@'],
    'б' : ['б', '6', 'b'],
    'в' : ['в', 'b', 'v'],
    'г' : ['г', 'r', 'g'],
    'д' : ['д', 'd', 'g'],
    'е' : ['е', 'e'],
    'ё' : ['ё', 'е', 'e'],
    'ж' : ['ж', 'zh', '*'],
    'з' : ['з', '3', 'z'],
    'и' : ['и', 'u', 'i'],
    'й' : ['й', 'u', 'y', 'i'],
    'к' : ['к', 'k', 'i{', '|{'],
    'л' : ['л', 'l', 'ji'],
    'м' : ['м', 'm'],
    'н' : ['н', 'h', 'n'],
    'о' : ['о', 'o', '0'],
    'п' : ['п', 'n', 'p', '/7'],
    'р' : ['р', 'r', 'p'],
    'с' : ['с', 'c', 's'],
    'т' : ['т', 'm', 't'],
    'у' : ['у', 'y', 'u'],
    'ф' : ['ф', 'f'],
    'х' : ['х', 'x', 'h', 'к', '}{', ')('],
    'ц' : ['ц', 'c', 'u,'],
    'ч' : ['ч', 'ch'],
    'ш' : ['ш', 'sh'],
    'щ' : ['щ', 'sch'],
    'ь' : ['ь', 'b'],
    'ы' : ['ы', 'bi'],
    'ъ' : ['ъ'],
    'э' : ['э', 'е', 'e'],
    'ю' : ['ю', 'io'],
    'я' : ['я', 'ya'],
}

last_top = None

#Todo:
#ignore karmaspam from users
# def check_user_for_karma(user_id: int, dest_user_id: int):
#     try:
#         usr_ch = user_karma[user_id]
#     except:
#         return True

def count_mats(message_text):
    count_mats: int = 0
    for regex_letter in regex_letters:
        for regex_char in regex_letters[regex_letter]:
            messageText = message_text.replace(regex_char, regex_letter)

    for stopword in stopwords:
        if stopword.lower() in message_text:
            count_mats += 1

    #limit
    if count_mats > 5:
        count_mats = 5
    return count_mats

def get_karma(user_id : int):
    user = users[user_id]

    replytext = f"Привіt {user['username']}, tвоя карма:\n\n"
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
            replytext += 'підняли '
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

    replytext += f'карму користувача {_username} до вартості {new_karma}!'
    saveToFile(users)

    return replytext

def stats(update, context):
    command = update.callback_query.data
    if command == 'refresh_top':
        replytext, reply_markup = getTop()
        replytext += f'\n`Оновлено UTC {datetime.datetime.utcnow()}`'
        query = update.callback_query
        query.edit_message_text(text=replytext, reply_markup=reply_markup, parse_mode=ParseMode.MARKDOWN)
        return

def getTop():
    replytext = "*Топ 10 карми чату:*\n"
    users_list = [ v for k, v in users.items()]
    sorted_users_list = sorted(users_list, key = lambda i: i['karma'], reverse = True)[:10]

    for usr in sorted_users_list:
        username = usr['username']
        karma = usr['karma']
        replytext+=f'`{username}` - карма `{karma}`\n'

    replytext += "\n*Топ 10 актив чату:*\n"
    sorted_users_list = sorted(users_list, key = lambda i: i['total_messages'], reverse = True)[:10]

    for usr in sorted_users_list:
        username = usr['username']
        messagescount = usr['total_messages']
        replytext+=f'`{username}` - повідомлень `{messagescount}`\n'

    replytext += "\n*Топ 10 емоціонали чату:*\n"
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
    context.bot.delete_message(chat_id=context.job.context[0], message_id=context.job.context[1])

def openFile():
    if os.path.isfile(database_filename):
        global users
        users = eval(open(database_filename, 'r', encoding= 'utf-8').read())
    else:
        print ("File not exist")

def on_msg(update, context):
    global last_top
    try:
        message = update.message
        if message is None:
            return

        if message.text == None:
            return

        is_old = False
        if message.date and (datetime.datetime.utcnow() - message.date).seconds > 300:
            is_old = True

        user_id = message.from_user.id
        username = message.from_user.name
        _chat_id = message.chat_id

        # chats control, you can define it in telegram bot father
        # if _chat_id != chat_id and user_id != admin_id:
            # return

        messageText = message.text.lower()

        # karma message
        if message.reply_to_message and message.reply_to_message.from_user.id and user_id != message.reply_to_message.from_user.id:
            karma_changed = increase_karma(message.reply_to_message.from_user.id, messageText)
            if karma_changed and not is_old:
                msg = context.bot.send_message(_chat_id, text=karma_changed)
                context.job_queue.run_once(autodelete_message, destruction_timeout, context=[msg.chat_id, msg.message_id])

        # commands
        if ("шарий" in messageText or "шарій" in messageText) and not is_old:
            msg = message.reply_video(quote = True ,video=open('sh.MOV', mode='rb'))
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
            if not last_top or (datetime.datetime.utcnow() - last_top).seconds > 300:
                reply_text, reply_markup = getTop()
                msg = context.bot.send_message(_chat_id, text=reply_text, reply_markup=reply_markup, parse_mode=ParseMode.MARKDOWN)
                context.job_queue.run_once(autodelete_message, 300, context=[msg.chat_id, msg.message_id])
                last_top = datetime.datetime.utcnow()

        mats = count_mats(messageText)
        add_or_update_user(user_id, username, mats)

    except Exception as e:
        print(e)

def main():
    global bot_id

    openFile()

    updater = Updater(bot_token, use_context=True)

    dp = updater.dispatcher
    dp.add_handler(MessageHandler(Filters.text, on_msg, edited_updates = True))
    dp.add_handler(CallbackQueryHandler(stats))

    updater.start_polling()
    bot_id = updater.bot.id
    print("Bot is started.")
    updater.idle()

if __name__ == '__main__':
    main()
