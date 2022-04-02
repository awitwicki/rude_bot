# RudeBot
Karmabot for telegram chat [@rude_chat](https://t.me/rude_chat)
![Waterfall](data/media/cat.jpg)

## Install

Use next environment variables:

* `RUDEBOT_TELEGRAM_TOKEN={YOUR_TOKEN}` - telegram token

    (other variables is not necessary and have default values)

* `RUDEBOT_FLOOD_TIMEOUT=10` - cooldown to allow +- karma per chat, default 30 seconds
* `RUDEBOT_DELETE_TIMEOUT=30` - time before bot messages being deleted
* `RUDEBOT_DATABASE_FILENAME=rudebot_db.json` - stored database name
* `RUDEBOT_ALLOWED_CHATS=-10010101,-10000101010` - whitelist chats. If it empty or not added to envs, whitelist mode will be turned off.

**Python:** Add to system environment that variables.

**Docker compose:**  create `.env` file and fill it with that variables.

## Run

```
docker-compose up --build -d
```
