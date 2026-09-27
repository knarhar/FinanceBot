using Telegram.Bot;

namespace FinanceBot.Commands;

public class UnknownCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly MessageTemplates _messages;

    // Only two dependencies needed: something to reply with, and
    // something to know what to say. No database access 
    public UnknownCommandHandler(ITelegramBotClient bot, MessageTemplates messages)
    {
        _bot = bot;
        _messages = messages;
    }

    public async Task HandleAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        
        await _bot.SendMessage(chatId, _messages.UnknownCommandMessage, cancellationToken: cancellationToken);
    }
}