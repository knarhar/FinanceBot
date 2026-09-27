using FinanceBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceBot.Services;

public class TelegramPollingWorker : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly BotUpdateHandler _updateHandler;

    public TelegramPollingWorker(ITelegramBotClient bot, BotUpdateHandler updateHandler)
    {
        _bot = bot;
        _updateHandler = updateHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        await _updateHandler.HandleAsync(update, ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Telegram error: {ex.Message}");
        return Task.CompletedTask;
    }
}