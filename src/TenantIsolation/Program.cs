// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using TenantIsolation.Configuration;
using TenantIsolation.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.;Database=TenantIsolation;Trusted_Connection=true;TrustServerCertificate=true;";

// Register tenant isolation framework
builder.Services.AddTenantIsolationSqlServer(connectionString, options =>
{
    options.AutoMigrate = true;
    options.EnableAuditLogging = true;
    options.EnableSoftDeleteFilter = true;
    options.ValidateTenantOnEveryRequest = true;
});

// Register feature toggle service
builder.Services.AddTenantFeatureToggle();

// Add logging
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    if (builder.Environment.IsDevelopment())
        config.AddDebug();
});

// Build app
var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// Use tenant resolution middleware
app.UseTenantResolution();

app.UseAuthorization();
app.MapControllers();

// Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

    try
    {
        if (dbContext.Database.IsSqlServer())
            await dbContext.Database.MigrateAsync();
        else if (dbContext.Database.IsInMemory())
            await dbContext.Database.EnsureCreatedAsync();

        Console.WriteLine("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration failed: {ex.Message}");
        throw;
    }
}

app.Run();
