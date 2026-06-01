using Investment.Domain.Enums;

namespace Investment.Domain.Entities;

public class Price
{
    public int PriceId { get; set; }
    public int AssetId { get; set; }
    public DateTime PriceDate { get; set; }
    public decimal PriceValue { get; set; }
    public PriceSource Source { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Asset Asset { get; set; } = null!;
}
