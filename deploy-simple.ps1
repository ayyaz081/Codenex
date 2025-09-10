# CodeNex Solutions Production Deployment Script
Write-Host "Starting CodeNex Solutions Production Deployment..." -ForegroundColor Green

# Set production environment
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Navigate to project directory
$projectPath = "C:\Users\Az\source\repos\ayyaz081\Portfolio\PortfolioBackend"
Set-Location $projectPath

Write-Host "Working directory: $(Get-Location)" -ForegroundColor Cyan

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force }
if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force }

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "Production build completed successfully!" -ForegroundColor Green
Write-Host "Ready to deploy! Run: dotnet run --configuration Release" -ForegroundColor Yellow
