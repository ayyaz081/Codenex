# CodeNex Deployment Verification Script

Write-Host "🔍 Checking deployment files and configurations..." -ForegroundColor Cyan

# Check if web.config exists and has correct DLL reference
if (Test-Path "web.config") {
    $webConfig = Get-Content "web.config" -Raw
    if ($webConfig -match "CodeNex.dll") {
        Write-Host "✅ web.config references correct DLL (CodeNex.dll)" -ForegroundColor Green
    } else {
        Write-Host "❌ web.config references wrong DLL!" -ForegroundColor Red
        Write-Host "   Please ensure it points to 'CodeNex.dll'" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ web.config not found!" -ForegroundColor Red
}

# Check GitHub Actions workflow
if (Test-Path ".github\workflows\master_codenex.yml") {
    Write-Host "✅ GitHub Actions workflow found" -ForegroundColor Green
} else {
    Write-Host "❌ GitHub Actions workflow missing!" -ForegroundColor Red
}

# Verify project file
if (Test-Path "CodeNex.csproj") {
    Write-Host "✅ Project file found (CodeNex.csproj)" -ForegroundColor Green
} else {
    Write-Host "❌ Project file missing!" -ForegroundColor Red
}

# Check required files for deployment
$requiredFiles = @(
    "Program.cs",
    "web.config",
    "CodeNex.csproj",
    "appsettings.json",
    "appsettings.Production.json"
)

Write-Host "`n📋 Checking required files:" -ForegroundColor Cyan
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file" -ForegroundColor Red
    }
}

# Build project to verify no errors
Write-Host "`n🔨 Testing build..." -ForegroundColor Cyan
$buildResult = dotnet build --configuration Release
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host $buildResult
}

# Check if deployment artifacts are generated
if (Test-Path "bin\Release\net8.0\CodeNex.dll") {
    Write-Host "✅ Release build artifacts found" -ForegroundColor Green
} else {
    Write-Host "❌ Release build artifacts missing!" -ForegroundColor Red
}

Write-Host "`n🌐 Azure Web App Information:" -ForegroundColor Cyan
Write-Host "  URL: https://codenex.azurewebsites.net"
Write-Host "  Health Check: https://codenex.azurewebsites.net/health"
Write-Host "  Swagger UI: https://codenex.azurewebsites.net/swagger"

Write-Host "`n📝 Deployment Checklist:" -ForegroundColor Cyan
Write-Host "  1. Commit all changes:       git add . && git commit -m 'Updated deployment config'"
Write-Host "  2. Push to master:           git push origin master"
Write-Host "  3. Monitor deployment:       https://github.com/ayyaz081/Codenex/actions"
Write-Host "  4. Check Azure health:       https://codenex.azurewebsites.net/health"
Write-Host "  5. Monitor Azure logs:       Azure Portal → App Service → codenex → Monitoring → Log stream"
