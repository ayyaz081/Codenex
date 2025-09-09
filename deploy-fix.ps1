# Deploy fixes to Azure Web App
param(
    [string]$ProjectPath = ".\PortfolioBackend",
    [string]$OutputPath = ".\publish-fix"
)

Write-Host "Deploying Azure Web App fixes..." -ForegroundColor Green

# Clean previous builds
if (Test-Path $OutputPath) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Build and publish
Write-Host "Building application..." -ForegroundColor Yellow
try {
    dotnet clean $ProjectPath
    dotnet restore $ProjectPath
    dotnet build $ProjectPath --configuration Release --no-restore
    
    Write-Host "Publishing application..." -ForegroundColor Yellow
    dotnet publish $ProjectPath --configuration Release --no-build --output $OutputPath
    
    Write-Host "Build completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Show deployment files
Write-Host "`nPublished files:" -ForegroundColor Cyan
Get-ChildItem $OutputPath | Select-Object Name, Length

Write-Host "`n=== NEXT STEPS ===" -ForegroundColor Yellow
Write-Host "1. The application has been built with fixes applied"
Write-Host "2. Deploy the contents of '$OutputPath' to your Azure Web App"
Write-Host "3. You can use:"
Write-Host "   - Azure Portal (App Service > Deployment Center)"
Write-Host "   - Azure CLI: az webapp deployment source config-zip"
Write-Host "   - GitHub Actions (push to master branch)"
Write-Host "   - FTP/SFTP deployment"
Write-Host "4. After deployment, test with: .\test-api.ps1"

Write-Host "`n=== AZURE PORTAL CONFIGURATION ===" -ForegroundColor Yellow
Write-Host "Make sure these Application Settings are configured in Azure Portal:"
Write-Host "- ASPNETCORE_ENVIRONMENT = Production"
Write-Host "- Jwt__Key = (your secure JWT key)"
Write-Host "- Jwt__Issuer = CodenexSolutions"
Write-Host "- Jwt__Audience = CodenexSolutions"
Write-Host "- EMAIL_PASSWORD = (your email app password)"

Write-Host "`nDeployment preparation completed!" -ForegroundColor Green
