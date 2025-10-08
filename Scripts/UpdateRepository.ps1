# PowerShell script to update repository settings in CodeNex
# Usage: Update the variables below and run this script

param(
    [int]$RepositoryId = 1,
    [decimal]$Price = 29.99,
    [string]$GitHubRepoFullName = "CodeNex-Premium/your-repo-name",
    [string]$BaseUrl = "http://localhost:7150",
    [string]$AuthToken = ""
)

Write-Host "CodeNex Repository Update Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check if auth token is provided
if ([string]::IsNullOrEmpty($AuthToken)) {
    Write-Host "ERROR: No authorization token provided!" -ForegroundColor Red
    Write-Host "You need to:" -ForegroundColor Yellow
    Write-Host "  1. Log in to your application as admin" -ForegroundColor Yellow
    Write-Host "  2. Open browser DevTools (F12)" -ForegroundColor Yellow
    Write-Host "  3. Go to Application/Storage > Local Storage" -ForegroundColor Yellow
    Write-Host "  4. Copy the 'token' value" -ForegroundColor Yellow
    Write-Host "  5. Run this script with -AuthToken parameter" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Example:" -ForegroundColor Green
    Write-Host "  .\UpdateRepository.ps1 -RepositoryId 1 -Price 29.99 -GitHubRepoFullName 'YourOrg/repo-name' -AuthToken 'your_token_here'" -ForegroundColor Green
    exit 1
}

# 1. First, let's get the repository details
Write-Host "Step 1: Fetching repository $RepositoryId..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/repository/$RepositoryId" -Method Get -ErrorAction Stop
    Write-Host "SUCCESS - Current Repository Data:" -ForegroundColor Green
    Write-Host "  Title: $($response.title)" -ForegroundColor White
    Write-Host "  IsPremium: $($response.isPremium)" -ForegroundColor White
    Write-Host "  IsFree: $($response.isFree)" -ForegroundColor White
    Write-Host "  Price: $($response.price)" -ForegroundColor White
    Write-Host "  GitHubRepoFullName: $($response.gitHubRepoFullName)" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "ERROR - Failed to fetch repository: $_" -ForegroundColor Red
    Write-Host "Make sure the repository exists and the API is running at $BaseUrl" -ForegroundColor Yellow
    exit 1
}

# 2. Update the repository
Write-Host "Step 2: Updating repository settings..." -ForegroundColor Yellow
Write-Host "  Setting IsPremium: true" -ForegroundColor White
Write-Host "  Setting IsFree: false" -ForegroundColor White
Write-Host "  Setting Price: $Price" -ForegroundColor White
Write-Host "  Setting GitHubRepoFullName: $GitHubRepoFullName" -ForegroundColor White
Write-Host ""

$headers = @{
    "Authorization" = "Bearer $AuthToken"
    "Content-Type" = "application/json"
}

$body = @{
    IsPremium = $true
    IsFree = $false
    Price = $Price
    GitHubRepoFullName = $GitHubRepoFullName
} | ConvertTo-Json

try {
    $updateResponse = Invoke-RestMethod -Uri "$BaseUrl/api/repository/$RepositoryId" `
        -Method Put `
        -Headers $headers `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "SUCCESS - Repository updated successfully!" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR - Failed to update repository: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error Details: $errorBody" -ForegroundColor Red
    }
    exit 1
}

# 3. Verify the update
Write-Host "Step 3: Verifying the update..." -ForegroundColor Yellow
try {
    $verifyResponse = Invoke-RestMethod -Uri "$BaseUrl/api/repository/$RepositoryId" -Method Get -ErrorAction Stop
    Write-Host "SUCCESS - Updated Repository Data:" -ForegroundColor Green
    Write-Host "  Title: $($verifyResponse.title)" -ForegroundColor White
    Write-Host "  IsPremium: $($verifyResponse.isPremium)" -ForegroundColor White
    Write-Host "  IsFree: $($verifyResponse.isFree)" -ForegroundColor White
    Write-Host "  Price: $($verifyResponse.price)" -ForegroundColor White
    Write-Host "  GitHubRepoFullName: $($verifyResponse.gitHubRepoFullName)" -ForegroundColor White
    Write-Host ""
    
    # Check if the update was successful
    if ($verifyResponse.isPremium -eq $true -and $verifyResponse.price -eq $Price) {
        Write-Host "SUCCESS! Repository is now properly configured as premium." -ForegroundColor Green
    } else {
        Write-Host "WARNING: Update may not have taken effect. Please check manually." -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR - Failed to verify update: $_" -ForegroundColor Red
}
