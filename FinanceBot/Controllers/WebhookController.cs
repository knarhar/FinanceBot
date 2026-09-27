using System.Text.Json;
using FinanceBot.Handlers;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceBot.Controllers;

[ApiController]
[Route("bot")]
public class WebhookController : ControllerBase
{
    private readonly BotUpdateHandler _updateHandler;
    private readonly string _webhookSecret;

    public WebhookController(BotUpdateHandler updateHandler, IConfiguration configuration)
    {
        _updateHandler = updateHandler;
        _webhookSecret = configuration["Telegram:WebhookSecret"] ?? "";
    }

    [HttpPost("{secret}")]
    public async Task<IActionResult> Post(string secret, CancellationToken ct)
    {
        if (secret != _webhookSecret) return NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var update = JsonSerializer.Deserialize<Update>(body, JsonBotAPI.Options);
        if (update != null)
        {
            await _updateHandler.HandleAsync(update, ct);
        }

        return Ok();
    }
}