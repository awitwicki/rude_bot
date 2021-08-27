FROM python:buster
WORKDIR /app
RUN mkdir data
COPY . .
RUN pip3 install -r requirements.txt
ENTRYPOINT ["python"]
CMD ["main.py"]
