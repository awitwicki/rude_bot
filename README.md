# rude_bot
Karmabot for telegram chat [@rude_chat]('https://t.me/rude_chat')

## Install

Use next environment variables:

* `RUDEBOT_TELEGRAM_TOKEN={YOUR_TOKEN}` - telegram token

    (other variables is not necessarty and have default values)

* `RUDEBOT_FLOOD_TIMEOUT=10` - cooldown to allow +- karma per chat, default 30 seconds
* `RUDEBOT_DELETE_TIMEOUT=30` - time before bot messages being deleted
* `RUDEBOT_DATABASE_FILENAME=rudebot_db.json` - stored database name
* `RUDEBOT_ALLOWED_CHATS=-10010101,-10000101010` - whitelist chats. If it empty, whitelist mode will be turned off.

**Python:** Add to system environment that variables.

## Run

### Python

```
cd app/
pip3 install -r requirements.txt
python main.py
```
