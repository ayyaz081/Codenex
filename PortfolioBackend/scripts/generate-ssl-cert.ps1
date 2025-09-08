# SSL Certificate Generation Script for Windows
# Usage: .\generate-ssl-cert.ps1 -Domain "yourdomain.com" -Output ".\ssl" -Password "your-password"

param(
    [Parameter(Mandatory=$true)]
    [string]$Domain,
    [string]$Output = ".\ssl",
    [string]$Password = "",
    [switch]$SelfSigned = $true,
    [int]$ValidityDays = 365
)

Write-Host "=== SSL Certificate Generation ===" -ForegroundColor Green
Write-Host "Domain: $Domain"
Write-Host "Output Directory: $Output"
Write-Host "Self-Signed: $SelfSigned"
Write-Host "Validity Days: $ValidityDays"

# Create output directory if it doesn't exist
if (!(Test-Path $Output)) {
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
    Write-Host "Created directory: $Output" -ForegroundColor Gray
}

$CertPath = "$Output\certificate.pfx"
$CrtPath = "$Output\certificate.crt"
$KeyPath = "$Output\certificate.key"

if ($SelfSigned) {
    Write-Host "Generating self-signed certificate..." -ForegroundColor Yellow
    
    try {
        # Create self-signed certificate
        $Cert = New-SelfSignedCertificate -DnsName $Domain -CertStoreLocation "cert:\LocalMachine\My" -KeySpec KeyExchange -NotAfter (Get-Date).AddDays($ValidityDays)
        
        # Export to PFX
        $SecurePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force
        Export-PfxCertificate -Cert $Cert -FilePath $CertPath -Password $SecurePassword | Out-Null
        
        # Export public certificate
        Export-Certificate -Cert $Cert -FilePath $CrtPath | Out-Null
        
        Write-Host "Self-signed certificate generated successfully!" -ForegroundColor Green
        Write-Host "Certificate files:" -ForegroundColor Gray
        Write-Host "  PFX: $CertPath" -ForegroundColor Gray
        Write-Host "  CRT: $CrtPath" -ForegroundColor Gray
        Write-Host "  Thumbprint: $($Cert.Thumbprint)" -ForegroundColor Gray
        
        # Clean up certificate store
        Remove-Item -Path "cert:\LocalMachine\My\$($Cert.Thumbprint)" -ErrorAction SilentlyContinue
        
        # Display certificate information
        $CertInfo = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CrtPath)
        Write-Host ""
        Write-Host "Certificate Information:" -ForegroundColor Yellow
        Write-Host "  Subject: $($CertInfo.Subject)" -ForegroundColor Gray
        Write-Host "  Issuer: $($CertInfo.Issuer)" -ForegroundColor Gray
        Write-Host "  Valid From: $($CertInfo.NotBefore)" -ForegroundColor Gray
        Write-Host "  Valid To: $($CertInfo.NotAfter)" -ForegroundColor Gray
        
        return $true
    }
    catch {
        Write-Error "Failed to generate self-signed certificate: $($_.Exception.Message)"
        return $false
    }
}
else {
    Write-Host "Let's Encrypt certificate generation is not supported on Windows via this script." -ForegroundColor Yellow
    Write-Host "Please use one of the following options:" -ForegroundColor Gray
    Write-Host "1. win-acme (https://www.win-acme.com/)" -ForegroundColor Gray
    Write-Host "2. Certbot with manual DNS validation" -ForegroundColor Gray
    Write-Host "3. Purchase a certificate from a Certificate Authority" -ForegroundColor Gray
    return $false
}

Write-Host ""
Write-Host "Certificate generation completed!" -ForegroundColor Green
