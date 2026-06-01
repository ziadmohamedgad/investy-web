using Investment.Domain.Enums;

namespace Investment.Domain.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    public int AssetId { get; set; }
    public TransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal ManufacturingFeePerGram { get; set; }
    public decimal NetAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Asset Asset { get; set; } = null!;
}
