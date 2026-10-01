using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Investment.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Investment.Infrastructure.Services;

public class PriceFetchOrchestrator : IPriceFetchOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PriceFetchOrchestrator> _logger;

    public PriceFetchOrchestrator(IServiceProvider serviceProvider, ILogger<PriceFetchOrchestrator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<PriceFetchLog> ExecuteFetchAsync(bool isIntraday = false)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<string>();
        int assetsUpdated = 0;

        var cairoToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")).Date;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var eodhdFetcher = scope.ServiceProvider.GetRequiredService<EodhdPriceFetcher>();

        try
        {
            var activeAssetCount = await unitOfWork.Assets.CountActiveAssetsAsync();
            var effectiveMode = FetchMode.EODHD;

            _logger.LogInformation("Price fetch executing. Mode: {Mode}, Active assets: {Count}, Intraday: {Intraday}",
                effectiveMode, activeAssetCount, isIntraday);

            var assetsWithTicker = await unitOfWork.Assets.GetActiveStockAssetsWithTickerAsync();
            var assetsWithTickerList = assetsWithTicker.ToList();
            var prices = await eodhdFetcher.FetchPricesAsync(assetsWithTickerList);

            var pricesList = prices.ToList();
            DateTime? latestSyncedDate = null;

            foreach (var (assetId, price, date) in pricesList)
            {
                if (isIntraday)
                {
                    var lastPrice = await unitOfWork.Prices.GetLastPriceForAssetOnDateAsync(assetId, date);
                    if (lastPrice != null && lastPrice.PriceValue == price)
                        continue;
                }

                await unitOfWork.Prices.AddAsync(new Price
                {
                    AssetId = assetId,
                    PriceDate = date,
                    PriceValue = price,
                    Source = PriceSource.EODHD,
                    CreatedAt = DateTime.UtcNow
                });
                assetsUpdated++;

                if (latestSyncedDate == null || date.Date > latestSyncedDate)
                    latestSyncedDate = date.Date;
            }

            sw.Stop();

            string? syncedDateNote = null;
            if (latestSyncedDate != null)
            {
                syncedDateNote = latestSyncedDate.Value.Date == cairoToday
                    ? "تم مزامنة الأسعار حتى تاريخ اليوم"
                    : $"تم مزامنة الأسعار حتى {latestSyncedDate.Value.ToString("dddd d/M/yyyy", new System.Globalization.CultureInfo("ar-EG"))}";
            }

            var log = new PriceFetchLog
            {
                FetchDate = DateTime.UtcNow,
                Mode = effectiveMode.ToString(),
                AssetsUpdated = assetsUpdated,
                TotalAssets = assetsWithTickerList.Count,
                Success = true,
                DurationMs = sw.Elapsed.TotalMilliseconds,
                Errors = errors.Count > 0
                    ? string.Join("; ", errors) + (syncedDateNote != null ? "; " + syncedDateNote : null)
                    : syncedDateNote
            };

            await unitOfWork.PriceFetchLogs.AddAsync(log);
            _logger.LogInformation("Price fetch completed. Updated {Count} assets. SyncedDate: {Date}. Duration: {Duration}ms",
                assetsUpdated, latestSyncedDate?.ToString("dd/MM/yyyy") ?? "none", sw.Elapsed.TotalMilliseconds);
            return log;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Price fetch failed");

            var log = new PriceFetchLog
            {
                FetchDate = DateTime.UtcNow,
                Mode = "ERROR",
                AssetsUpdated = assetsUpdated,
                TotalAssets = 0,
                Success = false,
                DurationMs = sw.Elapsed.TotalMilliseconds,
                Errors = ex.Message
            };

            try
            {
                await unitOfWork.PriceFetchLogs.AddAsync(log);
            }
            catch { /* Don't fail on logging */ }

            return log;
        }
    }
}
