using Investment.Domain.Entities;
using Investment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Investment.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(InvestmentDbContext context, ILogger logger)
    {
        if (await context.Assets.AnyAsync())
        {
            logger.LogInformation("Database already contains data. Skipping seed.");
            return;
        }

        logger.LogInformation("Seeding database with initial data...");

        // Seed AppSettings
        var settings = new List<AppSetting>
        {
            new() { SettingKey = "PriceFetchMode", SettingValue = "AUTO", LastUpdated = DateTime.UtcNow },
            new() { SettingKey = "EodhdApiKey", SettingValue = "demo", LastUpdated = DateTime.UtcNow }
        };
        context.AppSettings.AddRange(settings);

        // Seed Assets
        var assets = new List<Asset>
        {
            new()
            {
                AssetCode = "COMI",
                AssetName = "Commercial International Bank",
                AssetType = AssetType.Stock,
                Currency = "EGP",
                ExternalTicker = "COMI",
                IsActive = true,
                Notes = "Largest private sector bank in Egypt",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                AssetCode = "HRHO",
                AssetName = "Hermes Holding",
                AssetType = AssetType.Stock,
                Currency = "EGP",
                ExternalTicker = "HRHO",
                IsActive = true,
                Notes = "Leading financial services corporation",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                AssetCode = "GOLD-EGP",
                AssetName = "Gold (21K per gram)",
                AssetType = AssetType.Gold,
                Currency = "EGP",
                ExternalTicker = null,
                IsActive = true,
                Notes = "Physical gold investment in EGP",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                AssetCode = "BFUND",
                AssetName = "Banque Misr Investment Fund",
                AssetType = AssetType.Fund,
                Currency = "EGP",
                ExternalTicker = "BFUND",
                IsActive = true,
                Notes = "Fixed income investment fund",
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Assets.AddRange(assets);
        await context.SaveChangesAsync();

        // Seed Portfolios
        var portfolios = new List<Portfolio>
        {
            new() { Name = "Long Term", Description = "Long-term buy and hold investments", CreatedAt = DateTime.UtcNow },
            new() { Name = "Short Term", Description = "Short-term trading positions", CreatedAt = DateTime.UtcNow }
        };
        context.Portfolios.AddRange(portfolios);
        await context.SaveChangesAsync();

        // Assign assets to portfolios
        var portfolioAssets = new List<PortfolioAsset>
        {
            new() { PortfolioId = portfolios[0].PortfolioId, AssetId = assets[0].AssetId }, // COMI -> Long Term
            new() { PortfolioId = portfolios[0].PortfolioId, AssetId = assets[2].AssetId }, // Gold -> Long Term
            new() { PortfolioId = portfolios[0].PortfolioId, AssetId = assets[3].AssetId }, // Fund -> Long Term
            new() { PortfolioId = portfolios[1].PortfolioId, AssetId = assets[1].AssetId }, // HRHO -> Short Term
            new() { PortfolioId = portfolios[1].PortfolioId, AssetId = assets[0].AssetId }, // COMI -> Short Term
        };
        context.PortfolioAssets.AddRange(portfolioAssets);

        // Seed Transactions (15 transactions over past 6 months)
        var now = DateTime.UtcNow;
        var transactions = new List<Transaction>
        {
            // COMI buys
            new() { AssetId = assets[0].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-6), Quantity = 100, PricePerUnit = 52.00m, TotalAmount = 5200.00m, Fees = 26.00m, NetAmount = 5226.00m, CreatedAt = now },
            new() { AssetId = assets[0].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-4), Quantity = 50, PricePerUnit = 55.50m, TotalAmount = 2775.00m, Fees = 13.88m, NetAmount = 2788.88m, CreatedAt = now },
            new() { AssetId = assets[0].AssetId, TransactionType = TransactionType.Sell, TransactionDate = now.AddMonths(-2), Quantity = 30, PricePerUnit = 60.00m, TotalAmount = 1800.00m, Fees = 9.00m, NetAmount = 1791.00m, CreatedAt = now },
            new() { AssetId = assets[0].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-1), Quantity = 75, PricePerUnit = 57.25m, TotalAmount = 4293.75m, Fees = 21.47m, NetAmount = 4315.22m, CreatedAt = now },

            // HRHO buys & sells
            new() { AssetId = assets[1].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-5), Quantity = 200, PricePerUnit = 18.30m, TotalAmount = 3660.00m, Fees = 18.30m, NetAmount = 3678.30m, CreatedAt = now },
            new() { AssetId = assets[1].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-3), Quantity = 150, PricePerUnit = 19.75m, TotalAmount = 2962.50m, Fees = 14.81m, NetAmount = 2977.31m, CreatedAt = now },
            new() { AssetId = assets[1].AssetId, TransactionType = TransactionType.Sell, TransactionDate = now.AddMonths(-1), Quantity = 100, PricePerUnit = 22.00m, TotalAmount = 2200.00m, Fees = 11.00m, NetAmount = 2189.00m, CreatedAt = now },

            // Gold buys
            new() { AssetId = assets[2].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-6), Quantity = 10, PricePerUnit = 2850.00m, TotalAmount = 28500.00m, Fees = 0.00m, NetAmount = 28500.00m, CreatedAt = now },
            new() { AssetId = assets[2].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-3), Quantity = 5, PricePerUnit = 3100.00m, TotalAmount = 15500.00m, Fees = 0.00m, NetAmount = 15500.00m, CreatedAt = now },
            new() { AssetId = assets[2].AssetId, TransactionType = TransactionType.Sell, TransactionDate = now.AddDays(-15), Quantity = 3, PricePerUnit = 3350.00m, TotalAmount = 10050.00m, Fees = 0.00m, NetAmount = 10050.00m, CreatedAt = now },

            // Fund buys
            new() { AssetId = assets[3].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-5), Quantity = 500, PricePerUnit = 12.50m, TotalAmount = 6250.00m, Fees = 31.25m, NetAmount = 6281.25m, CreatedAt = now },
            new() { AssetId = assets[3].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-3), Quantity = 300, PricePerUnit = 12.80m, TotalAmount = 3840.00m, Fees = 19.20m, NetAmount = 3859.20m, CreatedAt = now },
            new() { AssetId = assets[3].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddMonths(-1), Quantity = 200, PricePerUnit = 13.10m, TotalAmount = 2620.00m, Fees = 13.10m, NetAmount = 2633.10m, CreatedAt = now },
            new() { AssetId = assets[3].AssetId, TransactionType = TransactionType.Sell, TransactionDate = now.AddDays(-10), Quantity = 100, PricePerUnit = 13.50m, TotalAmount = 1350.00m, Fees = 6.75m, NetAmount = 1343.25m, CreatedAt = now },

            // Extra COMI buy
            new() { AssetId = assets[0].AssetId, TransactionType = TransactionType.Buy, TransactionDate = now.AddDays(-5), Quantity = 25, PricePerUnit = 58.00m, TotalAmount = 1450.00m, Fees = 7.25m, NetAmount = 1457.25m, CreatedAt = now },
        };
        context.Transactions.AddRange(transactions);

        // Seed Prices for past 30 days
        var random = new Random(42);
        var prices = new List<Price>();
        var basePrices = new Dictionary<int, decimal>
        {
            { assets[0].AssetId, 58.00m },  // COMI
            { assets[1].AssetId, 21.50m },   // HRHO
            { assets[2].AssetId, 3300.00m }, // Gold
            { assets[3].AssetId, 13.25m }    // Fund
        };

        for (int day = 30; day >= 0; day--)
        {
            var date = now.AddDays(-day).Date;
            foreach (var asset in assets)
            {
                var basePrice = basePrices[asset.AssetId];
                var variation = (decimal)(random.NextDouble() * 0.04 - 0.02); // ±2%
                var price = Math.Round(basePrice * (1 + variation), 4);
                basePrices[asset.AssetId] = price; // drift

                prices.Add(new Price
                {
                    AssetId = asset.AssetId,
                    PriceDate = date,
                    PriceValue = price,
                    Source = PriceSource.Manual,
                    CreatedAt = now
                });
            }
        }
        context.Prices.AddRange(prices);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeded successfully with {AssetCount} assets, {TxnCount} transactions, {PriceCount} prices, {PortfolioCount} portfolios.",
            assets.Count, transactions.Count, prices.Count, portfolios.Count);
    }
}
