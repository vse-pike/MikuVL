using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Downloader;

namespace TelegramBot.Telegram;

public class UpdateHandler(ITelegramBotClient botClient, IDownloader downloader, ILogger<UpdateHandler> logger)
    : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var telegramId = GetChatIdOrDefault(update);

        if (!telegramId.HasValue)
        {
            return;
        }

        switch (update.Type)
        {
            case UpdateType.Message:
                await HandleTextMessageAsync(telegramId.Value, update, cancellationToken);

                return;
        }
    }

    private async Task HandleTextMessageAsync(long telegramId, Update update, CancellationToken cancellationToken)
    {
        try
        {
            var message = update.Message;

            if (message == null)
            {
                return;
            }

            logger.LogInformation(
                $"[UpdateHandler] Received text message: {message.Text} from user with id {telegramId}");

            if (message.Text == "/start")
            {
                using var stream = new FileStream("./Miku.gif", FileMode.Open, FileAccess.Read, FileShare.Read);

                await botClient.SendMessage(telegramId,
                   text:
                    "Приветик! Я Мику, твоя помощница 🎀 \nОтправь мне ссылочку на видео весом до 50 mb (например, с X, YouTube, Instagram или TikTok), и я постараюсь достать его для тебя, хорошо~?",
                    cancellationToken: cancellationToken);

                await botClient.SendAnimation(telegramId,
                    animation: InputFile.FromStream(stream),
                    cancellationToken: cancellationToken);
            }
            else
            {
                var extractedUrl = TryExtractUrl(message);

                if (string.IsNullOrEmpty(extractedUrl))
                {
                    logger.LogWarning("[UpdateHandler] Url was extracted unsuccessful");

                    await botClient.SendMessage(telegramId,
                        "Эээ… Кажется, я не смогла найти ссылочку в твоём сообщении 😿\nПопробуй ещё раз, пожалуйста! Я очень стараюсь~ 🌸",
                        cancellationToken: cancellationToken);

                    return;
                }

                logger.LogInformation($"[UpdateHandler] Url was extracted: {extractedUrl}");

                var outputMessage = await botClient.SendMessage(telegramId,
                    "Начинаю поиск ^_^",
                    cancellationToken: cancellationToken);

                var result = await downloader.SendDownloadRequest(extractedUrl, telegramId, outputMessage.MessageId);

                logger.LogInformation($"[UpdateHandler] Download request result: {result}");

                if (result)
                {
                    await botClient.EditMessageText(
                        chatId: telegramId,
                        messageId: outputMessage.MessageId,
                        text: "Ура! Сейчас найду видео и аккуратно сложу его в коробочку~ 📦\nНемножечко подожди, хорошо? 🎶",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.EditMessageText(
                        chatId: telegramId,
                        messageId: outputMessage.MessageId,
                        text:
                        "Ой-ой… Кажется, что-то пошло не так, и я не смогла достать видео 😢\nМожет, попробуем ещё раз чуть позже?..",
                        cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception _)
        {
            logger.LogCritical($"[UpdateHandler] Unhanded error: {_}");

            await botClient.SendMessage(telegramId,
                "Ой-ой… Кажется, что-то пошло не так, и я не смогла достать видео 😢\nМожет, попробуем ещё раз чуть позже?..",
                cancellationToken: cancellationToken);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient telegramBotClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static long? GetChatIdOrDefault(Update update)
    {
        return update.Message?.Chat.Id;
    }

    private static string? TryExtractUrl(Message message)
    {
        var text = (message.Text ?? message.Caption) ?? string.Empty;

        var regex = new Regex(@"https?:\/\/[^\s]+", RegexOptions.IgnoreCase);
        var match = regex.Match(text);

        return match.Success ? match.Value : null;
    }
}