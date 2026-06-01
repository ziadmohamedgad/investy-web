namespace Investment.Domain.Entities;

public class PortfolioAsset
{
    public int PortfolioAssetId { get; set; }
    public int PortfolioId { get; set; }
    public int AssetId { get; set; }

    // Navigation properties
    public Portfolio Portfolio { get; set; } = null!;
    public Asset Asset { get; set; } = null!;
}
