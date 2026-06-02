namespace Investment.Application.DTOs;

// Holdings endpoint
public class HoldingDto
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public bool IsDailyAccrualFund { get; set; }
    public decimal DailyAccrualAnnualRatePercent { get; set; }
    public decimal GoldCashbackPerGram { get; set; }
    public decimal TotalUnitsHeld { get; set; }
    public decimal WeightedAverageBuyPrice { get; set; }
    public decimal TotalCostBasis { get; set; }
    public decimal TotalFeesPaid { get; set; }
    public decimal TotalPaidIncludingFees { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal RealizedPnLPercent { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal TotalPnLPercent { get; set; }
}

// Performance endpoint
public class PerformanceDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal StartingValue { get; set; }
    public decimal EndingValue { get; set; }
    public decimal NetInvestedCapital { get; set; }
    public decimal AbsoluteReturn { get; set; }
    public decimal PercentageReturn { get; set; }
    public List<AssetPerformanceDto> AssetBreakdown { get; set; } = new();
}

public class AssetPerformanceDto
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal StartValue { get; set; }
    public decimal EndValue { get; set; }
    public decimal ReturnPercent { get; set; }
    public decimal InvestedInPeriod { get; set; }
}

// Summary endpoint
public class PortfolioAnalyticsSummaryDto
{
    public decimal TotalInvestedCapital { get; set; }
    public decimal TotalCurrentValue { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalUnrealizedPnLPercent { get; set; }
    public decimal TotalRealizedPnL { get; set; }
    public decimal TotalFeesPaid { get; set; }
    public decimal PortfolioReturnSinceInception { get; set; }
}

// Price Fetch DTOs
public class PriceFetchStatusDto
{
    public string CurrentMode { get; set; } = string.Empty;
    public DateTime? LastRunTime { get; set; }
    public int ActiveAssetCount { get; set; }
    public int AssetsWithTicker { get; set; }
    public int DailyApiCallsUsed { get; set; }
    public int? LastAssetsUpdated { get; set; }
}

public class PriceFetchLogDto
{
    public int Id { get; set; }
    public DateTime FetchDate { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int AssetsUpdated { get; set; }
    public int TotalAssets { get; set; }
    public string? Errors { get; set; }
    public bool Success { get; set; }
    public double DurationMs { get; set; }
}
