using Microsoft.AspNetCore.Builder;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot;
using TelegramBot.Clients;
using TelegramBot.Services;
using TelegramBot.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddEnvironmentVariables();

var token = builder.Configuration.GetRequiredSection("Telegram").GetValue<string>("BotToken");

if (string.IsNullOrEmpty(token))
{
    throw new Exception("Token is null or empty");
}

builder.Services.AddHostedService<PollingService>();

builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>(httpClient =>
    {
        TelegramBotClientOptions options = new(token);
        return new TelegramBotClient(options, httpClient);
    });

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddScoped<IUpdateHandler, UpdateHandler>();
builder.Services.AddScoped<ReceiverService>();
builder.Services.AddScoped<IClient, DownloaderClient>();
builder.Services.AddHostedService<CleanupWorker>();
builder.Services.AddHttpClient<DownloaderClient>();
builder.Services.AddSingleton<ITelethonClient, TelethonClient>();
builder.Services.AddScoped<IUploader, TelegramUploader>();
builder.Services.AddSingleton<AppConfigService>();
builder.Services.AddControllers();

builder.Host.UseSerilog();

var host = builder.Build();
host.MapControllers();
host.Run();