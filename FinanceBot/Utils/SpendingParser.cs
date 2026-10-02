using System.Globalization;
using System.Text.RegularExpressions;

namespace FinanceBot.Utils;

public class ParsedSpending
{
    public decimal Amount { get; set; }
    public string Category { get; set; } = "";
    public string? Note { get; set; }
}

// Why a line could not be parsed.
public enum SpendingParseError
{
    None,
    InvalidAmount,
    NonPositiveAmount,
    MissingCategory,
    InvalidCategory
}

public static class SpendingParser
{
    public static bool TryParse(string text, out ParsedSpending? result, out SpendingParseError error)
    {
        result = null;
        error = SpendingParseError.None;

        var parts = text.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

        var amountToken = parts.Length > 0 ? parts[0].Replace(',', '.') : "";
        if (!decimal.TryParse(amountToken, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            error = SpendingParseError.InvalidAmount;
            return false;
        }
        if (amount <= 0)
        {
            error = SpendingParseError.NonPositiveAmount;
            return false;
        }

        if (parts.Length < 2)
        {
            error = SpendingParseError.MissingCategory;
            return false;
        }

        var category = parts[1];
        if (!Regex.IsMatch(category, "^[a-zA-Z]+$"))
        {
            error = SpendingParseError.InvalidCategory;
            return false;
        }

        result = new ParsedSpending
        {
            Amount = amount,
            Category = category.ToLowerInvariant(),
            Note = parts.Length > 2 ? parts[2] : null
        };
        return true;
    }
}
