namespace Investment.Domain.Entities;

public class Portfolio
{
    public int PortfolioId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<PortfolioAsset> PortfolioAssets { get; set; } = new List<PortfolioAsset>();
}
