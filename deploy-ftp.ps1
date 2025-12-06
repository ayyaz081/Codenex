# FTP Deployment Script for site4now
param(
    [string]$Username = "neelsol-002",
    [string]$Password = "Training!123",
    [string]$FtpServer = "ftp://win1017.site4now.net/neelsol",
    [string]$LocalPath = ".\bin\Release\net8.0\publish"
)

Write-Host "Starting FTP deployment to site4now..." -ForegroundColor Green

# Function to upload file via FTP
function Upload-FtpFile {
    param($LocalFile, $RemotePath)
    
    try {
        $uri = New-Object System.Uri("$FtpServer/$RemotePath")
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
        $request.UseBinary = $true
        $request.UsePassive = $true
        
        $fileContent = [System.IO.File]::ReadAllBytes($LocalFile)
        $request.ContentLength = $fileContent.Length
        
        $requestStream = $request.GetRequestStream()
        $requestStream.Write($fileContent, 0, $fileContent.Length)
        $requestStream.Close()
        
        $response = $request.GetResponse()
        Write-Host "  Uploaded: $RemotePath" -ForegroundColor Gray
        $response.Close()
        return $true
    }
    catch {
        Write-Host "  Failed: $RemotePath - $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to create FTP directory
function Create-FtpDirectory {
    param($RemoteDir)
    
    try {
        $uri = New-Object System.Uri("$FtpServer/$RemoteDir")
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $request.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
        $request.UsePassive = $true
        
        $response = $request.GetResponse()
        $response.Close()
    }
    catch {
        # Directory might already exist, ignore error
    }
}

# Upload files recursively
function Upload-FtpDirectory {
    param($LocalDir, $RemoteDir = "")
    
    # Create remote directory
    if ($RemoteDir -ne "") {
        Create-FtpDirectory $RemoteDir
    }
    
    # Upload files
    Get-ChildItem -Path $LocalDir -File | ForEach-Object {
        $remotePath = if ($RemoteDir -eq "") { $_.Name } else { "$RemoteDir/$($_.Name)" }
        Upload-FtpFile $_.FullName $remotePath
    }
    
    # Upload subdirectories
    Get-ChildItem -Path $LocalDir -Directory | ForEach-Object {
        $remoteSubDir = if ($RemoteDir -eq "") { $_.Name } else { "$RemoteDir/$($_.Name)" }
        Upload-FtpDirectory $_.FullName $remoteSubDir
    }
}

# Main deployment
Write-Host "Publishing path: $LocalPath" -ForegroundColor Cyan
Write-Host "FTP Server: $FtpServer" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $LocalPath) {
    Upload-FtpDirectory $LocalPath
    Write-Host ""
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Error: Publish folder not found at $LocalPath" -ForegroundColor Red
    Write-Host "Please run 'dotnet publish -c Release' first" -ForegroundColor Yellow
}
