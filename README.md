# FinanceBot

A Telegram bot that tracks your spending. Send it `4.50 coffee with milk` and it
saves the entry; ask it `/today` or `/month` and it replies with a recap.

Built with ASP.NET Core (net9.0), Telegram.Bot and EF Core on PostgreSQL.

## Setup

1. Clone the repo.
2. Create a `.env` file in `FinanceBot/` (see `.env.sample`):

```
Telegram__BotToken=your-bot-token-here
Telegram__Currency=AMD
Telegram__DigestHourUtc=18
Telegram__UpdateMode=Polling
PublicBaseUrl=https://your-public-url
DB__ConnectionString=Host=localhost;Database=financebot;Username=postgres;Password=postgres
```

3. Apply the migrations:

```
dotnet ef database update --project FinanceBot
```

4. Run:

```
dotnet run --project FinanceBot
```

5. Message the bot on Telegram and send `/start`.

## Usage

| Input | What it does |
| --- | --- |
| `/start` | Registers the chat and issues a report token |
| `<amount> <category> [note]` | Saves a spending, e.g. `4.50 coffee with milk` |
| `/today` | Today's total, entry count and a list of entries |
| `/month` | Month total, last 7 vs previous 7 days, typical day, top 3 categories, report link |

Amounts accept `.` or `,` as the decimal separator and must be positive.
Categories are letters only and are stored lowercased. Commands work with a bot
suffix too (`/today@MyBot`).

## How it works

- `Program.cs` loads `.env`, wires up the bot client, the `AppDbContext`, the
  message templates and every command handler as a keyed service.
- Updates arrive either by long polling (`TelegramPollingWorker`) or by webhook
  (`TelegramWebhookService` + `WebhookController`), selected with
  `Telegram__UpdateMode` (`Polling` or `Webhook`).
- `BotUpdateHandler` maps the incoming text to a command key via
  `CommandKeys.FromText` and resolves the matching `ICommandHandler`. Text that
  doesn't start with `/` is treated as a spending entry (and gets a format hint
  back if it can't be parsed); an unrecognised `/command` falls through to
  `UnknownCommandHandler`.
- `SpendingParser` turns `<amount> <category> [note]` into a `ParsedSpending`.
- `RecapService` computes the `/month` figures in a single query and returns them
  as placeholder values.
- All user-facing text lives in `Resources/messageTemplates.json`, with
  `{placeholder}` substitution via `MessageTemplates.Format`.

## Data model

- `Chat` — one row per Telegram chat: `TelegramChatId` (unique), `ReportToken`
  (unique), `StartedAt`.
- `Spending` — `Amount` (18,2), `Category`, optional `Note`, `SpentAt`, FK to
  `Chat` with cascade delete.

Timestamps are stored and compared in UTC.

## Webhook mode

Set `Telegram__UpdateMode=Webhook`, point `PublicBaseUrl` at a public HTTPS URL
(ngrok works for local development) and set `Telegram__WebhookSecret`. The bot
registers `{PublicBaseUrl}/bot/{secret}` on startup and removes it on shutdown.
Requests with the wrong secret get a 404.

## Not implemented yet

- `/history` command
- Daily digest worker (the `dailyDigestMessage` template and
  `Telegram__DigestHourUtc` are in place, the worker is not)
- Report page at `/report/{token}` (the `/month` link points at it already)

**Note:** `.env` holds real secrets — keep it out of git.
