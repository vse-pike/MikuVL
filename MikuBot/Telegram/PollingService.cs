namespace TelegramBot.Telegram;

public class PollingService(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<ReceiverService>();

                await receiver.ReceiveAsync(stoppingToken);
            }

            catch (Exception _)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}