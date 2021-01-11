import requests
import re

def get_random_cat_image_url():
    catapi_url = 'https://api.thecatapi.com/v1/images/search?mime_types=jpg,png'
    resp = requests.get(catapi_url)
    content = resp.text
    text = re.findall(r'https:\/\/[\w.,@?^=%&:/~+#-]*', content)

    return text[0]
