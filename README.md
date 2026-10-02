# FinanceBot

A Telegram bot that tracks your spending. Send it `4.50 coffee with milk` and it
saves the entry; ask it `/today` or `/month` and it replies with a recap. Every
day it also sends an automatic digest, with a link to a full report page.

Built with ASP.NET Core (net9.0), Telegram.Bot and EF Core on PostgreSQL.

## Setup

1. Clone the repo.
2. Create a `.env` file in `FinanceBot/` (see `.env.sample`):

```
Telegram__BotToken=your-bot-token-here
Telegram__Currency=AMD
Telegram__DigestHourUtc=18
Telegram__UpdateMode=Polling
Telegram__WebhookSecret=some-random-string
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
- `RecapService` computes the recap/report figures in one query per chat and
  exposes two methods: `BuildPlaceholdersAsync` (used by `/month` and the daily
  digest — returns `null` for a chat with no spendings this month, so nothing
  is sent) and `GetReportAsync` (used by the report page — always returns data,
  even if every total is zero, since a direct link visit shouldn't 404).
- `DailyDigestWorker` runs once per UTC day at `Telegram__DigestHourUtc`, sends
  a recap to every chat with at least one spending this month, and schedules
  itself off `DateTime.UtcNow` rather than a fixed delay, so a restart doesn't
  shift the next run.
- `ReportController` serves `GET /report/{token}`, looks the chat up by token
  (404 if not found) and renders `Views/Report/Get.cshtml` inside a shared
  layout (`Views/Shared/_Layout.cshtml`) with month/week stats, the full
  category breakdown, a 14-day table and the last 20 spendings.
- All user-facing text lives in `Resources/messageTemplates.json`, with
  `{placeholder}` substitution via `MessageTemplates.Format`.

## Data model

- `Chat` — one row per Telegram chat: `TelegramChatId` (unique), `ReportToken`
  (unique), `StartedAt`.
- `Spending` — `Amount` (12,2), `Category`, optional `Note`, `SpentAt`, FK to
  `Chat` (one chat, many spendings).

Timestamps are stored and compared in UTC.

## Webhook mode

Set `Telegram__UpdateMode=Webhook`, point `PublicBaseUrl` at a public HTTPS URL
(ngrok works for local development) and set `Telegram__WebhookSecret`. The bot
registers `{PublicBaseUrl}/bot/{secret}` on startup and removes it on shutdown.
Requests with the wrong secret get a 404. Note: `Telegram.Bot` 22.4.3 needs
explicit JSON serializer options (`JsonBotAPI.Options`) to deserialize
webhook payloads correctly — this is handled in `WebhookController` and becomes
unnecessary if the package is upgraded to 22.5+.

## Report page

`GET /report/{token}` — the long-form version of the daily digest for one chat.
Shows month total/count, last 7 vs previous 7 days, typical day, the full
category breakdown (not just top 3), a day-by-day table for the last 14 days,
and the last 20 individual spendings. An unknown token returns 404; the token
is never exposed as the Telegram chat ID.

**Note:** `.env` holds real secrets — keep it out of git.
