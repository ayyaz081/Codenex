# Portfolio Backend - Admin User Setup Script
# This script helps you configure admin user environment variables for production

param(
    [Parameter(Mandatory=$false)]
    [string]$Email,
    
    [Parameter(Mandatory=$false)]
    [string]$Password,
    
    [Parameter(Mandatory=$false)]
    [string]$FirstName = "Admin",
    
    [Parameter(Mandatory=$false)]
    [string]$LastName = "User",
    
    [Parameter(Mandatory=$false)]
    [switch]$GeneratePassword = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$ShowEnvVars = $false
)

Write-Host "Portfolio Backend - Admin User Setup" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Function to generate a secure password
function Generate-SecurePassword {
    $length = 16
    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*"
    $password = ""
    for ($i = 0; $i -lt $length; $i++) {
        $password += $chars[(Get-Random -Maximum $chars.Length)]
    }
    return $password
}

# Function to validate email
function Test-Email {
    param([string]$EmailAddress)
    
    try {
        $null = [mailaddress]$EmailAddress
        return $true
    }
    catch {
        return $false
    }
}

# Function to validate password strength
function Test-PasswordStrength {
    param([string]$Password)
    
    if ($Password.Length -lt 8) {
        return $false, "Password must be at least 8 characters long"
    }
    
    if (-not ($Password -cmatch "[A-Z]")) {
        return $false, "Password must contain uppercase letters"
    }
    
    if (-not ($Password -cmatch "[a-z]")) {
        return $false, "Password must contain lowercase letters"
    }
    
    if (-not ($Password -match "\d")) {
        return $false, "Password must contain digits"
    }
    
    return $true, "Password meets requirements"
}

# Show current environment variables if requested
if ($ShowEnvVars) {
    Write-Host ""
    Write-Host "Current Admin Environment Variables:" -ForegroundColor Yellow
    Write-Host "ADMIN_EMAIL: $([Environment]::GetEnvironmentVariable('ADMIN_EMAIL'))"
    Write-Host "ADMIN_PASSWORD: $(if([Environment]::GetEnvironmentVariable('ADMIN_PASSWORD')) { '***CONFIGURED***' } else { 'NOT SET' })"
    Write-Host "ADMIN_FIRST_NAME: $([Environment]::GetEnvironmentVariable('ADMIN_FIRST_NAME'))"
    Write-Host "ADMIN_LAST_NAME: $([Environment]::GetEnvironmentVariable('ADMIN_LAST_NAME'))"
    Write-Host ""
    return
}

# Get admin email
if (-not $Email) {
    $Email = Read-Host "Enter admin email address"
}

if (-not (Test-Email $Email)) {
    Write-Error "Invalid email address format: $Email"
    exit 1
}

# Generate or get password
if ($GeneratePassword) {
    $Password = Generate-SecurePassword
    Write-Host "Generated secure password: $Password" -ForegroundColor Green
    Write-Host "IMPORTANT: Save this password securely!" -ForegroundColor Yellow
} elseif (-not $Password) {
    $SecurePassword = Read-Host "Enter admin password" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Validate password strength
$isValid, $message = Test-PasswordStrength $Password
if (-not $isValid) {
    Write-Error "Password validation failed: $message"
    Write-Host "Password requirements:" -ForegroundColor Yellow
    Write-Host "- Minimum 8 characters" -ForegroundColor White
    Write-Host "- Must contain uppercase letters" -ForegroundColor White
    Write-Host "- Must contain lowercase letters" -ForegroundColor White
    Write-Host "- Must contain digits" -ForegroundColor White
    Write-Host "- Special characters recommended (!@#$%^&*)" -ForegroundColor White
    exit 1
}

Write-Host "Password validation: $message" -ForegroundColor Green

# Display configuration
Write-Host ""
Write-Host "Admin User Configuration:" -ForegroundColor Green
Write-Host "Email: $Email"
Write-Host "First Name: $FirstName"
Write-Host "Last Name: $LastName"
Write-Host "Password: ***CONFIGURED***"

# Ask for confirmation
$confirm = Read-Host "`nDo you want to set these environment variables? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Configuration cancelled." -ForegroundColor Yellow
    exit 0
}

# Set environment variables
try {
    [Environment]::SetEnvironmentVariable("ADMIN_EMAIL", $Email, [EnvironmentVariableTarget]::User)
    [Environment]::SetEnvironmentVariable("ADMIN_PASSWORD", $Password, [EnvironmentVariableTarget]::User)
    [Environment]::SetEnvironmentVariable("ADMIN_FIRST_NAME", $FirstName, [EnvironmentVariableTarget]::User)
    [Environment]::SetEnvironmentVariable("ADMIN_LAST_NAME", $LastName, [EnvironmentVariableTarget]::User)
    
    Write-Host ""
    Write-Host "✅ Environment variables set successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To use these in your application:" -ForegroundColor Yellow
    Write-Host "1. Restart your terminal/IDE to pick up the new environment variables"
    Write-Host "2. Run your application - admin user will be created automatically"
    Write-Host "3. Check logs for admin creation status"
    Write-Host "4. Verify with: curl https://yourdomain.com/health/admin"
    
} catch {
    Write-Error "Failed to set environment variables: $_"
    exit 1
}

# Show .env format for production deployment
Write-Host ""
Write-Host "For production deployment (.env file format):" -ForegroundColor Cyan
Write-Host "ADMIN_EMAIL=$Email"
Write-Host "ADMIN_PASSWORD=$Password"
Write-Host "ADMIN_FIRST_NAME=$FirstName"
Write-Host "ADMIN_LAST_NAME=$LastName"

Write-Host ""
Write-Host "🔒 SECURITY REMINDER:" -ForegroundColor Red
Write-Host "- Never commit admin credentials to source control"
Write-Host "- Use secure password managers for production passwords"
Write-Host "- Consider rotating admin password regularly"
Write-Host "- Monitor admin user access logs"
