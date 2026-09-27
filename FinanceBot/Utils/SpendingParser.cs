using System.Globalization;
using System.Text.RegularExpressions;

namespace FinanceBot.Utils;

public class ParsedSpending
{
    public decimal Amount { get; set; }
    public string Category { get; set; } = "";
    public string? Note { get; set; }
}

public static class SpendingParser
{
    public static bool TryParse(string text, out ParsedSpending? result)
    {
        result = null;

        var parts = text.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        var amountToken = parts[0].Replace(',', '.');
        if (!decimal.TryParse(amountToken, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            return false;
        if (amount <= 0) return false;

        var category = parts[1];
        if (!Regex.IsMatch(category, "^[a-zA-Z]+$")) return false;

        result = new ParsedSpending
        {
            Amount = amount,
            Category = category.ToLowerInvariant(),
            Note = parts.Length > 2 ? parts[2] : null
        };
        return true;
    }
}