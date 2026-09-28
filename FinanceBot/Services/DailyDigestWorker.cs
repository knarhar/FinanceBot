using FinanceBot.Data;
using FinanceBot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace FinanceBot.Workers;

// Runs in the background for the whole life of the app.
// Once per UTC day it sends the recap to every eligible chat.
public class DailyDigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyDigestWorker> _logger;
    private readonly int _digestHourUtc;

    public DailyDigestWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyDigestWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // If it is missing, fall back to 18.
        _digestHourUtc = configuration.GetValue("Telegram:DigestHourUtc", 18);
    }

    
    // The framework calls this once at app start.
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        
        //await SendDigestAsync(ct); <-- uncomment to test

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // how long until the next digest hour?
                var now = DateTime.UtcNow;
                var nextRun = GetNextRun(now, _digestHourUtc);

                _logger.LogInformation("Next digest at {NextRun} UTC", nextRun);

                // sleep until then (without blocking a thread).
                await Task.Delay(nextRun - now, ct);

                //  the work.
                await SendDigestAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    // Absolute target time: today at the digest hour, or tomorrow's if already passed.
    private static DateTime GetNextRun(DateTime nowUtc, int hour)
    {
        var target = nowUtc.Date.AddHours(hour);

        if (target <= nowUtc)
        {
            target = target.AddDays(1);
        }

        return target;
    }

    private async Task SendDigestAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recap = scope.ServiceProvider.GetRequiredService<RecapService>();
        var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var messages = scope.ServiceProvider.GetRequiredService<MessageTemplates>();

        // Every chat that ever sent /start.
        var chats = await db.Chats.AsNoTracking().ToListAsync(ct);

        foreach (var chat in chats)
        {
            try
            {
                // The service returns null when this chat has no spendings this month.
                var values = await recap.BuildPlaceholdersAsync(chat, now, ct);

                if (values == null)
                {
                    continue; 
                }

                var text = MessageTemplates.Format(messages.DailyDigestMessage, values);

                await bot.SendMessage(chat.TelegramChatId, text, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                throw; // let shutdown propagate
            }
            catch (Exception ex)
            {
                // One failing chat (for example a user who blocked the bot)
                // must not stop the digest for everybody else.
                _logger.LogError(ex, "Digest failed for chat {ChatId}", chat.TelegramChatId);
            }
        }
    }
}