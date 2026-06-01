using Investment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IExcelExportService _exportService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ITransactionService _transactionService;

    public ExportController(IExcelExportService exportService, IAnalyticsService analyticsService, ITransactionService transactionService)
    {
        _exportService = exportService;
        _analyticsService = analyticsService;
        _transactionService = transactionService;
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> ExportHoldings()
    {
        var holdings = await _analyticsService.GetHoldingsAsync();
        var fileContent = _exportService.ExportHoldings(holdings);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Holdings_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> ExportTransactions([FromQuery] int? assetId, [FromQuery] string? type, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var transactions = await _transactionService.GetFilteredAsync(assetId, type, fromDate, toDate);
        var fileContent = _exportService.ExportTransactions(transactions);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Transactions_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
