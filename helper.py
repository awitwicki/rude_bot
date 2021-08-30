import random
import requests
import re

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
