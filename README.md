# FinanceBot

Minimal Telegram echo bot built with ASP.NET Core and Telegram.Bot. Replies "You said: <message>" to any text message. No database or finance commands yet.

## Setup

1. Clone the repo.
2. Create a `.env` file in the project root:

Telegram__BotToken=your-bot-token-here

3. Run:

dotnet run

4. Message the bot on Telegram.

## How it works

- `Program.cs` loads `.env`, reads `Telegram:BotToken`, registers `TelegramBotClient` as a singleton, and starts `TelegramPollingWorker` as a hosted service.
- `TelegramPollingWorker` long-polls Telegram and echoes back any text it receives.

## Not implemented yet

- Database / message persistence
- `/start`, `/today`, `/month`, `/history` commands
- Amount/category parsing
- Daily digest worker
- Report page

**Note:** `.env` holds a real secret — keep it out of git.
