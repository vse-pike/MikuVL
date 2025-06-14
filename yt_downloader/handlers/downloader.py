import os
from yt_dlp import YoutubeDL
from loguru import logger
from clients.miku_client import send_video_ready_callback
from urllib.parse import urlparse

DOWNLOAD_DIR = os.environ.get("DOWNLOAD_DIR", "../downloads")

ydl_opts = {
    "format": "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
    "merge_output_format": "mp4",
    "outtmpl": os.path.join(DOWNLOAD_DIR, "%(id)s.%(ext)s"),
    "noplaylist": True,
}

ydl = YoutubeDL(ydl_opts)


LIGHT_DOMAINS = ["tiktok.com", "instagram.com", "x.com", "twitter.com"]

ydl_light_opts = {
    "format": "best",
    "merge_output_format": "mp4",
    "list_formats": True,
    "outtmpl": os.path.join(DOWNLOAD_DIR, "%(id)s.%(ext)s"),
}

ydl_light = YoutubeDL(ydl_opts)


def download_process(data):
    url = data.get("url")
    telegram_id = data.get("telegramId")
    message_id = data.get("messageId")

    try:

        if is_light_domain(url):
            logger.info(f"Using ydl_light: {url}")

            info = ydl_light.extract_info(url, download=False)
            filename = ydl_light.prepare_filename(info)

            saved_name = os.path.basename(filename)

        else:
            logger.info(f"Using ydl: {url}")

            info = ydl.extract_info(url, download=True)
            filename = ydl.prepare_filename(info)

            saved_name = os.path.basename(filename)

        logger.info(
            {
                "result": "success",
                "telegramId": telegram_id,
                "messageId": message_id,
                "url": url,
                "filename": saved_name,
            }
        )

        send_video_ready_callback(
            {
                "result": "success",
                "telegramId": telegram_id,
                "messageId": message_id,
                "filename": saved_name,
            }
        )

    except Exception as e:
        logger.error(f"Ошибка скачивания файла: {e}")
        send_video_ready_callback(
            {
                "result": "failed",
                "telegramId": telegram_id,
                "messageId": message_id,
                "filename": "None",
                "error": str(e),
            }
        )


def is_light_domain(url):
    try:
        hostname = urlparse(url).hostname or ""
        return any(hostname.endswith(domain) for domain in LIGHT_DOMAINS)
    except Exception:
        return False
