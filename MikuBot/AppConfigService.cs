namespace TelegramBot;

public class AppConfigService
{
    public string ApiId { get; }
    public string ApiHash { get; }
    public string PhoneNumber { get; }
    public string SessionPath { get; }
    public string ChannelName { get; }

    public long LightLimit { get; }

    public long HeavyLimit { get; }

    public string StorageChannelId { get; }

    private readonly Dictionary<string, string> _telethonConfig;

    public AppConfigService(IConfiguration config, ILogger<AppConfigService> logger)
    {
        var telethonSection = config.GetSection("Telethon");
        var storageSection = config.GetSection("StorageSettings");

        ApiId = telethonSection["api_id"] ?? throw new InvalidOperationException("Missing Telethon:api_id");
        ApiHash = telethonSection["api_hash"] ?? throw new InvalidOperationException("Missing Telethon:api_hash");
        PhoneNumber = telethonSection["phone"] ?? throw new InvalidOperationException("Missing Telethon:phone_number");
        SessionPath = telethonSection["session_pathname"] ?? throw new InvalidOperationException("Missing Telethon:session_pathname");
        ChannelName = telethonSection["channelName"] ??
                      throw new InvalidOperationException("Missing Telethon:channelName");

        LightLimit = long.Parse(storageSection["LightLimit"] ??
                                throw new InvalidOperationException("Missing StorageSettings:LightLimit"));
        HeavyLimit = long.Parse(storageSection["HeavyLimit"] ??
                                throw new InvalidOperationException("Missing StorageSettings:HeavyLimit"));
        StorageChannelId = storageSection["StorageChannelId"] ??
                           throw new InvalidOperationException("Missing StorageSettings:StorageChannelId");
        
        logger.LogInformation($"Telethon:ApiId: {ApiId}");
        logger.LogInformation($"Telethon:ApiHash: {ApiHash}");
        logger.LogInformation($"Telethon:PhoneNumber: {PhoneNumber}");
        logger.LogInformation($"Telethon:SessionPath: {SessionPath}");
        logger.LogInformation($"Telethon:ChannelName: {ChannelName}");
        logger.LogInformation($"Telethon:LightLimit: {LightLimit}");
        logger.LogInformation($"Telethon:HeavyLimit: {HeavyLimit}");
        logger.LogInformation($"Telethon:StorageChannelId: {StorageChannelId}");

        _telethonConfig = new Dictionary<string, string>
        {
            { "api_id", ApiId },
            { "api_hash", ApiHash },
            { "phone_number", PhoneNumber },
            { "session_pathname", SessionPath }
        };

        logger.LogInformation("[AppConfigService] Configuration loaded successfully");
    }

    public Dictionary<string, string> GetTelethonConfig() => _telethonConfig;
}