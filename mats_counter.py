# -*- coding: utf8 -*-

#read mats from file
mats_filename = 'mats.txt'
stopwords = open(mats_filename, 'r', encoding= 'utf-8').read().split('\n')

def count_mats(message_text: str):
    count_mats: int = 0

    for stopword in stopwords:
        if stopword.lower() in message_text:
            count_mats += 1

    #limit
    if count_mats > 5:
        count_mats = 5
    return count_mats
