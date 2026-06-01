using Investment.Domain.Enums;

namespace Investment.Application.DTOs;

// === Asset DTOs ===
public class AssetDto
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Currency { get; set; } = "EGP";
    public string? ExternalTicker { get; set; }
    public string? Notes { get; set; }
    public bool IsDailyAccrualFund { get; set; }
    public decimal DailyAccrualAnnualRatePercent { get; set; }
    public decimal GoldCashbackPerGram { get; set; } = 28.5m;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Portfolios { get; set; } = new();
}

public class CreateAssetDto
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Currency { get; set; } = "EGP";
    public string? ExternalTicker { get; set; }
    public string? Notes { get; set; }
    public bool IsDailyAccrualFund { get; set; }
    public decimal DailyAccrualAnnualRatePercent { get; set; } = 16m;
    public decimal GoldCashbackPerGram { get; set; } = 28.5m;
    public bool IsActive { get; set; } = true;
}

public class UpdateAssetDto
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Currency { get; set; } = "EGP";
    public string? ExternalTicker { get; set; }
    public string? Notes { get; set; }
    public bool IsDailyAccrualFund { get; set; }
    public decimal DailyAccrualAnnualRatePercent { get; set; } = 16m;
    public decimal GoldCashbackPerGram { get; set; } = 28.5m;
    public bool IsActive { get; set; } = true;
}

public class AssetSummaryDto
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public bool IsDailyAccrualFund { get; set; }
    public decimal DailyAccrualAnnualRatePercent { get; set; }
    public decimal GoldCashbackPerGram { get; set; } = 28.5m;
    public decimal TotalUnitsHeld { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal TotalCostBasis { get; set; }
    public decimal TotalFeesPaid { get; set; }
    public decimal TotalPaidIncludingFees { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal TotalPnLPercent { get; set; }
}

public class ExternalAssetSearchDto
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = "Stock";
    public string Currency { get; set; } = "EGP";
    public string? ExternalTicker { get; set; }
    public bool IsDailyAccrualFund { get; set; }
}

public class EnsureAssetRequestDto
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = "Stock";
    public string Currency { get; set; } = "EGP";
    public string? ExternalTicker { get; set; }
    public bool IsDailyAccrualFund { get; set; }
}

public class CreateManualAssetDto
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Currency { get; set; } = "EGP";
    public string? Notes { get; set; }
    public decimal? InitialPrice { get; set; }
    public bool IsDailyAccrualFund { get; set; }
    public decimal DailyAccrualAnnualRatePercent { get; set; } = 16m;
    public decimal GoldCashbackPerGram { get; set; } = 28.5m;
}

public class SetAssetCurrentPriceDto
{
    public decimal Price { get; set; }
    public DateTime? PriceDate { get; set; }
}

public class SetAssetFinancialSettingsDto
{
    public decimal? GoldCashbackPerGram { get; set; }
    public decimal? DailyAccrualAnnualRatePercent { get; set; }
}
