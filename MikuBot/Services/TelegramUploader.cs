using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using TelegramBot.Clients;

namespace TelegramBot.Services;

public interface IUploader
{
    Task UploadAndSendAsync(long telegramId, int messageId, string fullPath, string filename);
}

public class TelegramUploader : IUploader
{
    private readonly long _lightLimit;
    private readonly string _storageChannelId;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramUploader> _logger;
    private readonly ITelethonClient _telethonClient;

    public TelegramUploader(
        ITelegramBotClient botClient,
        ITelethonClient telethonClient,
        AppConfigService appConfigService,
        ILogger<TelegramUploader> logger)
    {
        _lightLimit = appConfigService.LightLimit;
        _storageChannelId = appConfigService.StorageChannelId;
        _botClient = botClient;
        _logger = logger;
        _telethonClient = telethonClient;
    }

    public async Task UploadAndSendAsync(long telegramId, int messageId, string fullPath, string filename)
    {
        try
        {
            var fileSize = new FileInfo(fullPath).Length;
            _logger.LogInformation($"[Uploader] Processing file {filename} with size {fileSize} bytes");

            if (fileSize <= _lightLimit)
            {
                await _botClient.EditMessageText(
                    chatId: telegramId,
                    messageId: messageId,
                    text: "Всё готово! Я уже держу его в руках~\nОтправляю тебе прямо сейчас! 💌");

                await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await _botClient.SendDocument(telegramId, InputFile.FromStream(stream, filename));
                _logger.LogInformation($"[Uploader] File {filename} sent via Bot API");
            }
            else
            {
                await _botClient.EditMessageText(
                    chatId: telegramId,
                    messageId: messageId,
                    text: "Загружаю в Telegram~ 💫\nНужно подождать..~");

                _logger.LogInformation($"[Uploader] File {filename} exceeds limit. Uploading via Telegram API...");
                var messageIdInChannel = await _telethonClient.UploadFileToChannelAsync(fullPath, filename);

                await _botClient.CopyMessage(
                    chatId: telegramId,
                    fromChatId: _storageChannelId!,
                    messageId: messageIdInChannel);
                _logger.LogInformation($"[Uploader] File {filename} sent via file_id after upload to Telegram API");
            }
        }
        catch (ApiRequestException apiEx)
        {
            _logger.LogError(apiEx, $"[Uploader] Telegram API error while sending {filename}");
            throw new BusinessException($"[Uploader] Telegram API error while sending {filename}: {apiEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Uploader] Error occurred while processing {filename}");
            throw;
        }
    }
}