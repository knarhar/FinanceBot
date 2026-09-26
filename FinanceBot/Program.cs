using System.Text.Json;
using DotNetEnv.Configuration;
using FinanceBot.Commands;
using FinanceBot.Workers;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotNetEnv(".env");

var botToken = builder.Configuration["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(botToken))
    throw new Exception("Telegram:BotToken is missing — check your .env file");

// ------------ Load message templates ------------
var messagesJson = File.ReadAllText("Resources/messageTemplates.json");
var messages = JsonSerializer.Deserialize<MessageTemplates>(messagesJson,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

builder.Services.AddSingleton(messages);
builder.Services.AddSingleton(new TelegramBotClient(botToken));
builder.Services.AddHostedService<TelegramPollingWorker>();


// ------------- Register Commands ---------------
builder.Services.AddScoped<ICommand, StartCommand>();
builder.Services.AddScoped<ICommandFactory, CommandFactory>();


var app = builder.Build();

app.Run();