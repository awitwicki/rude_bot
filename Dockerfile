FROM python:buster
WORKDIR /app
RUN mkdir data
COPY ./requirements.txt .
RUN pip3 install -r requirements.txt
COPY . .
ENTRYPOINT ["python"]
CMD ["main.py"]
