using System.Globalization;
using FinanceBot.Data;
using FinanceBot.Utils;
using FinanceBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace FinanceBot.Commands;

public class SpendingCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;
    private readonly MessageTemplates _messages;
    private readonly string _currency;

    public SpendingCommandHandler(ITelegramBotClient bot, AppDbContext db, MessageTemplates messages, IConfiguration configuration)
    {
        _bot = bot;
        _db = db;
        _messages = messages;
        _currency = configuration["Telegram:Currency"] ?? "";
    }

    public async Task HandleAsync(long chatId, string text, CancellationToken ct)
    {
        if (!SpendingParser.TryParse(text, out var parsed, out var error) || parsed == null)
        {
            var reason = error switch
            {
                SpendingParseError.InvalidAmount => _messages.InvalidAmountMessage,
                SpendingParseError.NonPositiveAmount => _messages.NonPositiveAmountMessage,
                SpendingParseError.MissingCategory => _messages.MissingCategoryMessage,
                SpendingParseError.InvalidCategory => _messages.InvalidCategoryMessage,
                _ => _messages.InvalidFormatMessage
            };

            await _bot.SendMessage(chatId, reason, cancellationToken: ct);
            return;
        }

        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.TelegramChatId == chatId, ct);
        if (chat == null)
        {
            await _bot.SendMessage(chatId, _messages.NotStartedMessage, cancellationToken: ct);
            return;
        }

        var spending = new Spending
        {
            ChatId = chat.Id,
            Amount = parsed.Amount,
            Category = parsed.Category,
            Note = parsed.Note,
            SpentAt = DateTime.UtcNow
        };

        _db.Spendings.Add(spending);
        await _db.SaveChangesAsync(ct);

        var reply = MessageTemplates.Format(_messages.AddSpendingMessage, new()
        {
            ["amount"] = parsed.Amount.ToString(CultureInfo.InvariantCulture),
            ["category"] = parsed.Category,
            ["currency"] = _currency
        });

        await _bot.SendMessage(chatId, reply, cancellationToken: ct);
    }
}