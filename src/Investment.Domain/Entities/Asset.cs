using Investment.Domain.Enums;

namespace Investment.Domain.Entities;

public class Asset
{
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public string Currency { get; set; } = "EGP";
    public string? ExternalTicker { get; set; }
    public string? Notes { get; set; }
    public bool IsDailyAccrualFund { get; set; }
    public decimal DailyAccrualAnnualRatePercent { get; set; } = 16m;
    public decimal GoldCashbackPerGram { get; set; } = 28.5m;
    public decimal ClosedRealizedPnL { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Price> Prices { get; set; } = new List<Price>();
    public ICollection<PortfolioAsset> PortfolioAssets { get; set; } = new List<PortfolioAsset>();
}
