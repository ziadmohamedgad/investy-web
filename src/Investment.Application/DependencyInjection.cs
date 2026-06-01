using FluentValidation;
using Investment.Application.Mappings;
using Investment.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Investment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IPriceService, PriceService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IPriceFetchService, PriceFetchService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IExcelSyncService, ExcelSyncService>();

        return services;
    }
}
