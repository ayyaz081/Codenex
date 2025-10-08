# Simple script to check repository configuration
# This doesn't require authentication

param(
    [string]$BaseUrl = "http://localhost:7150"
)

Write-Host "CodeNex Repository Checker" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan
Write-Host ""

# Check if API is running
Write-Host "Checking if API is running at $BaseUrl..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-RestMethod -Uri "$BaseUrl/api/repository" -Method Get -TimeoutSec 5 -ErrorAction Stop
    Write-Host "[OK] API is running!" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "[ERROR] Cannot connect to API at $BaseUrl" -ForegroundColor Red
    Write-Host "  Make sure your application is running" -ForegroundColor Yellow
    Write-Host "  Run: dotnet run" -ForegroundColor Yellow
    exit 1
}

# Get all repositories
Write-Host "Fetching all repositories..." -ForegroundColor Yellow
try {
    $repositories = Invoke-RestMethod -Uri "$BaseUrl/api/repository" -Method Get -ErrorAction Stop
    
    if ($repositories.Count -eq 0) {
        Write-Host "[WARNING] No repositories found in the database" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Found $($repositories.Count) repository/repositories" -ForegroundColor Green
    Write-Host ""
    Write-Host ("=" * 100) -ForegroundColor Gray
    
    foreach ($repo in $repositories) {
        Write-Host ""
        Write-Host "Repository ID: $($repo.id)" -ForegroundColor Cyan
        Write-Host "Title: $($repo.title)" -ForegroundColor White
        Write-Host "IsPremium: $($repo.isPremium)" -ForegroundColor $(if ($repo.isPremium) { "Green" } else { "Gray" })
        Write-Host "IsFree: $($repo.isFree)" -ForegroundColor $(if ($repo.isFree) { "Green" } else { "Gray" })
        Write-Host "Price: $($repo.price)" -ForegroundColor $(if ($repo.price -and $repo.price -gt 0) { "Green" } else { "Red" })
        Write-Host "GitHubRepoFullName: $($repo.gitHubRepoFullName)" -ForegroundColor $(if ($repo.gitHubRepoFullName) { "Green" } else { "Red" })
        Write-Host "IsActive: $($repo.isActive)" -ForegroundColor $(if ($repo.isActive) { "Green" } else { "Red" })
        
        # Check if premium repo is properly configured
        if ($repo.isPremium) {
            Write-Host ""
            $issues = @()
            
            if (-not $repo.price -or $repo.price -le 0) {
                $issues += "Price is not set or is 0"
            }
            
            if (-not $repo.gitHubRepoFullName) {
                $issues += "GitHubRepoFullName is not set"
            }
            
            if ($issues.Count -gt 0) {
                Write-Host "[WARNING] ISSUES FOUND:" -ForegroundColor Red
                foreach ($issue in $issues) {
                    Write-Host "  [X] $issue" -ForegroundColor Red
                }
                Write-Host ""
                Write-Host "This repository will NOT work with Stripe checkout!" -ForegroundColor Red
                Write-Host "Fix it by running:" -ForegroundColor Yellow
                Write-Host "  .\UpdateRepository.ps1 -RepositoryId $($repo.id) -Price 29.99 -GitHubRepoFullName 'YourOrg/repo-name' -AuthToken 'your_token'" -ForegroundColor Green
            } else {
                Write-Host "[SUCCESS] This premium repository is properly configured!" -ForegroundColor Green
            }
        }
        
        Write-Host ""
        Write-Host ("=" * 100) -ForegroundColor Gray
    }
    
    # Summary
    Write-Host ""
    Write-Host "SUMMARY:" -ForegroundColor Cyan
    $premiumCount = ($repositories | Where-Object { $_.isPremium }).Count
    $freeCount = ($repositories | Where-Object { $_.isFree }).Count
    $properlyConfiguredPremium = ($repositories | Where-Object { 
        $_.isPremium -and $_.price -gt 0 -and $_.gitHubRepoFullName 
    }).Count
    $brokenPremium = $premiumCount - $properlyConfiguredPremium
    
    Write-Host "  Total Repositories: $($repositories.Count)" -ForegroundColor White
    Write-Host "  Premium Repositories: $premiumCount" -ForegroundColor White
    Write-Host "  Free Repositories: $freeCount" -ForegroundColor White
    Write-Host "  Properly Configured Premium: $properlyConfiguredPremium" -ForegroundColor $(if ($properlyConfiguredPremium -eq $premiumCount) { "Green" } else { "Yellow" })
    
    if ($brokenPremium -gt 0) {
        Write-Host "  [WARNING] Broken Premium: $brokenPremium" -ForegroundColor Red
        Write-Host ""
        Write-Host "ACTION REQUIRED: Fix the broken premium repositories!" -ForegroundColor Red
        Write-Host "See: FIX_PREMIUM_REPO_GUIDE.md for instructions" -ForegroundColor Yellow
    } else {
        Write-Host ""
        Write-Host "[SUCCESS] All repositories are properly configured!" -ForegroundColor Green
    }
    
} catch {
    Write-Host "[ERROR] Error fetching repositories" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
