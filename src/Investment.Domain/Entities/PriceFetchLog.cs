namespace Investment.Domain.Entities;

public class PriceFetchLog
{
    public int Id { get; set; }
    public DateTime FetchDate { get; set; } = DateTime.UtcNow;
    public string Mode { get; set; } = string.Empty;
    public int AssetsUpdated { get; set; }
    public int TotalAssets { get; set; }
    public string? Errors { get; set; }
    public bool Success { get; set; }
    public double DurationMs { get; set; }
}
