using System.Net.Http.Json;

namespace TelegramBot.Downloader;

public class Downloader(HttpClient httpClient, IConfiguration configuration, ILogger<Downloader> logger) : IDownloader
{
    private readonly string _downloadServiceUrl =
        configuration["Downloader:ServiceUrl"] ?? "http://localhost:5005/download";

    public async Task<bool> SendDownloadRequest(string url, long telegramId, int messageId)
    {
        if (!IsPossibleToDownload(url))
        {
            logger.LogWarning($"[Downloader] {url} have unsupported social media type");

            return false;
        }

        var requestBody = new { url, telegramId, messageId };

        try
        {
            logger.LogInformation($"[Downloader] Download request in progress. Request body: {requestBody}");

            var response = await httpClient.PostAsJsonAsync(_downloadServiceUrl, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    $"[Downloader] Download request failed. Response with status code {response.StatusCode} - {response.Content}");

                return false;
            }

            return true;
        }
        catch (Exception _)
        {
            logger.LogCritical($"[Downloader] Unhanded error: {_}");
            return false;
        }
    }

    private bool IsPossibleToDownload(string url)
    {
        Uri.TryCreate(url, UriKind.Absolute, out var uri);

        var host = uri?.Host.ToLower() ?? string.Empty;

        return host.Contains("x.com") || host.Contains("twitter.com") ||
               host.Contains("tiktok.com") ||
               host.Contains("instagram.com") ||
               host.Contains("youtube.com") || host.Contains("youtu.be");
    }
}