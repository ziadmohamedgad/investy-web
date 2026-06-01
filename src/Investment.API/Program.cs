using Investment.Application;
using Investment.Application.Services;
using Investment.Infrastructure;
using Investment.Infrastructure.Data;
using Investment.API.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddMemoryCache();

// Swagger/OpenAPI temporarily disabled due to package version conflicts.
// (Core API functionality is unaffected.)

// Clean Architecture Layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        builder =>
        {
            builder.WithOrigins("http://localhost:4200")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowAngularDev");

app.UseAuthorization();

app.MapControllers();

// Initialize DB and apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InvestmentDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        context.Database.Migrate();
        // Seed data disabled to avoid inserting demo data into user databases.

        var excelSyncService = scope.ServiceProvider.GetRequiredService<IExcelSyncService>();
        await excelSyncService.RefreshAsync();

        logger.LogInformation("Database initialized.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
    }
}

app.Run();
