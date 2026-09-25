using System.Reflection.Metadata;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceBot.Workers;

public class TelegramPollingWorker : BackgroundService
{
    private TelegramBotClient _bot;

    public TelegramPollingWorker(TelegramBotClient bot)
    {
        _bot = bot;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            cancellationToken: stoppingToken
            );
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message?.Text is not string text) return;

        long chatId = update.Message.Chat.Id;

        await bot.SendMessage(chatId, $"You said: {text}", cancellationToken: ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Telegram error: {ex.Message}");
        return Task.CompletedTask;
    }
}