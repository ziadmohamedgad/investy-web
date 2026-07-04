namespace Investment.Application.DTOs;

public class TransactionDto
{
    public int TransactionId { get; set; }
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public bool IsDailyAccrualFund { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal ManufacturingFeePerGram { get; set; }
    public decimal NetAmount { get; set; }
    public string DividendKind { get; set; } = "Cash";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTransactionDto
{
    public int AssetId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal Fees { get; set; }
    public decimal ManufacturingFeePerGram { get; set; }
    public string DividendKind { get; set; } = "Cash";
    public string? Notes { get; set; }
}

public class UpdateTransactionDto
{
    public int AssetId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal Fees { get; set; }
    public decimal ManufacturingFeePerGram { get; set; }
    public string DividendKind { get; set; } = "Cash";
    public string? Notes { get; set; }
}
