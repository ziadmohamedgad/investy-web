using Investment.Application.DTOs;
using Investment.Application.Services;
using Investment.Infrastructure.Services;
using Investment.Domain.Interfaces;
using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Investment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IMemoryCache _cache;
    private readonly EodhdPriceFetcher _eodhdFetcher;
    private readonly IUnitOfWork _unitOfWork;

    public AssetsController(IAssetService assetService, IMemoryCache cache, EodhdPriceFetcher eodhdFetcher, IUnitOfWork unitOfWork)
    {
        _assetService = assetService;
        _cache = cache;
        _eodhdFetcher = eodhdFetcher;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAll()
    {
        var assets = await _assetService.GetAllAsync();
        return Ok(assets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDto>> Get(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return NotFound();
        return Ok(asset);
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<AssetSummaryDto>> GetSummary(int id)
    {
        var summary = await _assetService.GetSummaryAsync(id);
        if (summary == null) return NotFound();
        return Ok(summary);
    }

    [HttpGet("{id}/compare")]
    public async Task<ActionResult<object>> Compare(int id)
    {
        var summary = await _assetService.GetSummaryAsync(id);
        if (summary == null) return NotFound();

        var asset = await _unitOfWork.Assets.GetByIdAsync(id);
        if (asset == null) return NotFound();

        var external = await FetchLivePriceAsync(asset);

        return Ok(new { Local = summary, External = external == null ? null : new { price = external.Value.Price, date = external.Value.Date } });
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ExternalAssetSearchDto>>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(Array.Empty<ExternalAssetSearchDto>());

        var q = ToAssetCode(query).Trim().ToUpperInvariant();
        if (q.Length == 0)
            return Ok(Array.Empty<ExternalAssetSearchDto>());

        var localAssets = await SearchLocalAssetsAsync(q);

        if (localAssets.Any())
            return Ok(localAssets);

        var cacheKey = $"eodhd:asset-search:{q.ToUpperInvariant()}";
        List<(string Code, string Name, string Type, string Currency, string ExternalTicker)>? eodhdAssets = null;
        if (!_cache.TryGetValue(cacheKey, out eodhdAssets) || eodhdAssets == null)
        {
            eodhdAssets = await _eodhdFetcher.SearchAssetsAsync(q);
            _cache.Set(cacheKey, eodhdAssets, TimeSpan.FromHours(12));
        }

        var results = eodhdAssets
            .Select(a => new ExternalAssetSearchDto
                {
                    AssetCode = a.Code,
                    AssetName = a.Name,
                    AssetType = a.Type,
                    Currency = a.Currency,
                    ExternalTicker = a.ExternalTicker
                })
            .GroupBy(a => a.ExternalTicker ?? a.AssetCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(20)
            .ToList();

        if (results.Any())
            return Ok(results);

        return Ok(Array.Empty<ExternalAssetSearchDto>());
    }

    [HttpGet("non-stock-suggestions")]
    public async Task<ActionResult<IEnumerable<ExternalAssetSearchDto>>> GetNonStockSuggestions([FromQuery] string? query, [FromQuery] string? assetType)
    {
        var assets = await _unitOfWork.Assets.GetAllAsync();
        
        var normalizedType = assetType?.Trim();
        var suggestions = assets
            .Where(a => a.IsActive)
            .Where(a => string.IsNullOrWhiteSpace(normalizedType)
                     || (normalizedType == "DailyAccrualFund" ? a.IsDailyAccrualFund : a.AssetType.ToString() == normalizedType))
            .Where(a => 
            {
                if (string.IsNullOrWhiteSpace(query))
                    return true;
                var q = query.Trim().ToLower();
                return a.AssetCode.ToLower().StartsWith(q) ||
                       a.AssetName.ToLower().StartsWith(q);
            })
            .OrderBy(a => a.AssetCode)
            .Take(20)
            .Select(a => new ExternalAssetSearchDto
            {
                AssetCode = a.AssetCode,
                AssetName = a.AssetName,
                AssetType = a.AssetType.ToString(),
                Currency = a.Currency,
                ExternalTicker = a.ExternalTicker ?? a.AssetCode,
                IsDailyAccrualFund = a.IsDailyAccrualFund
            })
            .ToList();

        return Ok(suggestions);
    }

    [HttpGet("latest-price")]
    public async Task<ActionResult<object>> GetLatestPrice([FromQuery] string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            return BadRequest(new { message = "Ticker is required." });

        var normalizedTicker = ticker.Trim().ToUpperInvariant();
        var cacheKey = $"eodhd:latest-price:{normalizedTicker}";

        if (!_cache.TryGetValue(cacheKey, out (decimal Price, DateTime Date)? latestPrice) || latestPrice == null)
        {
            latestPrice = await _eodhdFetcher.FetchLatestPriceAsync(normalizedTicker);
            if (latestPrice != null)
                _cache.Set(cacheKey, latestPrice, TimeSpan.FromMinutes(10));
        }

        if (latestPrice == null)
            return NotFound(new { message = "No latest price found for ticker." });

        return Ok(new { price = latestPrice.Value.Price, date = latestPrice.Value.Date });
    }

    [HttpGet("summaries")]
    public async Task<ActionResult<IEnumerable<AssetSummaryDto>>> GetAllSummaries()
    {
        var assets = (await _unitOfWork.Assets.GetActiveAssetsAsync())
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.AssetId)
            .ToList();
        var assetIds = assets.Select(a => a.AssetId).ToList();
        var latestPrices = await _unitOfWork.Prices.GetLatestPricesForAssetsAsync(assetIds);
        var summaries = new List<AssetSummaryDto>();
        foreach (var asset in assets)
        {
            var transactions = (await _unitOfWork.Transactions.GetByAssetIdOrderedAsync(asset.AssetId)).ToList();
            if (transactions.Count == 0 && Math.Abs(asset.ClosedRealizedPnL) <= 0.005m)
            {
                continue;
            }

            var currentPrice = latestPrices.TryGetValue(asset.AssetId, out var price) ? price.PriceValue : 0m;
            var s = AssetService.CalculateAssetSummary(asset, transactions, currentPrice);
            if (s != null) summaries.Add(s);
        }
        return Ok(summaries);
    }

    [HttpPost("ensure")]
    public async Task<ActionResult<AssetDto>> Ensure([FromBody] EnsureAssetRequestDto dto)
    {
        var normalizedCode = NormalizeAssetCode(dto.AssetCode);
        var existing = await _assetService.GetByCodeAsync(normalizedCode);
        if (existing != null)
        {
            return Ok(existing);
        }

        var assetType = NormalizeAssetType(dto.AssetType);
        string? externalTicker = null;
        if (assetType == AssetType.Stock.ToString())
        {
            externalTicker = NormalizeOptionalText(dto.ExternalTicker, 20) ??
                               (normalizedCode.Length <= 20 ? normalizedCode : normalizedCode[..20]);
        }

        try
        {
            var created = await _assetService.CreateAsync(new CreateAssetDto
            {
                AssetCode = normalizedCode,
                AssetName = NormalizeRequiredText(dto.AssetName, 200),
                AssetType = assetType,
                Currency = NormalizeRequiredText(dto.Currency, 10, "EGP"),
                ExternalTicker = externalTicker,
                Notes = null,
                IsDailyAccrualFund = dto.IsDailyAccrualFund,
                DailyAccrualAnnualRatePercent = dto.IsDailyAccrualFund ? 16m : 0m,
                IsActive = true
            });

            if (assetType == AssetType.Stock.ToString())
                await TrySyncNewAssetPriceAsync(created);

            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("manual")]
    public async Task<ActionResult<AssetDto>> CreateManual([FromBody] CreateManualAssetDto? dto)
    {
        if (dto == null)
            return BadRequest(new { message = "بيانات الأصل مطلوبة." });

        try
        {
            var created = await _assetService.CreateManualAsync(dto);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message,
                detailed = ex.InnerException?.Message
            });
        }
    }

    [HttpPut("{id:int}/current-price")]
    public async Task<IActionResult> SetCurrentPrice(int id, [FromBody] SetAssetCurrentPriceDto dto)
    {
        try
        {
            var updated = await _assetService.SetCurrentPriceAsync(id, dto.Price, dto.PriceDate);
            if (!updated) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/sync-price")]
    public async Task<ActionResult<object>> SyncPrice(int id)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(id);
        if (asset == null)
            return NotFound();

        if (asset.AssetType != AssetType.Stock || string.IsNullOrWhiteSpace(asset.ExternalTicker))
            return BadRequest(new { message = "Price sync is available for stock assets with an external ticker only." });

        var latest = await FetchLivePriceAsync(asset);
        if (latest == null)
            return NotFound(new { message = "No latest price found for this asset." });

        await _unitOfWork.Prices.AddAsync(new Price
        {
            AssetId = asset.AssetId,
            PriceDate = latest.Value.Date,
            PriceValue = latest.Value.Price,
            Source = PriceSource.EODHD,
            CreatedAt = DateTime.UtcNow
        });

        return Ok(new { price = latest.Value.Price, date = latest.Value.Date });
    }

    [HttpPut("{id:int}/financial-settings")]
    public async Task<IActionResult> UpdateFinancialSettings(int id, [FromBody] SetAssetFinancialSettingsDto dto)
    {
        try
        {
            var updated = await _assetService.UpdateFinancialSettingsAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<AssetDto>> Create([FromBody] CreateAssetDto dto)
    {
        try
        {
            var created = await _assetService.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.AssetId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private async Task<(int AssetId, decimal Price, DateTime Date)?> FetchLivePriceAsync(Asset asset)
    {
        if (string.IsNullOrWhiteSpace(asset.ExternalTicker))
            return null;

        try
        {
            var temp = new Asset { AssetId = asset.AssetId, ExternalTicker = asset.ExternalTicker };
            var list = await _eodhdFetcher.FetchPricesAsync(new[] { temp });
            var first = list.FirstOrDefault();
            return first == default ? null : first;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeAssetType(string? assetType)
    {
        if (string.IsNullOrWhiteSpace(assetType))
            return AssetType.Stock.ToString();

        var trimmed = assetType.Trim();
        if (Enum.TryParse<AssetType>(trimmed, true, out var parsed))
            return parsed.ToString();

        return trimmed.Contains("stock", StringComparison.OrdinalIgnoreCase)
            ? AssetType.Stock.ToString()
            : AssetType.Other.ToString();
    }

    private static string NormalizeAssetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return ToAssetCode(code).Trim().ToUpperInvariant();
    }

    private static string ToAssetCode(string value) => new(value
        .Where(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
        .ToArray());

    private static string NormalizeRequiredText(string? value, int maxLength, string fallback = "Unknown")
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private async Task<List<ExternalAssetSearchDto>> SearchLocalAssetsAsync(string query)
    {
        return (await _unitOfWork.Assets.GetAllAsync())
            .Where(a => a.IsActive
                     && (a.AssetCode.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                         || a.AssetName.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                         || (!string.IsNullOrWhiteSpace(a.ExternalTicker)
                             && a.ExternalTicker.StartsWith(query, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(a => a.AssetCode.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a.AssetName.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a.AssetCode)
            .Take(20)
            .Select(a => new ExternalAssetSearchDto
            {
                AssetCode = a.AssetCode,
                AssetName = a.AssetName,
                AssetType = a.AssetType.ToString(),
                Currency = a.Currency,
                ExternalTicker = a.ExternalTicker ?? a.AssetCode,
                IsDailyAccrualFund = a.IsDailyAccrualFund
            })
            .ToList();
    }

    private async Task TrySyncNewAssetPriceAsync(AssetDto asset)
    {
        if (!string.Equals(asset.AssetType, AssetType.Stock.ToString(), StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(asset.ExternalTicker))
            return;

        var startedAt = DateTime.UtcNow;
        var assetsUpdated = 0;
        string? error = null;

        try
        {
            var existingToday = await _unitOfWork.Prices.GetLatestByAssetIdAsync(asset.AssetId);

            if (existingToday != null && existingToday.PriceDate.Date == DateTime.UtcNow.Date)
            {
                assetsUpdated = 0;
                return;
            }

            var temp = new Asset { AssetId = asset.AssetId, ExternalTicker = asset.ExternalTicker };
            var prices = await _eodhdFetcher.FetchPricesAsync(new[] { temp });
            var latest = prices.OrderByDescending(p => p.Date).FirstOrDefault();

            if (latest != default)
            {
                await _unitOfWork.Prices.AddAsync(new Price
                {
                    AssetId = latest.AssetId,
                    PriceDate = latest.Date,
                    PriceValue = latest.Price,
                    Source = PriceSource.EODHD,
                    CreatedAt = DateTime.UtcNow
                });
                assetsUpdated = 1;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            await _unitOfWork.PriceFetchLogs.AddAsync(new PriceFetchLog
            {
                FetchDate = DateTime.UtcNow,
                Mode = "EODHD_NEW_ASSET",
                AssetsUpdated = assetsUpdated,
                TotalAssets = 1,
                Success = error == null,
                DurationMs = (DateTime.UtcNow - startedAt).TotalMilliseconds,
                Errors = error
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AssetDto>> Update(int id, [FromBody] UpdateAssetDto dto)
    {
        try
        {
            var updated = await _assetService.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _assetService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
