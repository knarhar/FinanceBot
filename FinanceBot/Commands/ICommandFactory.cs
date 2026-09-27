namespace FinanceBot.Commands;

public interface ICommandFactory
{
    ICommand Resolve(string text);
}