using Investment.Application.DTOs;
using Investment.Domain.Enums;
using Investment.Domain.Interfaces;

namespace Investment.Application.Services;

public interface IAnalyticsService
{
    Task<IEnumerable<HoldingDto>> GetHoldingsAsync();
    Task<PerformanceDto> GetPerformanceAsync(string period, DateTime? fromDate, DateTime? toDate);
    Task<PortfolioAnalyticsSummaryDto> GetSummaryAsync();
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public AnalyticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<HoldingDto>> GetHoldingsAsync()
    {
        var assets = await _unitOfWork.Assets.GetAllAsync();
        var assetIds = assets.Select(a => a.AssetId).ToList();
        if (assetIds.Count == 0)
        {
            return Enumerable.Empty<HoldingDto>();
        }

        var latestPrices = await _unitOfWork.Prices.GetLatestPricesForAssetsAsync(assetIds);
        var transactionsByAsset = (await _unitOfWork.Transactions.GetByAssetIdsOrderedAsync(assetIds))
            .GroupBy(t => t.AssetId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var holdings = new List<HoldingDto>();

        foreach (var asset in assets)
        {
            var transactions = transactionsByAsset.TryGetValue(asset.AssetId, out var list)
                ? list
                : new List<Investment.Domain.Entities.Transaction>();
            if (transactions.Count == 0 && Math.Abs(asset.ClosedRealizedPnL) <= 0.005m)
            {
                continue;
            }

            var currentPrice = asset.IsDailyAccrualFund
                ? AssetService.GetCurrentPrice(asset, 0m, DateTime.UtcNow)
                : latestPrices.ContainsKey(asset.AssetId) ? latestPrices[asset.AssetId].PriceValue : 0;
            var summary = AssetService.CalculateAssetSummary(asset, transactions, currentPrice);
            if (summary.IsClosedPosition && Math.Abs(summary.RealizedPnL + summary.UnrealizedPnL) <= 0.005m)
            {
                continue;
            }

            holdings.Add(new HoldingDto
            {
                AssetId = summary.AssetId,
                AssetCode = summary.AssetCode,
                AssetName = summary.AssetName,
                AssetType = summary.AssetType,
                IsDailyAccrualFund = summary.IsDailyAccrualFund,
                DailyAccrualAnnualRatePercent = summary.DailyAccrualAnnualRatePercent,
                GoldCashbackPerGram = summary.GoldCashbackPerGram,
                TotalUnitsHeld = summary.TotalUnitsHeld,
                WeightedAverageBuyPrice = summary.AverageBuyPrice,
                TotalCostBasis = summary.TotalCostBasis,
                TotalFeesPaid = summary.TotalFeesPaid,
                TotalPaidIncludingFees = summary.TotalPaidIncludingFees,
                CurrentPrice = summary.CurrentPrice,
                CurrentValue = summary.CurrentValue,
                UnrealizedPnL = summary.UnrealizedPnL,
                UnrealizedPnLPercent = summary.UnrealizedPnLPercent,
                RealizedPnL = summary.RealizedPnL,
                RealizedPnLPercent = summary.RealizedPnLPercent,
                TotalPnL = summary.TotalPnL,
                TotalPnLPercent = summary.TotalPnLPercent,
                TotalAccruedReturn = summary.TotalAccruedReturn
            });
        }

        return holdings;
    }

    public async Task<PerformanceDto> GetPerformanceAsync(string period, DateTime? fromDate, DateTime? toDate)
    {
        var now = DateTime.UtcNow;
        var (start, end) = period.ToUpper() switch
        {
            "1D" => (now.Date.AddDays(-1), now),
            "MTD" => (new DateTime(now.Year, now.Month, 1), now),
            "YTD" => (new DateTime(now.Year, 1, 1), now),
            "CUSTOM" when fromDate.HasValue && toDate.HasValue => (fromDate.Value, toDate.Value),
            _ => (DateTime.MinValue, now) // ALL
        };

        var assets = await _unitOfWork.Assets.GetAllAsync();
        var assetIds = assets.Select(a => a.AssetId).ToList();
        var transactionsByAsset = assetIds.Count == 0
            ? new Dictionary<int, List<Investment.Domain.Entities.Transaction>>()
            : (await _unitOfWork.Transactions.GetByAssetIdsOrderedAsync(assetIds))
                .GroupBy(t => t.AssetId)
                .ToDictionary(g => g.Key, g => g.ToList());
        var assetBreakdown = new List<AssetPerformanceDto>();
        decimal totalStartValue = 0, totalEndValue = 0, totalNetCapital = 0;

        foreach (var asset in assets)
        {
            if (!transactionsByAsset.TryGetValue(asset.AssetId, out var transactions) || transactions.Count == 0)
            {
                continue;
            }

            // Calculate units held and avg cost at period start
            decimal startUnits = 0, startAvgCost = 0;
            foreach (var txn in transactions.Where(t => t.TransactionDate < start))
            {
                if (txn.TransactionType == TransactionType.Buy)
                {
                    var prev = startAvgCost * startUnits;
                    startUnits += txn.Quantity;
                    startAvgCost = startUnits > 0 ? (prev + txn.PricePerUnit * txn.Quantity) / startUnits : 0;
                }
                else
                {
                    startUnits -= txn.Quantity;
                }
            }

            // Calculate units and avg cost at period end (current)
            decimal endUnits = 0, endAvgCost = 0;
            decimal investedInPeriod = 0;
            foreach (var txn in transactions.Where(t => t.TransactionDate <= end))
            {
                if (txn.TransactionType == TransactionType.Buy)
                {
                    var prev = endAvgCost * endUnits;
                    endUnits += txn.Quantity;
                    endAvgCost = endUnits > 0 ? (prev + txn.PricePerUnit * txn.Quantity) / endUnits : 0;

                    if (txn.TransactionDate >= start)
                        investedInPeriod += txn.NetAmount;
                }
                else
                {
                    endUnits -= txn.Quantity;
                    if (txn.TransactionDate >= start)
                        investedInPeriod -= txn.NetAmount;
                }
            }

            var startPrice = await _unitOfWork.Prices.GetPriceBeforeOrOnDateAsync(asset.AssetId, start);
            var endPrice = await _unitOfWork.Prices.GetLatestByAssetIdAsync(asset.AssetId);

            var assetStartValue = startUnits * (startPrice?.PriceValue ?? startAvgCost);
            var assetEndValue = endUnits * (endPrice?.PriceValue ?? endAvgCost);

            totalStartValue += assetStartValue;
            totalEndValue += assetEndValue;
            totalNetCapital += investedInPeriod;

            var returnPct = assetStartValue > 0
                ? Math.Round((assetEndValue - assetStartValue - investedInPeriod) / assetStartValue * 100, 2)
                : 0;

            assetBreakdown.Add(new AssetPerformanceDto
            {
                AssetId = asset.AssetId,
                AssetCode = asset.AssetCode,
                AssetName = asset.AssetName,
                AssetType = asset.AssetType.ToString(),
                StartValue = Math.Round(assetStartValue, 2),
                EndValue = Math.Round(assetEndValue, 2),
                ReturnPercent = returnPct,
                InvestedInPeriod = Math.Round(investedInPeriod, 2)
            });
        }

        var absoluteReturn = totalEndValue - totalStartValue - totalNetCapital;
        var percentReturn = totalStartValue > 0
            ? Math.Round(absoluteReturn / totalStartValue * 100, 2)
            : 0;

        return new PerformanceDto
        {
            Period = period,
            FromDate = start,
            ToDate = end,
            StartingValue = Math.Round(totalStartValue, 2),
            EndingValue = Math.Round(totalEndValue, 2),
            NetInvestedCapital = Math.Round(totalNetCapital, 2),
            AbsoluteReturn = Math.Round(absoluteReturn, 2),
            PercentageReturn = percentReturn,
            AssetBreakdown = assetBreakdown
        };
    }

    public async Task<PortfolioAnalyticsSummaryDto> GetSummaryAsync()
    {
        var holdings = (await GetHoldingsAsync()).ToList();
        var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
        var totalFees = allTransactions.Sum(t => t.Fees);
        var totalInvested = holdings.Sum(h => h.TotalCostBasis);
        var totalCurrentValue = holdings.Sum(h => h.CurrentValue);
        var totalUnrealizedPnL = holdings.Sum(h => h.UnrealizedPnL);
        var totalRealizedPnL = holdings.Sum(h => h.RealizedPnL);
        var totalReturnSinceInception = totalUnrealizedPnL + totalRealizedPnL;

        return new PortfolioAnalyticsSummaryDto
        {
            TotalInvestedCapital = Math.Round(totalInvested, 2),
            TotalCurrentValue = Math.Round(totalCurrentValue, 2),
            TotalUnrealizedPnL = Math.Round(totalUnrealizedPnL, 2),
            TotalUnrealizedPnLPercent = totalInvested != 0 ? Math.Round(totalUnrealizedPnL / totalInvested * 100, 2) : 0,
            TotalRealizedPnL = Math.Round(totalRealizedPnL, 2),
            TotalFeesPaid = Math.Round(totalFees, 2),
            PortfolioReturnSinceInception = totalInvested != 0
                ? Math.Round(totalReturnSinceInception / totalInvested * 100, 2) : 0,
        };
    }
}
