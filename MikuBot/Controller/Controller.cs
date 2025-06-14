using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using TelegramBot.Clients;
using TelegramBot.Services;

namespace TelegramBot.Controller;

[ApiController]
[Route("")]
public class VideoReadyController : ControllerBase
{
    private readonly long _defaultLimit;
    private readonly long _maxLimit;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<VideoReadyController> _logger;
    private readonly IUploader _uploader;
    private readonly IClient _client;
    private readonly IConfiguration _configuration;

    public VideoReadyController(
        ITelegramBotClient botClient,
        IConfiguration configuration,
        IClient client,
        IUploader uploader,
        AppConfigService appConfigService,
        ILogger<VideoReadyController> logger)
    {
        _defaultLimit = appConfigService.LightLimit;
        _maxLimit = appConfigService.HeavyLimit;
        _botClient = botClient;
        _logger = logger;
        _uploader = uploader;
        _client = client;
        _configuration = configuration;
    }

    [HttpPost]
    [Route("video-ready")]
    public async Task<IActionResult> VideoReady([FromBody] VideoReadyPayload request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[VideoReadyController] Received 'video-ready' with result: {Result}, file: {File}, telegramId: {Id}",
            request.Result, request.Filename, request.TelegramId);

        if (request.Result != "success")
        {
            throw new BusinessException(
                "[VideoReadyController] Received 'video-ready' with result: {Result}, file: {File}, telegramId: {Id}");
        }

        var downloadDirectory = _configuration["Downloader:DownloadDirectory"] ?? "/app/downloads";
        var fullPath = Path.Combine(downloadDirectory, request.Filename);

        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogError("[VideoReadyController] File not found at path: {Path}", fullPath);
            return Ok();
        }

        _ = Task.Run(() => _uploader.UploadAndSendAsync(
            request.TelegramId,
            request.MessageId,
            fullPath,
            request.Filename));

        return Ok();
    }

    [HttpPost]
    [Route("meta-ready")]
    public async Task<IActionResult> MetaReady([FromBody] MetaReadyPayload request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[VideoReadyController] Received 'meta-ready' with result: {Result}, size: {Size}, telegramId: {Id}",
            request.Result, request.Filesize, request.TelegramId);

        if (request.Result != "success")
        {
            throw new BusinessException(
                "[VideoReadyController] Received 'meta-ready' with result: {Result}, size: {Size}, telegramId: {Id}");
        }

        if (!IsWeightAllowed(request.Filesize))
        {
            await _botClient.EditMessageText(
                chatId: request.TelegramId,
                messageId: request.MessageId,
                text: "Файл такой... огромный 😳 Я не справлюсь с ним, прости~",
                cancellationToken: cancellationToken);

            return Ok();
        }

        await _client.SendDownloadRequest(request.Url, request.TelegramId, request.MessageId);

        if (request.Filesize < _maxLimit && request.Filesize > _defaultLimit)
        {
            await _botClient.EditMessageText(
                chatId: request.TelegramId,
                messageId: request.MessageId,
                text:
                "Он такой тяжёленький... Мне нужно чуть больше времени, чтобы аккуратно его распаковать~ \ud83e\udd7a",
                cancellationToken: cancellationToken);

            return Ok();
        }

        await _botClient.EditMessageText(
            chatId: request.TelegramId,
            messageId: request.MessageId,
            text: "Ура! Сейчас найду видео и аккуратно сложу его в коробочку~ 📦\nНемножечко подожди, хорошо? 🎶",
            cancellationToken: cancellationToken);

        return Ok();
    }

    private bool IsWeightAllowed(long fileSize)
    {
        if (fileSize <= _defaultLimit)
        {
            return true;
        }

        if (fileSize > _maxLimit)
        {
            _logger.LogWarning($"File size is too big: {fileSize}");
            return false;
        }

        return true;
    }

    public class VideoReadyPayload
    {
        public string Result { get; set; } = "";

        public long TelegramId { get; set; }

        public int MessageId { get; set; }
        public string Filename { get; set; } = "";

        public string? Error { get; set; }
    }

    public class MetaReadyPayload
    {
        public string Result { get; set; } = "";
        public string Url { get; set; } = "";
        public long TelegramId { get; set; }
        public int MessageId { get; set; }
        public long Filesize { get; set; }
        public string? Error { get; set; }
    }
}