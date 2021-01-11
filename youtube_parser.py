import requests
import re

def cut_url(content):
    text = re.findall(r'url="https:\/\/[\w.,@?^=%&:/~+#-]*', content)

    #idk if this a good way
    return text[0].replace('url="', '').replace('?version=3', '').replace('/v/', '/watch?v=')


def get_urls(url):
    resp = requests.get(url)
    urls = [cut_url(link) for link in resp.text.split('\n') if 'media:content' in link]
    return urls


def get_new_urls(old_urls, new_urls):
    return list(set(new_urls)-set(old_urls))
