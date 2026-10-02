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

    
    // record = a class built for "just carries data", with built-in equality
    // and no need to write a constructor by hand.
    public record CategoryTotal(string Category, decimal Total);
    public record DailyTotal(DateTime Date, decimal Total);

    public record ReportData(
        string Currency,
        decimal MonthTotal,
        int MonthCount,
        decimal Last7Total,
        decimal Prev7Total,
        decimal TypicalDay,
        List<CategoryTotal> CategoryBreakdown,
        List<DailyTotal> DailyTotals,
        List<Spending> RecentSpendings);
    
    
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
    
    // Everything the report page needs for one chat.
    // Unlike BuildPlaceholdersAsync, this never returns null —
    // the page must render even for a chat with zero spendings.
    public async Task<ReportData> GetReportAsync(Chat chat, DateTime nowUtc, CancellationToken ct)
    {
        var todayStart = nowUtc.Date;
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last7Start = todayStart.AddDays(-7);
        var prev7Start = todayStart.AddDays(-14);
        var last14Start = todayStart.AddDays(-13); // 14 days INCLUDING today

        // earliestNeeded: the furthest-back date any of our numbers requires.
        var earliestNeeded = new[] { monthStart, prev7Start, last14Start }.Min();

        // One query covering everything except the "last 20 spendings" list,
        // which can reach further back than any of these windows.
        var windowSpendings = await _db.Spendings
            .AsNoTracking()
            .Where(s => s.ChatId == chat.Id && s.SpentAt >= earliestNeeded && s.SpentAt <= nowUtc)
            .ToListAsync(ct);

        var monthSpendings = windowSpendings.Where(s => s.SpentAt >= monthStart).ToList();

        decimal monthTotal = monthSpendings.Sum(s => s.Amount);
        int monthCount = monthSpendings.Count;

        decimal last7Total = windowSpendings
            .Where(s => s.SpentAt >= last7Start && s.SpentAt < todayStart)
            .Sum(s => s.Amount);

        decimal prev7Total = windowSpendings
            .Where(s => s.SpentAt >= prev7Start && s.SpentAt < last7Start)
            .Sum(s => s.Amount);

        // nowUtc.Day = days elapsed this month, same rule as the digest.
        // Guard against a month with 0 spendings: division by Day is still fine
        // since Day is never 0, but if monthTotal is 0 the result is just 0.
        decimal typicalDay = Math.Round(monthTotal / nowUtc.Day, 2);

        // FULL category breakdown this month — no Take(3) here.
        var categoryBreakdown = monthSpendings
            .GroupBy(s => s.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(c => c.Total)
            .ThenBy(c => c.Category)
            .ToList();

        // 14 rows, one per calendar day, today included, oldest first.
        // We build all 14 dates ourselves so a day with zero spendings
        // still shows up as a row with total 0 — "one row per day" means all 14.
        var dailyTotals = new List<DailyTotal>();
        for (var day = last14Start; day <= todayStart; day = day.AddDays(1))
        {
            var dayTotal = windowSpendings
                .Where(s => s.SpentAt >= day && s.SpentAt < day.AddDays(1))
                .Sum(s => s.Amount);
            dailyTotals.Add(new DailyTotal(day, dayTotal));
        }

        // Separate query: the 20 most recent spendings can be older than
        // everything above (e.g. a quiet chat), so this is not limited
        // to earliestNeeded.
        var last20 = await _db.Spendings
            .AsNoTracking()
            .Where(s => s.ChatId == chat.Id)
            .OrderByDescending(s => s.SpentAt)
            .Take(20)
            .ToListAsync(ct);

        return new ReportData(
            Currency: _currency,
            MonthTotal: monthTotal,
            MonthCount: monthCount,
            Last7Total: last7Total,
            Prev7Total: prev7Total,
            TypicalDay: typicalDay,
            CategoryBreakdown: categoryBreakdown,
            DailyTotals: dailyTotals,
            RecentSpendings: last20);
    }
}