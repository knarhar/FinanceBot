using FinanceBot.Data;
using FinanceBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Telegram.Bot;

namespace FinanceBot.Commands;

public class StartCommand : ICommand
{
    private readonly AppDbContext _db;
    private readonly MessageTemplates _messages;

    public StartCommand(AppDbContext db, MessageTemplates messages)
    {
        _db = db;
        _messages = messages;
    }

    public bool CanHandle(string text) => text.StartsWith("/start");

    public async Task ExecuteAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken ct)
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.TelegramChatId == chatId, ct);

        if (chat == null)
        {
            chat = new Chat
            {
                TelegramChatId = chatId,
                ReportToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                StartedAt = DateTime.UtcNow
            };

            _db.Chats.Add(chat);
            await _db.SaveChangesAsync(ct);
        }

        await bot.SendMessage(chatId, _messages.StartMessage, cancellationToken: ct);
    }
}