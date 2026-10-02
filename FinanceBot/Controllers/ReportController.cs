using FinanceBot.Data;
using FinanceBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceBot.Controllers;

[Route("report")]
public class ReportController : Controller
{
    private readonly AppDbContext _db;
    private readonly RecapService _recapService;

    public ReportController(AppDbContext db, RecapService recapService)
    {
        _db = db;
        _recapService = recapService;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token, CancellationToken ct)
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.ReportToken == token, ct);
        if (chat == null) return NotFound();

        var report = await _recapService.GetReportAsync(chat, DateTime.UtcNow, ct);

        return View(report); // Views/Report/Get.cshtml
    }
}