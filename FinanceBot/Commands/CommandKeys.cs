namespace FinanceBot.Commands;

public static class CommandKeys
{
    public const string Start = "/start";
    public const string Today = "/today";
    public const string Month = "/month";
    public const string Spending = "spending";
    public const string Unknown = "unknown";

    // /today@BotName extra → "/today"
    // /calc @BotName extra → "/today"
    // 4.50 coffee [note] → spending
    public static string FromText(string text)
    {
        if (!text.StartsWith('/'))
        {
            return Spending;
        }

        int end = text.Length;

        for (int i = 1; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '@' || char.IsWhiteSpace(c))
            {
                end = i;
                break;
            }
        }

        return text.Substring(0, end).ToLowerInvariant();
    }
}