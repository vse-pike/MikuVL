using Telegram.Bot;
using Telegram.Bot.Polling;

namespace TelegramBot.Telegram;

public class ReceiverService(
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler)
{
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = null,
            DropPendingUpdates = true,
        };

        await botClient.ReceiveAsync(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }
}