param(
    [string]$LocalPath = "publish",
    [string]$FtpServer = "ftp://win1017.site4now.net:21/neelsol",
    [string]$FtpUser = "neelsol-002",
    [string]$FtpPass = "Training!123"
)

function Upload-FtpFile {
    param($LocalFile, $RemotePath, $User, $Pass)
    
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create($RemotePath)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($User, $Pass)
        $ftpRequest.UseBinary = $true
        $ftpRequest.UsePassive = $true
        
        $content = [System.IO.File]::ReadAllBytes($LocalFile)
        $ftpRequest.ContentLength = $content.Length
        
        $requestStream = $ftpRequest.GetRequestStream()
        $requestStream.Write($content, 0, $content.Length)
        $requestStream.Close()
        
        $response = $ftpRequest.GetResponse()
        $response.Close()
        
        return $true
    }
    catch {
        Write-Host "Error uploading $LocalFile : $_" -ForegroundColor Red
        return $false
    }
}

function Create-FtpDirectory {
    param($RemotePath, $User, $Pass)
    
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create($RemotePath)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($User, $Pass)
        $ftpRequest.UsePassive = $true
        
        $response = $ftpRequest.GetResponse()
        $response.Close()
        return $true
    }
    catch {
        # Directory might already exist, that's okay
        return $false
    }
}

Write-Host "Starting FTP deployment..." -ForegroundColor Green
Write-Host "Local path: $LocalPath"
Write-Host "FTP server: $FtpServer"

$files = Get-ChildItem -Path $LocalPath -Recurse -File
$totalFiles = $files.Count
$uploaded = 0
$failed = 0

Write-Host "Found $totalFiles files to upload" -ForegroundColor Cyan

$createdDirs = @{}

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring((Get-Item $LocalPath).FullName.Length + 1)
    $remotePath = "$FtpServer/$($relativePath.Replace('\', '/'))"
    
    # Create directory if needed
    $remoteDir = Split-Path $remotePath -Parent
    if ($remoteDir -and !$createdDirs.ContainsKey($remoteDir)) {
        Create-FtpDirectory -RemotePath $remoteDir -User $FtpUser -Pass $FtpPass | Out-Null
        $createdDirs[$remoteDir] = $true
    }
    
    if (Upload-FtpFile -LocalFile $file.FullName -RemotePath $remotePath -User $FtpUser -Pass $FtpPass) {
        $uploaded++
        if ($uploaded % 10 -eq 0) {
            Write-Host "Uploaded $uploaded/$totalFiles files..." -ForegroundColor Yellow
        }
    }
    else {
        $failed++
    }
}

Write-Host "`nDeployment complete!" -ForegroundColor Green
Write-Host "Uploaded: $uploaded files" -ForegroundColor Green
if ($failed -gt 0) {
    Write-Host "Failed: $failed files" -ForegroundColor Red
}
