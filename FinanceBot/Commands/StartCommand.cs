using Telegram.Bot;

namespace FinanceBot.Commands;

public class StartCommand : ICommand
{
    private readonly MessageTemplates _messages;

    public StartCommand(MessageTemplates messages) => _messages = messages;

    public bool CanHandle(string text) => text.StartsWith("/start");

    public async Task ExecuteAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken ct)
    {
        await bot.SendMessage(chatId, _messages.StartMessage, cancellationToken: ct);
    }
}