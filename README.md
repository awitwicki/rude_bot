# rude_bot
Karmabot for telegram chat [@rude_chat](https://t.me/rude_chat)
![Waterfall](data/media/cat.jpg)

## Install

Use next environment variables:

* `RUDEBOT_TELEGRAM_TOKEN={YOUR_TOKEN}` - telegram token

    (other variables is not necessarty and have default values)

* `RUDEBOT_DELETE_TIMEOUT=30` - time before bot messages being deleted
* `RUDEBOT_ALLOWED_CHATS=-10010101,-10000101010` - whitelist chats. If it empty or not added to envs, whitelist mode will be turned off.

**Python:** Add to system environment that variables.

**Docker compose:**  create `.env` file and fill it with that variables.

## Run


### Docker compose

```
docker-compose up -d
```

### Python

```
pip3 install -r requirements.txt
python main.py
```
