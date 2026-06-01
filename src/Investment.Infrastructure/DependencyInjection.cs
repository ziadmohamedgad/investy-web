using Investment.Domain.Interfaces;
using Investment.Infrastructure.Data;
using Investment.Infrastructure.Repositories;
using Investment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Investment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InvestmentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IPriceFetchLogRepository, PriceFetchLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Price fetchers
        services.AddHttpClient<EodhdPriceFetcher>();
        services.AddScoped<IPriceFetchOrchestrator, PriceFetchOrchestrator>();

        return services;
    }
}
