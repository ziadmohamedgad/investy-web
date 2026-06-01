namespace Investment.Application.DTOs;

public class PortfolioDto
{
    public int PortfolioId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AssetDto> Assets { get; set; } = new();
}

public class CreatePortfolioDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdatePortfolioDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class PortfolioSummaryDto
{
    public int PortfolioId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public decimal RealizedPnL { get; set; }
    public List<AssetSummaryDto> AssetSummaries { get; set; } = new();
}

public class PortfolioAssetAssignDto
{
    public int AssetId { get; set; }
}
