public class TelegramOptions
{
    public const string SectionName = "Telegram";
    public string BotToken { get; set; } = "";
    public string Currency { get; set; } = "AMD";
    public int DigestHourUtc { get; set; } = 18;
    public string UpdateMode { get; set; } = "Polling";
    public string WebhookSecret { get; set; } = "";
}