namespace TelegramBot;

public class CleanupWorker(ILogger<CleanupWorker> logger, IConfiguration config) : BackgroundService
{
    private readonly string _directory = config["Clients:DownloadDirectory"] ?? "/app/downloads";
    private readonly TimeSpan _maxAge = TimeSpan.FromMinutes(20);
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(20);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[CleanupWorker] CleanupWorker is running. Waching {Directory}", _directory);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!Directory.Exists(_directory))
                {
                    logger.LogWarning("[CleanupWorker] Directory - {Directory} - does not exist", _directory);
                }
                else
                {
                    var files = Directory.GetFiles(_directory);

                    foreach (var file in files)
                    {
                        try
                        {
                            var lastWrite = File.GetLastWriteTimeUtc(file);
                            if (DateTime.UtcNow - lastWrite > _maxAge)
                            {
                                File.Delete(file);
                                logger.LogInformation("[CleanupWorker] {File} was deleted", Path.GetFileName(file));
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"[CleanupWorker] Exception while deleting {file}: {ex}", file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"[CleanupWorker] Unhandled error: {ex}");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}