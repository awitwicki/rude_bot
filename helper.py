import cachetools.func
import random
import json
import requests
import re
import urllib.request

better_advices: list

with open('better.txt', 'r', encoding='utf-8') as f:
    better_advices = f.readlines()


def get_random_cat_image_url():
    catapi_url = 'https://api.thecatapi.com/v1/images/search?mime_types=jpg,png'
    resp = requests.get(catapi_url)
    content = resp.text
    text = re.findall(r'https:\/\/[\w.,@?^=%&:/~+#-]*', content)

    return text[0]


def get_random_better_advice():
    return random.choice(better_advices)

@cachetools.func.ttl_cache(maxsize=1, ttl=60 * 5)
def get_tesla_stock():
    try:
        resp = urllib.request.urlopen('https://query2.finance.yahoo.com/v10/finance/quoteSummary/tsla?modules=price')
        data = json.loads(resp.read())
        price = data['quoteSummary']['result'][0]['price']['regularMarketPrice']['raw']
        return f'${price}'
    except:
        return 'nothing'
