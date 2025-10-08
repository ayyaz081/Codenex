# Interactive fix for Repository ID 5
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Fix Repository ID 5 - Interactive" -ForegroundColor Cyan  
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "STEP 1: Get your admin token" -ForegroundColor Yellow
Write-Host "  1. Make sure you're logged in to your app as admin" -ForegroundColor White
Write-Host "  2. Press F12 in your browser" -ForegroundColor White
Write-Host "  3. Go to Console tab" -ForegroundColor White
Write-Host "  4. Paste this: " -ForegroundColor White
Write-Host "     console.log(localStorage.getItem('token'));" -ForegroundColor Cyan
Write-Host "  5. Copy the token (long string starting with 'eyJ...')" -ForegroundColor White
Write-Host ""

$token = Read-Host "Paste your token here"

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "[ERROR] No token provided. Exiting." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "STEP 2: Configure repository settings" -ForegroundColor Yellow
Write-Host ""

$price = Read-Host "Enter price (e.g., 29.99) [default: 29.99]"
if ([string]::IsNullOrWhiteSpace($price)) {
    $price = "29.99"
}

$githubRepo = Read-Host "Enter GitHub repo (format: Org/repo-name) [default: CodeNex-Premium/test-repo]"
if ([string]::IsNullOrWhiteSpace($githubRepo)) {
    $githubRepo = "CodeNex-Premium/test-repo"
}

Write-Host ""
Write-Host "STEP 3: Updating repository..." -ForegroundColor Yellow
Write-Host "  Repository ID: 5" -ForegroundColor White
Write-Host "  Price: `$$price" -ForegroundColor White
Write-Host "  GitHub Repo: $githubRepo" -ForegroundColor White
Write-Host ""

try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    $body = @{
        IsPremium = $true
        IsFree = $false
        Price = [decimal]$price
        GitHubRepoFullName = $githubRepo
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:7150/api/repository/5" `
        -Method Put `
        -Headers $headers `
        -Body $body `
        -ErrorAction Stop

    Write-Host ""
    Write-Host "[SUCCESS] Repository updated successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Now try purchasing the repository again in your browser!" -ForegroundColor Cyan
    Write-Host ""
    
    # Verify
    Write-Host "Verifying update..." -ForegroundColor Yellow
    $verify = Invoke-RestMethod -Uri "http://localhost:7150/api/repository/5" -Method Get
    Write-Host "  Title: $($verify.title)" -ForegroundColor White
    Write-Host "  IsPremium: $($verify.isPremium)" -ForegroundColor Green
    Write-Host "  Price: `$$($verify.price)" -ForegroundColor Green
    Write-Host "  GitHubRepoFullName: $($verify.gitHubRepoFullName)" -ForegroundColor Green
    Write-Host ""
    Write-Host "[DONE] All set! The purchase should work now." -ForegroundColor Green
    
} catch {
    Write-Host ""
    Write-Host "[ERROR] Failed to update repository" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $errorBody = $reader.ReadToEnd()
        Write-Host "Details: $errorBody" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Possible issues:" -ForegroundColor Yellow
    Write-Host "  - Token is expired or invalid (try getting a new one)" -ForegroundColor Yellow
    Write-Host "  - You're not logged in as Admin" -ForegroundColor Yellow
    Write-Host "  - API is not running on http://localhost:7150" -ForegroundColor Yellow
}
