# Portfolio Backend - Same Domain Setup Script
# Quick setup for hosting frontend and backend on the same domain

param(
    [Parameter(Mandatory=$true)]
    [string]$Domain,
    
    [Parameter(Mandatory=$true)]
    [string]$AdminEmail,
    
    [Parameter(Mandatory=$true)]
    [string]$AdminPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$AdminFirstName = "Admin",
    
    [Parameter(Mandatory=$false)]
    [string]$AdminLastName = "User",
    
    [Parameter(Mandatory=$false)]
    [switch]$Development = $false
)

Write-Host "Portfolio Backend - Same Domain Setup" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Validate domain format
if ($Domain -notmatch "^https?://") {
    $Domain = "https://$Domain"
}

# For development, use localhost
if ($Development) {
    $Domain = "https://localhost:7151"
    Write-Host "Development mode: Using localhost" -ForegroundColor Yellow
}

Write-Host "Setting up same-domain hosting for: $Domain" -ForegroundColor Green

# Generate JWT key
$bytes = New-Object byte[] 32
(New-Object Random).NextBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)

Write-Host "Generated JWT Key: $jwtKey" -ForegroundColor Yellow

# Create .env file content
$envContent = @"
# ===========================================
# SAME DOMAIN HOSTING - FRONTEND + BACKEND TOGETHER
# Generated on $(Get-Date)
# ===========================================

# 🔐 SECURITY
JWT_KEY=$jwtKey
ADMIN_EMAIL=$AdminEmail
ADMIN_PASSWORD=$AdminPassword
ADMIN_FIRST_NAME=$AdminFirstName
ADMIN_LAST_NAME=$AdminLastName

# 🌐 SAME DOMAIN SETUP
FRONTEND_BASE_URL=$Domain
API_BASE_URL=$Domain/api
JWT_ISSUER=$Domain
JWT_AUDIENCE=$Domain
CORS_ALLOWED_ORIGINS=$Domain

# 📧 EMAIL SETTINGS (Configure these for email features)
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USERNAME=your.email@gmail.com
EMAIL_PASSWORD=your-gmail-app-password
EMAIL_FROM=your.email@gmail.com
EMAIL_FROM_NAME=Your Portfolio
EMAIL_ENABLE_SSL=true

# 🗄️ DATABASE
CONNECTION_STRING=Data Source=./wwwroot/App_Data/PortfolioDB.sqlite
DATABASE_PROVIDER=sqlite

# 📧 EMAIL PATHS
EMAIL_VERIFICATION_PATH=/auth/verify
PASSWORD_RESET_PATH=/auth/reset

# 🔒 SECURITY SETTINGS
REQUIRE_HTTPS=true
USE_HSTS=true
REQUIRE_EMAIL_CONFIRMATION=false
"@

# Write to .env file
$envPath = Join-Path $PSScriptRoot ".env"
$envContent | Out-File -FilePath $envPath -Encoding UTF8

Write-Host ""
Write-Host "✅ Environment configuration created!" -ForegroundColor Green
Write-Host "📁 File: $envPath" -ForegroundColor White

# Display configuration summary
Write-Host ""
Write-Host "📋 Configuration Summary:" -ForegroundColor Cyan
Write-Host "Domain: $Domain" -ForegroundColor White
Write-Host "API Endpoints: $Domain/api/*" -ForegroundColor White
Write-Host "Admin Email: $AdminEmail" -ForegroundColor White
Write-Host "Frontend: $Domain/" -ForegroundColor White
Write-Host "Backend: $Domain/api/" -ForegroundColor White

Write-Host ""
Write-Host "🚀 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Configure email settings in .env if you need email features" -ForegroundColor White
Write-Host "2. Test locally: dotnet run" -ForegroundColor White
Write-Host "3. Build your frontend into wwwroot/ folder" -ForegroundColor White
Write-Host "4. Deploy to your hosting service" -ForegroundColor White

if ($Development) {
    Write-Host ""
    Write-Host "🧪 Development Mode:" -ForegroundColor Yellow
    Write-Host "- Backend will run on: https://localhost:7151" -ForegroundColor White
    Write-Host "- API endpoints: https://localhost:7151/api/*" -ForegroundColor White
    Write-Host "- Health check: https://localhost:7151/health" -ForegroundColor White
}

Write-Host ""
Write-Host "💡 Tips:" -ForegroundColor Green
Write-Host "- For Gmail: Enable 2FA and generate app password" -ForegroundColor White
Write-Host "- Your admin user will be created automatically on first run" -ForegroundColor White
Write-Host "- Frontend files go in the wwwroot/ folder" -ForegroundColor White
Write-Host "- Use '/api/' prefix for all API calls from frontend" -ForegroundColor White
