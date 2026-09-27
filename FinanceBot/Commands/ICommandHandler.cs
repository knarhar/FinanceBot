namespace FinanceBot.Commands;

public interface ICommandHandler
{
    Task HandleAsync(long chatId, string text, CancellationToken cancellationToken);
}