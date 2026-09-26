using DotNetEnv.Configuration;
using FinanceBot.Data;
using FinanceBot.Workers;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotNetEnv(".env");

var botToken = builder.Configuration["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(botToken))
    throw new Exception("Telegram:BotToken is missing — check your .env file");

builder.Services.AddSingleton(new TelegramBotClient(botToken));
builder.Services.AddHostedService<TelegramPollingWorker>();

// ---------- Database Configuration -----------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DB:ConnectionString")));

var app = builder.Build();

app.Run();