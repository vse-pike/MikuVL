using Microsoft.AspNetCore.Builder;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramBot.Downloader;
using TelegramBot.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddEnvironmentVariables();

var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT__APITOKEN");

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
builder.Services.AddScoped<IDownloader, Downloader>();
builder.Services.AddHostedService<CleanupWorker>();
builder.Services.AddHttpClient<Downloader>();
builder.Services.AddControllers();

builder.Host.UseSerilog();

var host = builder.Build();
host.MapControllers();
host.Run();