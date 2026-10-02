using System.Text.Json;
using DotNetEnv.Configuration;
using FinanceBot.Data;
using FinanceBot.Commands;
using FinanceBot.Handlers;
using FinanceBot.Services;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotNetEnv(".env");

var botToken = builder.Configuration["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(botToken))
    throw new Exception("Telegram:BotToken is missing — check your .env file");

var updateMode = builder.Configuration["Telegram:UpdateMode"] ?? "Polling";

// ------------ Load message templates ------------
var messagesJson = File.ReadAllText("Resources/messageTemplates.json");
var messages = JsonSerializer.Deserialize<MessageTemplates>(messagesJson,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

builder.Services.AddSingleton(messages);
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

// -------------- Controllers ---------------
builder.Services.AddControllersWithViews();

// ----------- Polling vs Webhook -----------
if (updateMode == "Webhook")
{
    builder.Services.AddHostedService<TelegramWebhookService>();
}
else
{
    builder.Services.AddHostedService<TelegramPollingWorker>();
}

// ----------- Database Configuration ------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration["DB:ConnectionString"]));

// ------------- Register Commands ---------------
builder.Services.AddKeyedScoped<ICommandHandler, StartCommandHandler>(CommandKeys.Start);
builder.Services.AddKeyedScoped<ICommandHandler, SpendingCommandHandler>(CommandKeys.Spending);
builder.Services.AddKeyedScoped<ICommandHandler, UnknownCommandHandler>(CommandKeys.Unknown);

builder.Services.AddKeyedScoped<ICommandHandler, TodayCommandHandler>(CommandKeys.Today);
builder.Services.AddKeyedScoped<ICommandHandler, MonthCommandHandler>(CommandKeys.Month);
builder.Services.AddScoped<RecapService>();
builder.Services.AddHostedService<DailyDigestWorker>();
    

builder.Services.AddSingleton<BotUpdateHandler>();

var app = builder.Build();

app.MapControllers();

app.Run();