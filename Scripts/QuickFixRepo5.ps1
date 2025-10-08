# Quick fix for Repository ID 5 using SQL
# This script will directly update the database

param(
    [int]$RepositoryId = 5,
    [decimal]$Price = 29.99,
    [string]$GitHubRepoFullName = "CodeNex-Premium/test-repo"
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Quick Fix for Repository ID $RepositoryId" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Get connection string from environment or appsettings
$envPath = "C:\Users\Az\source\repos\ayyaz081\Codenex\.env"

if (Test-Path $envPath) {
    Write-Host "Reading connection string from .env file..." -ForegroundColor Yellow
    $envContent = Get-Content $envPath
    $connectionString = $envContent | Where-Object { $_ -match '^ConnectionStrings__DefaultConnection=' } | ForEach-Object {
        $_ -replace '^ConnectionStrings__DefaultConnection=', ''
    }
    
    if ($connectionString) {
        Write-Host "[OK] Found connection string" -ForegroundColor Green
        Write-Host ""
        
        # Create SQL script
        $sql = @"
UPDATE Repositories
SET 
    IsPremium = 1,
    IsFree = 0,
    Price = $Price,
    GitHubRepoFullName = '$GitHubRepoFullName',
    UpdatedAt = GETUTCDATE()
WHERE Id = $RepositoryId;

SELECT 
    Id, 
    Title, 
    IsPremium, 
    IsFree, 
    Price, 
    GitHubRepoFullName,
    UpdatedAt
FROM Repositories
WHERE Id = $RepositoryId;
"@

        Write-Host "Will execute the following SQL:" -ForegroundColor Yellow
        Write-Host $sql -ForegroundColor Gray
        Write-Host ""
        
        # Save to temp file
        $tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
        $sql | Out-File -FilePath $tempSqlFile -Encoding UTF8
        
        Write-Host "Executing SQL via sqlcmd..." -ForegroundColor Yellow
        
        # Try to execute using sqlcmd if available
        try {
            $result = sqlcmd -S "codenex.database.windows.net" -d "codenex" -G -i $tempSqlFile 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "[SUCCESS] Repository updated!" -ForegroundColor Green
                Write-Host ""
                Write-Host "Result:" -ForegroundColor Cyan
                Write-Host $result -ForegroundColor White
            } else {
                Write-Host "[ERROR] Failed to execute SQL" -ForegroundColor Red
                Write-Host $result -ForegroundColor Red
            }
        } catch {
            Write-Host "[WARNING] sqlcmd not available or failed" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Please execute this SQL manually in Azure Data Studio or SSMS:" -ForegroundColor Yellow
            Write-Host ""
            Write-Host $sql -ForegroundColor Cyan
            Write-Host ""
            Write-Host "Or run this file: $tempSqlFile" -ForegroundColor Yellow
        }
        
        # Clean up temp file
        if (Test-Path $tempSqlFile) {
            Remove-Item $tempSqlFile -Force
        }
        
    } else {
        Write-Host "[ERROR] Connection string not found in .env" -ForegroundColor Red
    }
} else {
    Write-Host "[ERROR] .env file not found at $envPath" -ForegroundColor Red
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Alternative: Use the API method" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Log in to your app as admin in the browser" -ForegroundColor Yellow
Write-Host "2. Press F12 -> Application -> Local Storage -> copy 'token'" -ForegroundColor Yellow
Write-Host "3. Run:" -ForegroundColor Yellow
Write-Host "   .\UpdateRepository.ps1 -RepositoryId $RepositoryId -Price $Price -GitHubRepoFullName '$GitHubRepoFullName' -AuthToken 'YOUR_TOKEN'" -ForegroundColor Green
