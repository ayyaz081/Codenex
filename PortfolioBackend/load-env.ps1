# Load environment variables from .env file
# Usage: . .\load-env.ps1  (note the dot at the beginning)

$envFile = ".env"

if (Test-Path $envFile) {
    Write-Host "Loading environment variables from $envFile..." -ForegroundColor Green
    
    Get-Content $envFile | Where-Object { 
        $_ -and (-not $_.StartsWith("#")) -and $_.Contains("=") 
    } | ForEach-Object {
        $parts = $_.Split("=", 2)
        if ($parts.Length -eq 2) {
            $key = $parts[0].Trim()
            $value = $parts[1].Trim()
            
            # Remove quotes if present
            if (($value.StartsWith('"') -and $value.EndsWith('"')) -or 
                ($value.StartsWith("'") -and $value.EndsWith("'"))) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            
            [Environment]::SetEnvironmentVariable($key, $value, [EnvironmentVariableTarget]::Process)
            Write-Host "Set $key" -ForegroundColor Yellow
        }
    }
    
    Write-Host "Environment variables loaded successfully!" -ForegroundColor Green
    Write-Host "You can now run: dotnet run" -ForegroundColor Cyan
} else {
    Write-Warning ".env file not found. Please create one from .env.example and fill in your values."
}
