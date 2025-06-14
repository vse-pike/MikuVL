import threading
import os
from flask import Flask, request, jsonify
from loguru import logger

from handlers.weight_check import weight_check_process
from handlers.downloader import download_process

app = Flask(__name__)

DOWNLOAD_DIR = os.environ.get("DOWNLOAD_DIR", "../downloads")
HOST = os.environ.get("HOST", "0.0.0.0")
PORT = int(os.environ.get("PORT", 5005))

os.makedirs(DOWNLOAD_DIR, exist_ok=True)

logger.add(
    "/app/logs/yt_downloader.log",
    format="{time} {level} {message}",
    level="DEBUG",
    rotation="10 MB",
    compression="zip",
)


@app.route("/weight-check", methods=["POST"])
def weight_check():
    data = request.get_json()
    threading.Thread(target=weight_check_process, args=(data,), daemon=True).start()
    return jsonify({}), 200


@app.route("/download", methods=["POST"])
def download():
    data = request.get_json()
    threading.Thread(target=download_process, args=(data,), daemon=True).start()
    return jsonify({}), 200


if __name__ == "__main__":
    app.run(host=HOST, port=PORT)
