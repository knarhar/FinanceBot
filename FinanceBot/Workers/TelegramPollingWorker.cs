using FinanceBot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceBot.Workers;

public class TelegramPollingWorker : BackgroundService
{
    private readonly TelegramBotClient _bot;
    private readonly ICommandFactory _commandFactory;

    public TelegramPollingWorker(TelegramBotClient bot, ICommandFactory commandFactory)
    {
        _bot = bot;
        _commandFactory = commandFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message?.Text is not string text) return;

        var command = _commandFactory.Resolve(text.Trim());
        await command.ExecuteAsync(bot, update.Message.Chat.Id, text.Trim(), ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Telegram error: {ex.Message}");
        return Task.CompletedTask;
    }
}