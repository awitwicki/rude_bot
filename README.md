# RudeBot
## Karmabot for telegram chat [@rude_chat](https://t.me/rude_chat)


[![CircleCI](https://img.shields.io/badge/Telegram-RudeChat-blue)](https://www.youtube.com/watch?v=dQw4w9WgXcQ)
![License](https://img.shields.io/badge/License-Apache%20License%202.0-blue)
![Tests](https://img.shields.io/github/stars/awitwicki/rude_bot)
![Tests](https://img.shields.io/github/languages/top/awitwicki/rude_bot)
![Tests](https://img.shields.io/badge/dotnet%20version-6.0-blue)
![Tests](https://img.shields.io/github/forks/awitwicki/rude_bot)
![Tests](https://img.shields.io/github/issues-pr/awitwicki/rude_bot)
![Tests](https://img.shields.io/github/last-commit/awitwicki/rude_bot)

![Waterfall](data/media/cat.jpg)

## Install

Use next environment variables:

* `RUDEBOT_TELEGRAM_TOKEN={YOUR_TOKEN}` - telegram token

    (other variables is not necessary and have default values)

* `RUDEBOT_FLOOD_TIMEOUT=10` - cooldown to allow +- karma per chat, default 30 seconds
* `RUDEBOT_DELETE_TIMEOUT=30` - time before bot messages being deleted
* `RUDEBOT_ALLOWED_CHATS=-10010101,-10000101010` - whitelist chats. If it empty or not added to envs, whitelist mode will be turned off.

**Docker compose:**  create `.env` file and fill it with that variables.

## Run

```
docker-compose up --build -d
```
