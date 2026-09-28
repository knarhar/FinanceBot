using FinanceBot.Data;
using FinanceBot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace FinanceBot.Commands;

// Handles the /month command. It only READS data.
public class MonthCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;
    private readonly MessageTemplates _messages;
    private readonly RecapService _recap;

    public MonthCommandHandler(
        ITelegramBotClient bot,
        AppDbContext db,
        MessageTemplates messages,
        RecapService recap)
    {
        _bot = bot;
        _db = db;
        _messages = messages;
        _recap = recap;
    }

    public async Task HandleAsync(long chatId, string text, CancellationToken ct)
    {
        // STEP 1: Find the chat. No row means the user never sent /start.
        var chat = await _db.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TelegramChatId == chatId, ct);

        if (chat == null)
        {
            await _bot.SendMessage(chatId, _messages.NotStartedMessage, cancellationToken: ct);
            return;
        }

        // STEP 2: Ask the shared service for the six items.
        var values = await _recap.BuildPlaceholdersAsync(chat, DateTime.UtcNow, ct);

        // STEP 3: null means no spendings this month -> friendly reply.
        if (values == null)
        {
            await _bot.SendMessage(chatId, _messages.MonthEmptyMessage, cancellationToken: ct);
            return;
        }

        // STEP 4: Fill the template and send.
        var reply = MessageTemplates.Format(_messages.MonthSummaryMessage, values);
        await _bot.SendMessage(chatId, reply, cancellationToken: ct);
    }
}