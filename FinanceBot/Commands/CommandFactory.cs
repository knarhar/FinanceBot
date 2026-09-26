namespace FinanceBot.Commands;

public class CommandFactory : ICommandFactory
{
    private readonly IEnumerable<ICommand> _commands;

    public CommandFactory(IEnumerable<ICommand> commands)
    {
        _commands = commands;
    }

    public ICommand Resolve(string text)
    {
        foreach (var command in _commands)
        {
            if (command.CanHandle(text))
                return command;
        }

        throw new InvalidOperationException("No command matched — did you register a fallback?");
    }
}