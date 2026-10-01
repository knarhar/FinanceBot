using Telegram.Bot;
using FinanceBot.Data;
using FinanceBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Telegram.Bot;


namespace FinanceBot.Commands;

public class StartCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly AppDbContext _db;
    private readonly MessageTemplates _messages;

    public StartCommandHandler(ITelegramBotClient bot, AppDbContext db, MessageTemplates messages)
    {
        _bot = bot;
        _db = db;
        _messages = messages;
    }

    public async Task HandleAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        // Look for an existing Chat row for this Telegram chat.
        var chat = await _db.Chats.FirstOrDefaultAsync(
            c => c.TelegramChatId == chatId, cancellationToken);

        // If this chat has never sent /start before, register it now.
        if (chat == null)
        {
            chat = new Chat
            {
                TelegramChatId = chatId,
                ReportToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                StartedAt = DateTime.UtcNow
            };

            _db.Chats.Add(chat);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Whether this chat is brand new or already registered,
        // always reply with the start message.
        var reply = MessageTemplates.Format(_messages.StartMessage, new()
        {
            ["exampleAmount"] = "4.50",
            ["exampleCategory"] = "coffee",
            ["exampleNote"] = "with milk"
        });

        await _bot.SendMessage(chatId, reply, cancellationToken: cancellationToken);
    }
}