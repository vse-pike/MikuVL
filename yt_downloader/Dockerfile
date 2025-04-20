FROM python:3.11-slim

WORKDIR /app
RUN apt-get update && apt-get install -y ffmpeg
COPY . .
RUN pip install --no-cache-dir -r requirements.txt

RUN mkdir -p /app/downloads
VOLUME /app/downloads

EXPOSE 5005

CMD ["python", "main.py"]