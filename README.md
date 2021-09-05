# autohater bot
For telegram chat [@rivne_autochat](https://t.me/autorivne)

## Install

Use next environment variables:

* `AUTOHATERBOT_TELEGRAM_TOKEN={YOUR_TOKEN}` - telegram token, required

    (other variables have default values)

* `AUTOHATERBOT_DELETE_TIMEOUT=180` - time before bot messages being deleted
* `AUTOHATERBOT_ALLOWED_CHATS=-10010101,-10000101010` - whitelist chats. If it empty or not added to envs, whitelist mode will be turned off.

**Python:** Add mentioned env vars to the system environment.

**Docker compose:**  create `.env` file with env vars.

## Run


### Docker compose

```
docker-compose up -d
```

### Python

```
pip3 install -r src/requirements.txt
python main.py
```

### TODO
* make filters and reactions configurable, add file-based storage
