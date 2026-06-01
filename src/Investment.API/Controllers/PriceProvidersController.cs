using Investment.Infrastructure.Services;
using Investment.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceProvidersController : ControllerBase
{
    private readonly EodhdPriceFetcher _eodhd;
    private readonly IUnitOfWork _unitOfWork;

    public PriceProvidersController(EodhdPriceFetcher eodhd, IUnitOfWork unitOfWork)
    {
        _eodhd = eodhd;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("eodhd/status")]
    public async Task<ActionResult<object>> GetEodhdStatus()
    {
        var hasApiKey = await _eodhd.HasConfiguredApiKeyAsync();
        var status = await _eodhd.GetAggregatedStatusAsync();
        if (status == null)
            return Ok(new
            {
                HasApiKey = hasApiKey,
                Name = string.Empty,
                Email = string.Empty,
                SubscriptionType = string.Empty,
                ApiRequestsUsedToday = 0,
                DailyRateLimit = 0,
                ExtraLimit = 0,
                TotalAvailable = 0,
                Remaining = 0,
                KeyCount = 0,
                Keys = Array.Empty<object>(),
                Available = (int?)null,
                Message = hasApiKey ? "Unavailable" : "MissingApiKey"
            });

        return Ok(new
        {
            HasApiKey = hasApiKey,
            Name = status.Name,
            Email = status.Email,
            SubscriptionType = status.SubscriptionType,
            ApiRequestsUsedToday = status.ApiRequestsUsedToday,
            DailyRateLimit = status.DailyRateLimit,
            ExtraLimit = status.ExtraLimit,
            TotalAvailable = status.TotalAvailable,
            Remaining = status.Remaining,
            KeyCount = status.KeyCount,
            Keys = status.Keys.Select(k => new
            {
                k.Index,
                k.Label,
                k.Name,
                k.Email,
                k.SubscriptionType,
                ApiRequestsUsedToday = k.ApiRequestsUsedToday,
                DailyRateLimit = k.DailyRateLimit,
                ExtraLimit = k.ExtraLimit,
                TotalAvailable = k.TotalAvailable,
                Remaining = k.Remaining,
                k.Available
            })
        });
    }

    [HttpGet("eodhd/configuration")]
    public async Task<ActionResult<object>> GetEodhdConfiguration()
    {
        return Ok(new { HasApiKey = await _eodhd.HasConfiguredApiKeyAsync() });
    }

    [HttpPost("eodhd/configuration")]
    public async Task<ActionResult<object>> SaveEodhdConfiguration([FromBody] SaveEodhdApiKeyRequest request)
    {
        var apiKey = request.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest(new { Message = "EODHD API key is required." });

        var info = await _eodhd.ValidateApiKeyAsync(apiKey);
        if (info == null)
            return BadRequest(new { Message = "Invalid EODHD API key or EODHD is unavailable." });

        await _unitOfWork.AppSettings.SetAsync("EodhdApiKey", apiKey);
        return Ok(new
        {
            HasApiKey = true,
            info.Name,
            info.Email,
            info.SubscriptionType,
            ApiRequestsUsedToday = EodhdPriceFetcher.GetApiRequestsUsedToday(info),
            info.DailyRateLimit,
            info.ExtraLimit,
            TotalAvailable = info.DailyRateLimit + info.ExtraLimit,
            Remaining = EodhdPriceFetcher.CalculateRemaining(info),
            KeyCount = 1,
            Keys = Array.Empty<object>()
        });
    }

    public class SaveEodhdApiKeyRequest
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
