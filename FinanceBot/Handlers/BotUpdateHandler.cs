using FinanceBot.Commands;
using Telegram.Bot.Types;

namespace FinanceBot.Handlers;

public class BotUpdateHandler
{
    private readonly IServiceScopeFactory _scopes;

    public BotUpdateHandler(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task HandleAsync(
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message == null || update.Message.Text == null)
        {
            return;
        }

        string text = update.Message.Text.Trim();
        long chatId = update.Message.Chat.Id;

        using IServiceScope scope = _scopes.CreateScope();

        ICommandHandler handler =
            scope.ServiceProvider.GetKeyedService<ICommandHandler>(
                CommandKeys.FromText(text))
            ??
            scope.ServiceProvider.GetRequiredKeyedService<ICommandHandler>(
                CommandKeys.Unknown);

        await handler.HandleAsync(chatId, text, cancellationToken);
    }
}