# autohater bot
For telegram chat [@rivne_autochat](https://t.me/autorivne)

## Install

Use next environment variables:

* `AUTOHATERBOT_TELEGRAM_TOKEN={YOUR_TOKEN}` - telegram token

    (other variables is not necessary and have default values)

* `AUTOHATERBOT_DELETE_TIMEOUT=180` - time before bot messages being deleted
* `AUTOHATERBOT_ALLOWED_CHATS=-10010101,-10000101010` - whitelist chats. If it empty or not added to envs, whitelist mode will be turned off.

**Python:** Add mentioned env vars to the system environment.

**Docker compose:**  create `.env` file with env vars.

**Ubuntu:** 
1. Copy a file autohater_bot.service to /etc/systemd/system/
2. Edit it with needed changes. For example: `ExecStart=/bin/bash -c "export PYTHONPATH=/home/user/bot/autohater/autohater_bot; export AUTOHATERBOT_TELEGRAM_TOKEN=your_token_here; export AUTOHATERBOT_DELETE_TIMEOUT=180; /usr/local/bin/python3.8 /home/deploy/bot/autohater/autohater_bot/src/main.py"`
3. Reload systemd `sudo systemctl daemon-reload`
4. Enable your new service `sudo systemctl enable autohater_bot` As a result you receive a message about successful symlink creation.
5. Run this service `systemctl start autohater_bot`

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
