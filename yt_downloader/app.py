import threading
import requests
import os
from flask import Flask, request, jsonify
from loguru import logger
from yt_dlp import YoutubeDL

app = Flask(__name__)

DOWNLOAD_DIR = os.environ.get("DOWNLOAD_DIR", "../downloads")
MIKU_BASE = os.environ.get("MIKU_BASE", "0.0.0.0:8080")
HOST = os.environ.get("HOST", "127.0.0.1")
MAX_FILESIZE = os.environ.get("MAX_FILESIZE", 50 * 1024 * 1024)

os.makedirs(DOWNLOAD_DIR, exist_ok=True)

ydl_opts = {
    "outtmpl": os.path.join(DOWNLOAD_DIR, "%(id)s.%(ext)s"),
    "format": "mp4",
    "max_filesize": MAX_FILESIZE,
    "noplaylist": True
}
ydl = YoutubeDL(ydl_opts)

logger.add("/app/logs/yt_downloader.log", format="{time} {level} {message}", level="DEBUG", rotation="10 MB", compression="zip")

@app.route("/download", methods=["POST"])
def download():
    data = request.get_json()
    url = data.get("url")
    telegramId = data.get("telegramId")
    messageId = data.get("messageId")

    logger.info(f"[→] Был вызван метод /download с json: {data}")

    if not url:
        logger.error(f"[!] Не был передан url")
        return jsonify({"error": "Missing url"}), 400

    threading.Thread(target=process_download, args=(url, telegramId, messageId), daemon=True).start()

    logger.info(f"[→] Процесс скачивания запущен")

    return jsonify({}),200


def process_download(url, telegramId, messageId):
    try:
        info = ydl.extract_info(url, download=True)
        filename = ydl.prepare_filename(info)

        saved_name = os.path.basename(filename)

        logger.info(f"[→] Файл {saved_name} скачен")

        send_callback({
            "result": "success",
            "telegramId": telegramId,
            "messageId": messageId,
            "filename": filename
        })

    except Exception as e:
        logger.error(f"[!] Ошибка скачивания файла для {telegramId}: {e}")
        send_callback({
            "result": "failed",
            "telegramId": telegramId,
            "messageId": messageId,
            "filename": None,
            "error": str(e)
        })


def send_callback(payload):
    try:
        logger.info(f"[.] http://{MIKU_BASE}/video-ready")
        response = requests.post(f"http://{MIKU_BASE}/video-ready", json=payload)
        
        if response.status_code >= 400:
            logger.warning(f"[!] Вебхук вернул ошибку: {response.status_code} - {response.text}")

        logger.info(f"[→] Вебхук отправлен: {response.status_code}")
    except Exception as e:
        logger.error(f"[!] Ошибка при отправке вебхука: {e}")


if __name__ == "__main__":
    app.run(host=HOST, port=5005)
