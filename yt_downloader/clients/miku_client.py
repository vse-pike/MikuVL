import requests
import os
from loguru import logger

MIKU_BASE = os.environ.get("MIKU_BASE", "localhost:8080")

def send_meta_ready_callback(payload):
    try:
        url = f"http://{MIKU_BASE}/meta-ready"
        response = requests.post(url, json=payload)
        logger.info(f"/meta-ready => {response.status_code}")
        if response.status_code >= 400:
            logger.warning(f"Ошибка от сервера: {response.status_code} - {response.text}")
    except Exception as e:
        logger.error(f"Ошибка отправки мета-вебхука: {e}")

def send_video_ready_callback(payload):
    try:
        url = f"http://{MIKU_BASE}/video-ready"
        response = requests.post(url, json=payload)
        logger.info(f"/video-ready => {response.status_code}")
        if response.status_code >= 400:
            logger.warning(f"Ошибка от сервера: {response.status_code} - {response.text}")
    except Exception as e:
        logger.error(f"Ошибка отправки видео-вебхука: {e}")