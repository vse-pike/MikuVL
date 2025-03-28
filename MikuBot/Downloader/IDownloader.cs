namespace TelegramBot.Downloader;

public interface IDownloader
{
    Task<bool> SendDownloadRequest(string url, long telegramId, int messageId);
}