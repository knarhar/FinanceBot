public class MessageTemplates
{
    public string StartMessage { get; set; } = "";
    public string AddSpendingMessage { get; set; } = "";
    public string InvalidFormatMessage { get; set; } = "";
    public string NotStartedMessage { get; set; } = "";
    public string TodayEmptyMessage { get; set; } = "";
    public string TodaySummaryMessage { get; set; } = "";
    public string MonthEmptyMessage { get; set; } = "";
    public string MonthSummaryMessage { get; set; } = "";
    public string DailyDigestMessage { get; set; } = "";
    public string UnknownCommandMessage { get; set; } = "";
    
    public static string Format(string template, Dictionary<string, string> values)
    {
        foreach (var kv in values)
            template = template.Replace("{" + kv.Key + "}", kv.Value);
        return template;
    }
}