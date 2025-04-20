using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Clients;
using TelegramBot.Services;

namespace TelegramBot.Telegram;

public class UpdateHandler(ITelegramBotClient botClient, IClient client, ILogger<UpdateHandler> logger)
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
                await StartCommand(telegramId, cancellationToken);
            }
            else
            {
                await DownloadCommand(message, telegramId, cancellationToken);
            }
        }
        catch (BusinessException _)
        {
            logger.LogError($"[UpdateHandler] BusinessException: {_}");

            await botClient.SendMessage(telegramId,
                "Ой-ой… Кажется, что-то пошло не так, и я не смогла достать видео 😢\nМожет, попробуем ещё раз чуть позже?..",
                cancellationToken: cancellationToken);
        }
        catch (Exception _)
        {
            logger.LogCritical($"[UpdateHandler] Unhandled error: {_}");

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

    private async Task StartCommand(long telegramId, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream("./Miku.gif", FileMode.Open, FileAccess.Read, FileShare.Read);

        var isPremium = await DbService.CheckUsersPremium(telegramId);
        
        logger.LogInformation($"[UpdateHandler] Starting command: {isPremium}");

        if (isPremium)
        {
            await botClient.SendMessage(telegramId,
                text:
                "Приветик! Я Мику, твоя помощница 🎀\nОтправь мне ссылочку на видео (например, с X, YouTube, Instagram или TikTok), и я постараюсь достать его для тебя \n\nТы у меня особенный 💎\nТак что можешь присылать ссылочки на видео весом до 1 гб~ \nЯ постараюсь найти и аккуратненько всё тебе передать, хорошо? ✨",
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(telegramId,
                text:
                "Приветик! Я Мику, твоя помощница 🎀 \nОтправь мне ссылочку на видео весом до 50 mb (например, с X, YouTube, Instagram или TikTok), и я постараюсь достать его для тебя, хорошо~?",
                cancellationToken: cancellationToken);
        }

        await botClient.SendAnimation(telegramId,
            animation: InputFile.FromStream(stream),
            cancellationToken: cancellationToken);
    }

    private async Task DownloadCommand(Message message, long telegramId, CancellationToken cancellationToken)
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

        await client.SendMetaRequest(extractedUrl, telegramId, outputMessage.MessageId);
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