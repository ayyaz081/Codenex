# CodeNex Solutions Production Deployment Script
# Ensures identical behavior between development and production

Write-Host "Starting CodeNex Solutions Production Deployment..." -ForegroundColor Green

# Set production environment
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:DOTNET_ENVIRONMENT = "Production"

# Navigate to project directory
$projectPath = "C:\Users\Az\source\repos\ayyaz081\Codenex"
Set-Location $projectPath

Write-Host "Working directory: $(Get-Location)" -ForegroundColor Cyan

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force }
if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force }

# Restore dependencies
Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed" -ForegroundColor Red
    exit 1
}

# Build the application
Write-Host "🔨 Building application..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

# Test database connection
Write-Host "🗄️  Testing database connection..." -ForegroundColor Yellow
$connectionString = "Server=tcp:codenex.database.windows.net,1433;Initial Catalog=codenex;Persist Security Info=False;User ID=codenex;Password=Az_55270;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

try {
    # Simple connection test using .NET
    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $connection.Close()
    Write-Host "✅ Database connection successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Database connection failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "🎯 Production deployment checks completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Production Configuration Summary:" -ForegroundColor Cyan
Write-Host "   🗄️  Database: Azure SQL Database (codenex)" -ForegroundColor White
Write-Host "   🌐 CORS: Allow all origins" -ForegroundColor White
Write-Host "   📧 Email: Gmail SMTP configured" -ForegroundColor White
Write-Host "   🔐 Auth: JWT with production key" -ForegroundColor White
Write-Host "   📊 Logging: Warning level for performance" -ForegroundColor White
Write-Host "   🩺 Health: /health, /health/ready, /health/live endpoints" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Ready to deploy! Run with:" -ForegroundColor Green
Write-Host "   dotnet run --configuration Release" -ForegroundColor Yellow
Write-Host ""
Write-Host "🌍 Application will be available at:" -ForegroundColor Cyan
Write-Host "   http://0.0.0.0:7150" -ForegroundColor White
Write-Host "   http://localhost:7150" -ForegroundColor White
