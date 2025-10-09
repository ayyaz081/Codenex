# Azure Redeploy Script
# This only affects Azure deployment, not Linux

Write-Host "`n=== Azure Redeploy Script ===" -ForegroundColor Cyan
Write-Host "This will redeploy to Azure App Service only (Linux deployment not affected)`n" -ForegroundColor Green

# Configuration
$ResourceGroup = "codenex-rg"
$AppName = "codenexsolutions"  # Change if different
$ProjectPath = "C:\Users\Az\source\repos\ayyaz081\Codenex"

# Step 1: Build and Publish
Write-Host "Step 1: Building and publishing..." -ForegroundColor Yellow
Set-Location $ProjectPath
dotnet publish CodeNex.csproj -c Release -o .\publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Aborting." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green

# Step 2: Create ZIP
Write-Host "`nStep 2: Creating deployment package..." -ForegroundColor Yellow
if (Test-Path ".\deploy.zip") {
    Remove-Item ".\deploy.zip" -Force
}
Compress-Archive -Path .\publish\* -DestinationPath .\deploy.zip -Force

Write-Host "✅ Package created: deploy.zip" -ForegroundColor Green

# Step 3: Deploy to Azure
Write-Host "`nStep 3: Deploying to Azure..." -ForegroundColor Yellow
Write-Host "Deploying to: $AppName.azurewebsites.net" -ForegroundColor Cyan

az webapp deploy --resource-group $ResourceGroup --name $AppName --src-path .\deploy.zip --type zip

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Deployment failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Deployment successful!" -ForegroundColor Green

# Step 4: Restart App Service
Write-Host "`nStep 4: Restarting App Service..." -ForegroundColor Yellow
az webapp restart --resource-group $ResourceGroup --name $AppName

Write-Host "`n✅ App Service restarted" -ForegroundColor Green

# Cleanup
Write-Host "`nStep 5: Cleaning up..." -ForegroundColor Yellow
Remove-Item .\publish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\deploy.zip -Force -ErrorAction SilentlyContinue

Write-Host "`n=== Deployment Complete! ===" -ForegroundColor Green
Write-Host "Your app is live at: https://$AppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "Test the health endpoint: https://$AppName.azurewebsites.net/health" -ForegroundColor Cyan
Write-Host "`nNote: Your Linux deployment is NOT affected." -ForegroundColor Yellow
