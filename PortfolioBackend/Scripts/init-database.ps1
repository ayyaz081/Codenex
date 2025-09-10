# Portfolio Backend Database Initialization Script
# This script initializes the database and runs migrations for production deployment

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString,
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Production",
    
    [Parameter(Mandatory=$false)]
    [switch]$Force = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SeedData = $false
)

Write-Host "Portfolio Backend Database Initialization" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Set environment variable
$env:ASPNETCORE_ENVIRONMENT = $Environment

# Change to the backend directory
$backendPath = Join-Path $PSScriptRoot ".."
Set-Location $backendPath

Write-Host "Current directory: $(Get-Location)" -ForegroundColor Yellow

# Check if connection string is provided
if ($ConnectionString) {
    $env:CONNECTION_STRING = $ConnectionString
    Write-Host "Using provided connection string" -ForegroundColor Green
} else {
    Write-Host "Using connection string from configuration" -ForegroundColor Yellow
}

try {
    # Build the project
    Write-Host "Building the project..." -ForegroundColor Yellow
    dotnet build --configuration Release --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed. Please fix build errors before running migrations."
        exit 1
    }

    # Run database migrations
    Write-Host "Running database migrations..." -ForegroundColor Yellow
    
    if ($Force) {
        Write-Host "Force flag set - dropping existing database" -ForegroundColor Red
        dotnet ef database drop --force
    }
    
    # Update database with migrations
    dotnet ef database update
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Database migration failed."
        exit 1
    }
    
    Write-Host "Database migrations completed successfully!" -ForegroundColor Green
    
    # Seed initial data if requested
    if ($SeedData) {
        Write-Host "Seeding initial data..." -ForegroundColor Yellow
        
        # Create a minimal seed data script
        $seedScript = @"
using Microsoft.EntityFrameworkCore;
using PortfolioBackend.Data;
using PortfolioBackend.Models;
using Microsoft.AspNetCore.Identity;

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? 
                      "Data Source=./wwwroot/App_Data/PortfolioDB.sqlite";

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlite(connectionString);

using var context = new AppDbContext(optionsBuilder.Options);

// Ensure database is created
await context.Database.EnsureCreatedAsync();

// Add any initial seed data here
Console.WriteLine("Database seeded successfully!");
"@
        
        $seedFile = Join-Path $env:TEMP "SeedDatabase.cs"
        $seedScript | Out-File -FilePath $seedFile -Encoding UTF8
        
        # This would need to be implemented as a proper seeding mechanism
        Write-Host "Seed data template created at: $seedFile" -ForegroundColor Green
        Write-Host "Note: Implement proper seeding in your application startup" -ForegroundColor Yellow
    }
    
    Write-Host "" -ForegroundColor Green
    Write-Host "✅ Database initialization completed successfully!" -ForegroundColor Green
    Write-Host "✅ Your database is ready for production!" -ForegroundColor Green
    
} catch {
    Write-Error "An error occurred during database initialization: $_"
    exit 1
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Verify your database connection" -ForegroundColor White
Write-Host "2. Check that all required environment variables are set" -ForegroundColor White
Write-Host "3. Test your application endpoints" -ForegroundColor White
Write-Host "4. Monitor application logs for any issues" -ForegroundColor White
