using Investment.Application.DTOs;
using Investment.Application.Services;
using Investment.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceFetchController : ControllerBase
{
    private readonly IPriceFetchService _priceFetchService;
    private readonly EodhdPriceFetcher _eodhd;

    public PriceFetchController(IPriceFetchService priceFetchService, EodhdPriceFetcher eodhd)
    {
        _priceFetchService = priceFetchService;
        _eodhd = eodhd;
    }

    [HttpPost("run")]
    public async Task<ActionResult<PriceFetchLogDto>> RunFetch()
    {
        var log = await _priceFetchService.RunFetchAsync();
        return Ok(log);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<PriceFetchLogDto>>> GetLogs()
    {
        var logs = await _priceFetchService.GetLogsAsync();
        return Ok(logs);
    }

    [HttpGet("status")]
    public async Task<ActionResult<PriceFetchStatusDto>> GetStatus()
    {
        var status = await _priceFetchService.GetStatusAsync();

        try
        {
            var aggregated = await _eodhd.GetAggregatedStatusAsync();
            if (aggregated != null)
            {
                status.DailyApiCallsUsed = aggregated.ApiRequestsUsedToday;
                Response.Headers["X-Eodhd-DailyRateLimit"] = aggregated.DailyRateLimit.ToString();
                Response.Headers["X-Eodhd-ExtraLimit"] = aggregated.ExtraLimit.ToString();
                Response.Headers["X-Eodhd-TotalAvailable"] = aggregated.TotalAvailable.ToString();
                Response.Headers["X-Eodhd-Remaining"] = aggregated.Remaining.ToString();
            }
        }
        catch
        {
            // ignore provider errors and return local status
        }

        return Ok(status);
    }
}
