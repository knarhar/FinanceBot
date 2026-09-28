using System.Globalization;
using System.Text;
using FinanceBot.Data;
using FinanceBot.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceBot.Services;

// Calculates the six recap items for ONE chat.
// summary or recapitulation

public class RecapService
{
    private readonly AppDbContext _db;
    private readonly string _currency;
    private readonly string _publicBaseUrl;

    public RecapService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _currency = configuration["Telegram:Currency"] ?? "";

        _publicBaseUrl = (configuration["PublicBaseUrl"] ?? "").TrimEnd('/');
    }

    // Returns the values for the message placeholders.
    public async Task<Dictionary<string, string>?> BuildPlaceholdersAsync(
        Chat chat, DateTime nowUtc, CancellationToken ct)
    {
        var todayStart = nowUtc.Date;
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last7Start = todayStart.AddDays(-7);
        var prev7Start = todayStart.AddDays(-14);

        DateTime loadFrom;
        if (monthStart < prev7Start)
        {
            loadFrom = monthStart;
        }
        else
        {
            loadFrom = prev7Start;
        }

        //One query. Only this chat's rows, from loadFrom until now.
        var spendings = await _db.Spendings
            .AsNoTracking()
            .Where(s => s.ChatId == chat.Id
                     && s.SpentAt >= loadFrom
                     && s.SpentAt <= nowUtc)
            .ToListAsync(ct);

        // The list may contain rows from before monthStart
        var monthSpendings = spendings.Where(s => s.SpentAt >= monthStart).ToList();

        if (monthSpendings.Count == 0)
        {
            return null;
        }

        decimal monthTotal = monthSpendings.Sum(s => s.Amount);
        int monthCount = monthSpendings.Count;

        //two rolling weeks.
        // >= start and < end: today is NOT included.
        decimal last7Total = spendings
            .Where(s => s.SpentAt >= last7Start && s.SpentAt < todayStart)
            .Sum(s => s.Amount);

        decimal prev7Total = spendings
            .Where(s => s.SpentAt >= prev7Start && s.SpentAt < last7Start)
            .Sum(s => s.Amount);

        // typical day.
        // Round to 2 decimals, because a division can give many digits.
        decimal typicalDay = Math.Round(monthTotal / nowUtc.Day, 2);

        // top 3 categories this month.
        // . new { Category = ..., Total = ... } creates an anonymous type:
        var topCategories = monthSpendings
            .GroupBy(s => s.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Category)
            .Take(3)
            .ToList();

        // Turn the list into text, one line per category: "1. food: 50 AMD"
        var topText = new StringBuilder();
        for (int i = 0; i < topCategories.Count; i++)
        {
            topText.Append(i + 1)
                   .Append(". ")
                   .Append(topCategories[i].Category)
                   .Append(": ")
                   .Append(topCategories[i].Total.ToString(CultureInfo.InvariantCulture))
                   .Append(' ')
                   .Append(_currency)
                   .AppendLine();
        }

        //  the link.
        var reportUrl = $"{_publicBaseUrl}/report/{chat.ReportToken}";

        //Return every value under the placeholder names of the templates.
        return new Dictionary<string, string>
        {
            ["monthTotal"] = monthTotal.ToString(CultureInfo.InvariantCulture),
            ["monthCount"] = monthCount.ToString(CultureInfo.InvariantCulture),
            ["currency"] = _currency,
            ["last7"] = last7Total.ToString(CultureInfo.InvariantCulture),
            ["prev7"] = prev7Total.ToString(CultureInfo.InvariantCulture),
            ["typicalDay"] = typicalDay.ToString(CultureInfo.InvariantCulture),
            ["topCategories"] = topText.ToString().TrimEnd(),
            ["reportUrl"] = reportUrl
        };
    }
}