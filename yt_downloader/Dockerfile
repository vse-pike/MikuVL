FROM python:3.11-slim

WORKDIR /app

COPY . .
RUN pip install --no-cache-dir -r requirements.txt

RUN mkdir -p /app/downloads
VOLUME /app/downloads

EXPOSE 5005

CMD ["python", "app.py"]