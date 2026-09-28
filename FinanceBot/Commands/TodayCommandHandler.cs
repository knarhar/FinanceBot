using System.Globalization;
using System.Text;
using FinanceBot.Data;
using FinanceBot.Utils;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace FinanceBot.Commands;

// Handles the /today command.
// It only READS data. It never writes anything to the database.
public class TodayCommandHandler : ICommandHandler
{
    // Used to send the reply message to Telegram.
    private readonly ITelegramBotClient _bot;

    private readonly AppDbContext _db;

    private readonly MessageTemplates _messages;

    private readonly string _currency;

    public TodayCommandHandler(
        ITelegramBotClient bot,
        AppDbContext db,
        MessageTemplates messages,
        IConfiguration configuration)
    {
        _bot = bot;
        _db = db;
        _messages = messages;

        _currency = configuration["Telegram:Currency"] ?? "";
    }

    public async Task HandleAsync(long chatId, string text, CancellationToken ct)
    {
        // Find this Telegram chat in our Chats table.
        var chat = await _db.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TelegramChatId == chatId, ct);

        // If there is no row, this user never sent /start.
        if (chat == null)
        {
            await _bot.SendMessage(chatId, _messages.NotStartedMessage, cancellationToken: ct);
            return;
        }

        // now        = the current moment in UTC.
        // now.Date   = today at 00:00 UTC (the start of today).
        var now = DateTime.UtcNow;
        var startOfToday = now.Date;

        var spendings = await _db.Spendings
            .AsNoTracking()
            .Where(s => s.ChatId == chat.Id
                     && s.SpentAt >= startOfToday
                     && s.SpentAt <= now)
            .OrderBy(s => s.SpentAt)
            .ToListAsync(ct);

        if (spendings.Count == 0)
        {
            await _bot.SendMessage(chatId, _messages.TodayEmptyMessage, cancellationToken: ct);
            return;
        }

        decimal total = spendings.Sum(s => s.Amount);
        int count = spendings.Count;

        // StringBuilder is used because we add many pieces of text in a loop.
        var entryList = new StringBuilder();

        foreach (var s in spendings)
        {
            entryList.Append("- ")
                     .Append(s.Amount.ToString(CultureInfo.InvariantCulture))
                     .Append(' ')
                     .Append(_currency)
                     .Append("  ")
                     .Append(s.Category);
            
            // Add " (note)" only when the note really has text.
            if (!string.IsNullOrWhiteSpace(s.Note))
            {
                entryList.Append(" (").Append(s.Note).Append(')');
            }

            // End the line, so the next spending starts on a new line.
            entryList.AppendLine();
        }

        // TrimEnd() removes the extra line break after the last entry.
        var reply = MessageTemplates.Format(_messages.TodaySummaryMessage, new()
        {
            ["total"] = total.ToString(CultureInfo.InvariantCulture),
            ["currency"] = _currency,
            ["count"] = count.ToString(CultureInfo.InvariantCulture),
            ["entryList"] = entryList.ToString().TrimEnd()
        });

        await _bot.SendMessage(chatId, reply, cancellationToken: ct);
    }
}