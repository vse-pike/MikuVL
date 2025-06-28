import json
from yt_dlp import YoutubeDL
from loguru import logger
from clients.miku_client import send_meta_ready_callback

ydl_meta_opts = {
    'skip_download': True,
    'quiet': True,
    'no_warnings': True,
    "format": "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
    "merge_output_format": "mp4"
}

ydl_meta = YoutubeDL(ydl_meta_opts)


def weight_check_process(data):
    url = data.get("url")
    telegram_id = data.get("telegramId")
    message_id = data.get("messageId")

    try:
        info = ydl_meta.extract_info(url, download=False)
        logger.info(f"YT-DLP info:\n{json.dumps(info, indent=2)}")
        filesize = info.get('filesize') or info.get('filesize_approx')

        logger.info({
            "result": "success",
            "telegramId": telegram_id,
            "messageId": message_id,
            "url": url,
            "filesize": filesize,
        })

        send_meta_ready_callback({
            "result": "success",
            "telegramId": telegram_id,
            "messageId": message_id,
            "url": url,
            "filesize": filesize,
        })

    except Exception as e:
        logger.error(f"Ошибка проверки веса файла: {e}")
        send_meta_ready_callback({
            "result": "failed",
            "telegramId": telegram_id,
            "messageId": message_id,
            "filesize": None,
            "url": url,
            "error": str(e)
        })
