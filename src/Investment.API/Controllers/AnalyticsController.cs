using Investment.Application.DTOs;
using Investment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("holdings")]
    public async Task<ActionResult<IEnumerable<HoldingDto>>> GetHoldings()
    {
        var holdings = await _analyticsService.GetHoldingsAsync();
        return Ok(holdings);
    }

    [HttpGet("performance")]
    public async Task<ActionResult<PerformanceDto>> GetPerformance([FromQuery] string period = "ALL", [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var performance = await _analyticsService.GetPerformanceAsync(period, fromDate, toDate);
        return Ok(performance);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<PortfolioAnalyticsSummaryDto>> GetSummary()
    {
        var summary = await _analyticsService.GetSummaryAsync();
        return Ok(summary);
    }
}
