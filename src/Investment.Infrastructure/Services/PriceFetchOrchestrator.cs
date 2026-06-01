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

            foreach (var (assetId, price, date) in prices)
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
            }

            sw.Stop();

            var log = new PriceFetchLog
            {
                FetchDate = DateTime.UtcNow,
                Mode = effectiveMode.ToString(),
                AssetsUpdated = assetsUpdated,
                TotalAssets = assetsWithTickerList.Count,
                Success = true,
                DurationMs = sw.Elapsed.TotalMilliseconds,
                Errors = errors.Count > 0 ? string.Join("; ", errors) : null
            };

            await unitOfWork.PriceFetchLogs.AddAsync(log);
            _logger.LogInformation("Price fetch completed. Updated {Count} assets in {Duration}ms", assetsUpdated, sw.Elapsed.TotalMilliseconds);
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
