using TL;
using WTelegram;

namespace TelegramBot.Clients;

public interface ITelethonClient
{
    Task<int> UploadFileToChannelAsync(string fullPath, string filename);
}

public class TelethonClient : ITelethonClient, IAsyncDisposable
{
    private readonly Client _client;
    private readonly string _channelName;
    private User? _self;
    private readonly ILogger<TelethonClient> _logger;

    public TelethonClient(ILogger<TelethonClient> logger, AppConfigService appConfigService)
    {
        _logger = logger;
        _channelName = appConfigService.ChannelName;

        var telethonConfig = appConfigService.GetTelethonConfig();

        _client = new Client(what => telethonConfig.TryGetValue(what, out var value) ? value : null);
    }

    public async Task<int> UploadFileToChannelAsync(string fullPath, string filename)
    {
        _self ??= await _client.LoginUserIfNeeded();
        
        var chats = await _client.Messages_GetAllChats();
        
        foreach (var chat in chats.chats.Values.OfType<Channel>())
        {
            _logger.LogInformation("[Telethon] Channel found: {0} | id: {1} | access_hash: {2}", chat.title, chat.id, chat.access_hash);
        }
        
        var channel = chats.chats.Values
            .OfType<Channel>()
            .FirstOrDefault(c => c.title.Equals(_channelName, StringComparison.OrdinalIgnoreCase));

        if (channel == null)
            throw new BusinessException("[Telethon] Channel not found");

        var peer = new InputPeerChannel(channel.id, channel.access_hash);
        _logger.LogInformation("[Telethon] Uploading {Path} to {Title}", fullPath, channel.title);

        var media = new InputMediaUploadedDocument
        {
            file = await _client.UploadFileAsync(fullPath),
            mime_type = "application/octet-stream",
            attributes = [new DocumentAttributeFilename { file_name = filename }]
        };

        var result = await _client.Messages_SendMedia(peer, media, message: "", random_id: Helpers.RandomLong());
        var messageId = (result as Updates)?.updates.OfType<UpdateNewMessage>().FirstOrDefault()?.message.ID ?? 0;

        if (messageId == 0)
            throw new BusinessException("[Telethon] Failed to get message ID");

        _logger.LogInformation("[Telethon] Uploaded. ID: {MessageId}", messageId);
        return messageId;
    }

    public async ValueTask DisposeAsync() => await _client.DisposeAsync();
}