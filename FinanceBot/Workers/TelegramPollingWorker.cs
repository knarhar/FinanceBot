using FinanceBot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceBot.Workers;

public class TelegramPollingWorker : BackgroundService
{
    private readonly TelegramBotClient _bot;
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramPollingWorker(TelegramBotClient bot, IServiceScopeFactory scopeFactory)
    {
        _bot = bot;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message?.Text is not string text) return;

        using var scope = _scopeFactory.CreateScope();
        var commandFactory = scope.ServiceProvider.GetRequiredService<ICommandFactory>(); // resolved here, not in constructor

        var command = commandFactory.Resolve(text.Trim());
        await command.ExecuteAsync(bot, update.Message.Chat.Id, text.Trim(), ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Telegram error: {ex.Message}");
        return Task.CompletedTask;
    }
}