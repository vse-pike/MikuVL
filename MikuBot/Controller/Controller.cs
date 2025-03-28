using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Telegram;

namespace TelegramBot.Controller;

[ApiController]
[Route("video-ready")]
public class VideoReadyController(
    ITelegramBotClient botClient,
    IConfiguration configuration,
    ILogger<VideoReadyController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> VideoReady([FromBody] VideoReadyPayload request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            $"[VideoReadyController] Request received with result: {request.Result}; file name: {request.Filename}; for telegramId: {request.TelegramId}");

        if (request.Result != "success")
        {
            return BadRequest();
        }

        var downloadDirectory = configuration["Downloader:DownloadDirectory"] ?? "/app/downloads";

        var fullPath = Path.Combine(downloadDirectory, request.Filename);

        logger.LogInformation($"[VideoReadyController] Full path to downloaded file: {fullPath}");

        if (!System.IO.File.Exists(fullPath))
        {
            logger.LogWarning("[VideoReadyController] Path doesn't exist");
            return BadRequest("File not found");
        }


        Task.Run(() => { return SendVideoAsync(request, fullPath); });


        return Ok();
    }

    private async Task SendVideoAsync(VideoReadyPayload request, string fullPath)
    {
        try
        {
            logger.LogInformation($"[SendVideoAsync] Старт отправки видео для {request.TelegramId}");

            await botClient.EditMessageText(
                chatId: request.TelegramId,
                messageId: request.MessageId,
                text: "Всё готово! Я уже держу его в руках~\nОтправляю тебе прямо сейчас! 💌"
            );

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            await botClient.SendDocument(
                chatId: request.TelegramId,
                document: InputFile.FromStream(stream, request.Filename)
            );

            logger.LogInformation($"[SendVideoAsync] Видео успешно отправлено: {request.Filename}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[SendVideoAsync] Ошибка при отправке файла {request.Filename}");
        }
    }

    public class VideoReadyPayload
    {
        public string Result { get; set; } = "";

        public long TelegramId { get; set; }

        public int MessageId { get; set; }
        public string Filename { get; set; } = "";

        public string? Error { get; set; }
    }
}