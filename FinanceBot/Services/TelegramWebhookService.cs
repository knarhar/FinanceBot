using Telegram.Bot;

namespace FinanceBot.Services;

public class TelegramWebhookService : IHostedService
{
    private readonly ITelegramBotClient _bot;
    private readonly IConfiguration _configuration;

    public TelegramWebhookService(ITelegramBotClient bot, IConfiguration configuration)
    {
        _bot = bot;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var publicUrl = _configuration["PublicBaseUrl"];
        var secret = _configuration["Telegram:WebhookSecret"];

        await _bot.SetWebhook(
            url: $"{publicUrl}/bot/{secret}",
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bot.DeleteWebhook(cancellationToken: cancellationToken);
    }
}