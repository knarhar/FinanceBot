using Telegram.Bot;

namespace FinanceBot.Commands;

public interface ICommand
{
    bool CanHandle(string text);
    Task ExecuteAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken ct);
}