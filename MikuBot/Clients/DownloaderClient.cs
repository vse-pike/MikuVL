using System.Net.Http.Json;

namespace TelegramBot.Clients;

public interface IClient
{
    Task SendDownloadRequest(string url, long telegramId, int messageId);
    Task SendMetaRequest(string url, long telegramId, int messageId);
}

public class DownloaderClient : IClient
{
    private readonly string _downloadServiceUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownloaderClient> _logger;

    public DownloaderClient(HttpClient httpClient, IConfiguration configuration, ILogger<DownloaderClient> logger)
    {
        _downloadServiceUrl = configuration["DOWNLOADER_URL"] ?? "localhost:5005";
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendMetaRequest(string url, long telegramId, int messageId)
    {
        if (!IsPossibleToDownload(url))
        {
            _logger.LogWarning($"[DownloaderClient] {url} have unsupported social media type");

            throw new BusinessException($"[DownloaderClient] {url} have unsupported social media type");
        }

        var requestBody = new { url, telegramId, messageId };

        try
        {
            _logger.LogInformation(
                $"[DownloaderClient] Meta request in progress. Request url: http://{_downloadServiceUrl}/weight-check");
            _logger.LogInformation($"[DownloaderClient] Meta request in progress. Request body: {requestBody}");

            var response = await _httpClient.PostAsJsonAsync($"http://{_downloadServiceUrl}/weight-check", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    $"[DownloaderClient] Meta request failed. Response with status code {response.StatusCode} - {response.Content}");

                throw new BusinessException($"[DownloaderClient] Meta request failed. Response with status code {response.StatusCode} - {response.Content}");
            }

            _logger.LogInformation(
                $"[DownloaderClient] Meta request for {telegramId} with {url} - success");
        }
        catch (Exception _)
        {
            _logger.LogCritical($"[DownloaderClient] Unhandled error: {_}");
            throw;
        }
    }

    public async Task SendDownloadRequest(string url, long telegramId, int messageId)
    {
        var requestBody = new { url, telegramId, messageId };

        try
        {
            _logger.LogInformation(
                $"[DownloaderClient] Download request in progress. Request url: http://{_downloadServiceUrl}/download");
            _logger.LogInformation($"[DownloaderClient] Download request in progress. Request body: {requestBody}");

            var response = await _httpClient.PostAsJsonAsync($"http://{_downloadServiceUrl}/download", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"[DownloaderClient] Download request failed. Response with status code {response.StatusCode} - {response.Content}");

                throw new BusinessException($"[DownloaderClient] Download request failed. Response with status code {response.StatusCode} - {response.Content}");
            }
        }
        catch (Exception _)
        {
            _logger.LogCritical($"[DownloaderClient] Unhandled error: {_}");
            throw;
        }
    }

    private static bool IsPossibleToDownload(string url)
    {
        Uri.TryCreate(url, UriKind.Absolute, out var uri);

        var host = uri?.Host.ToLower() ?? string.Empty;

        return host.Contains("x.com") || host.Contains("twitter.com") ||
               host.Contains("tiktok.com") ||
               host.Contains("instagram.com") ||
               host.Contains("youtube.com") || host.Contains("youtu.be");
    }
}