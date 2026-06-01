namespace Investment.Application.DTOs;

public class PriceDto
{
    public int PriceId { get; set; }
    public int AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public DateTime PriceDate { get; set; }
    public decimal Price { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreatePriceDto
{
    public int AssetId { get; set; }
    public DateTime PriceDate { get; set; }
    public decimal Price { get; set; }
}

public class BulkPriceDto
{
    public List<BulkPriceItem> Prices { get; set; } = new();
}

public class BulkPriceItem
{
    public string AssetCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
}
