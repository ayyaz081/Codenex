# Portfolio Backend Windows Deployment Script with SSL Support
# Usage: .\deploy-windows.ps1 -Environment "Production" -Domain "yourdomain.com" -Email "admin@yourdomain.com"

param(
    [string]$Environment = "Production",
    [string]$Domain = "localhost",
    [string]$Email = "admin@localhost",
    [string]$InstallPath = "C:\inetpub\portfolio-backend",
    [string]$SiteName = "PortfolioBackend",
    [switch]$UseIIS = $true,
    [switch]$UseSelfHosted = $false
)

Write-Host "=== Portfolio Backend Windows Deployment ===" -ForegroundColor Green
Write-Host "Environment: $Environment"
Write-Host "Domain: $Domain"
Write-Host "Email: $Email"
Write-Host "Install Path: $InstallPath"

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Error "This script must be run as Administrator. Exiting..."
    exit 1
}

# Create installation directories
Write-Host "Creating directories..." -ForegroundColor Yellow
$Directories = @(
    $InstallPath,
    "$InstallPath\ssl",
    "$InstallPath\data",
    "$InstallPath\uploads",
    "$InstallPath\logs"
)

foreach ($Dir in $Directories) {
    if (!(Test-Path $Dir)) {
        New-Item -ItemType Directory -Path $Dir -Force | Out-Null
        Write-Host "Created directory: $Dir" -ForegroundColor Gray
    }
}

# Install .NET 8 Hosting Bundle if not present
$DotNetPath = "${env:ProgramFiles}\dotnet\dotnet.exe"
if (!(Test-Path $DotNetPath)) {
    Write-Host "Installing .NET 8 Hosting Bundle..." -ForegroundColor Yellow
    $DownloadUrl = "https://download.microsoft.com/download/8/4/8/848f28ae-78c9-4304-ba4c-8dde29d82881/dotnet-hosting-8.0.0-win.exe"
    $OutputPath = "$env:TEMP\dotnet-hosting-8.0.0-win.exe"
    
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $OutputPath
    Start-Process -FilePath $OutputPath -ArgumentList "/install", "/quiet" -Wait
    Remove-Item $OutputPath -Force
    
    Write-Host ".NET 8 Hosting Bundle installed" -ForegroundColor Green
}

# Copy application files
Write-Host "Copying application files..." -ForegroundColor Yellow
Copy-Item -Path ".\*" -Destination $InstallPath -Recurse -Force -Exclude @("bin", "obj", "*.user", "*.cache")

# Generate or configure SSL certificate
Write-Host "Setting up SSL certificate..." -ForegroundColor Yellow
$SslPath = "$InstallPath\ssl"

if ($Environment -eq "Production" -and $Domain -ne "localhost") {
    Write-Host "For production with real domain, you'll need to obtain a proper SSL certificate" -ForegroundColor Yellow
    Write-Host "Options:" -ForegroundColor Gray
    Write-Host "1. Use Let's Encrypt with win-acme: https://www.win-acme.com/" -ForegroundColor Gray
    Write-Host "2. Purchase a certificate from a CA" -ForegroundColor Gray
    Write-Host "3. Use Azure Key Vault or similar service" -ForegroundColor Gray
    Write-Host ""
    
    # For now, create a self-signed certificate
    Write-Host "Creating self-signed certificate for testing..." -ForegroundColor Yellow
}

# Create self-signed certificate
$CertName = $Domain
$CertPath = "$SslPath\certificate.pfx"
$CertPassword = ""

if (!(Test-Path $CertPath)) {
    try {
        # Create self-signed certificate
        $Cert = New-SelfSignedCertificate -DnsName $CertName -CertStoreLocation "cert:\LocalMachine\My" -KeySpec KeyExchange -NotAfter (Get-Date).AddYears(2)
        
        # Export to PFX
        $SecurePassword = ConvertTo-SecureString -String $CertPassword -AsPlainText -Force
        Export-PfxCertificate -Cert $Cert -FilePath $CertPath -Password $SecurePassword | Out-Null
        
        # Export public certificate
        Export-Certificate -Cert $Cert -FilePath "$SslPath\certificate.crt" | Out-Null
        
        Write-Host "Self-signed certificate created: $CertPath" -ForegroundColor Green
        Write-Host "Certificate thumbprint: $($Cert.Thumbprint)" -ForegroundColor Gray
        
        # Store thumbprint for later use
        $CertThumbprint = $Cert.Thumbprint
    }
    catch {
        Write-Error "Failed to create SSL certificate: $($_.Exception.Message)"
        exit 1
    }
}

if ($UseIIS) {
    # Install IIS and ASP.NET Core Hosting Bundle
    Write-Host "Setting up IIS..." -ForegroundColor Yellow
    
    $Features = @(
        "IIS-WebServerRole",
        "IIS-WebServer",
        "IIS-CommonHttpFeatures",
        "IIS-HttpErrors",
        "IIS-HttpRedirect",
        "IIS-ApplicationDevelopment",
        "IIS-NetFxExtensibility45",
        "IIS-HealthAndDiagnostics",
        "IIS-HttpLogging",
        "IIS-Security",
        "IIS-RequestFiltering",
        "IIS-Performance",
        "IIS-WebServerManagementTools",
        "IIS-ManagementConsole",
        "IIS-IIS6ManagementCompatibility",
        "IIS-Metabase",
        "IIS-ASPNET45"
    )
    
    foreach ($Feature in $Features) {
        Enable-WindowsOptionalFeature -Online -FeatureName $Feature -All -NoRestart | Out-Null
    }
    
    # Import WebAdministration module
    Import-Module WebAdministration
    
    # Create Application Pool
    $AppPoolName = "PortfolioBackendPool"
    if (Get-IISAppPool -Name $AppPoolName -ErrorAction SilentlyContinue) {
        Remove-IISAppPool -Name $AppPoolName -Confirm:$false
    }
    
    New-IISAppPool -Name $AppPoolName
    Set-IISAppPoolProcessModel -Name $AppPoolName -IdentityType ApplicationPoolIdentity
    Set-IISAppPool -Name $AppPoolName -ManagedRuntimeVersion ""
    Set-IISAppPool -Name $AppPoolName -ProcessModel.IdleTimeout "00:00:00"
    Set-IISAppPool -Name $AppPoolName -Recycling.PeriodicRestart.Time "00:00:00"
    
    Write-Host "Created application pool: $AppPoolName" -ForegroundColor Green
    
    # Create IIS Site
    if (Get-IISSite -Name $SiteName -ErrorAction SilentlyContinue) {
        Remove-IISSite -Name $SiteName -Confirm:$false
    }
    
    New-IISSite -Name $SiteName -PhysicalPath $InstallPath -Port 80
    Set-IISSite -Name $SiteName -ApplicationPool $AppPoolName
    
    # Configure HTTPS binding if we have a certificate
    if ($CertThumbprint) {
        # Remove existing HTTPS binding if any
        Get-IISSiteBinding -Name $SiteName | Where-Object { $_.Protocol -eq "https" } | Remove-IISSiteBinding -Confirm:$false
        
        # Add HTTPS binding
        New-IISSiteBinding -Name $SiteName -Protocol https -Port 443 -CertificateThumbPrint $CertThumbprint -CertStoreLocation "Cert:\LocalMachine\My"
        Write-Host "Added HTTPS binding with certificate" -ForegroundColor Green
    }
    
    Write-Host "Created IIS site: $SiteName" -ForegroundColor Green
    
    # Set permissions on the application directory
    $Acl = Get-Acl $InstallPath
    $AccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $Acl.SetAccessRule($AccessRule)
    Set-Acl -Path $InstallPath -AclObject $Acl
    
    Write-Host "Set IIS_IUSRS permissions on $InstallPath" -ForegroundColor Green
}

if ($UseSelfHosted) {
    # Create Windows Service for self-hosted deployment
    Write-Host "Setting up Windows Service..." -ForegroundColor Yellow
    
    $ServiceName = "PortfolioBackend"
    $ServicePath = "$InstallPath\PortfolioBackend.exe"
    $ServiceDescription = "Portfolio Backend ASP.NET Core Application"
    
    # Stop and remove existing service if it exists
    $ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($ExistingService) {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 3
        & sc.exe delete $ServiceName
        Write-Host "Removed existing service: $ServiceName" -ForegroundColor Gray
    }
    
    # Create new service
    & sc.exe create $ServiceName binPath= "`"$ServicePath`"" start= auto DisplayName= "`"$ServiceDescription`""
    & sc.exe description $ServiceName "$ServiceDescription with SSL support"
    
    # Set service to restart on failure
    & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/5000/restart/5000
    
    # Set environment variables for the service
    $RegPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
    $EnvVars = @(
        "ASPNETCORE_ENVIRONMENT=$Environment",
        "ASPNETCORE_URLS=https://+:443;http://+:80"
    )
    Set-ItemProperty -Path $RegPath -Name "Environment" -Value $EnvVars -Type MultiString
    
    # Start the service
    Start-Service -Name $ServiceName
    Write-Host "Created and started Windows Service: $ServiceName" -ForegroundColor Green
}

# Configure Windows Firewall
Write-Host "Configuring Windows Firewall..." -ForegroundColor Yellow
try {
    New-NetFirewallRule -DisplayName "HTTP Inbound" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow -ErrorAction SilentlyContinue
    New-NetFirewallRule -DisplayName "HTTPS Inbound" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow -ErrorAction SilentlyContinue
    Write-Host "Configured firewall rules for HTTP/HTTPS" -ForegroundColor Green
}
catch {
    Write-Warning "Could not configure firewall rules: $($_.Exception.Message)"
}

# Display deployment summary
Write-Host ""
Write-Host "=== Deployment Complete! ===" -ForegroundColor Green
Write-Host "Application Path: $InstallPath" -ForegroundColor Gray
Write-Host "SSL Certificate: $InstallPath\ssl" -ForegroundColor Gray
Write-Host "Environment: $Environment" -ForegroundColor Gray

if ($UseIIS) {
    Write-Host "IIS Site: $SiteName" -ForegroundColor Gray
    Write-Host "Application Pool: $AppPoolName" -ForegroundColor Gray
}

if ($UseSelfHosted) {
    Write-Host "Windows Service: PortfolioBackend" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Your application should be available at:" -ForegroundColor Yellow
Write-Host "  https://$Domain" -ForegroundColor White
if ($Domain -eq "localhost") {
    Write-Host "  (Note: Self-signed certificate will show security warnings)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Update appsettings.Production.json with your configuration" -ForegroundColor Gray
Write-Host "2. Configure your domain's DNS to point to this server" -ForegroundColor Gray
Write-Host "3. For production, obtain a proper SSL certificate" -ForegroundColor Gray
Write-Host "4. Test the application and SSL certificate" -ForegroundColor Gray

if ($UseIIS) {
    Write-Host ""
    Write-Host "IIS Management:" -ForegroundColor Yellow
    Write-Host "- Use IIS Manager to further configure the site" -ForegroundColor Gray
    Write-Host "- Check application logs in Event Viewer" -ForegroundColor Gray
}

if ($UseSelfHosted) {
    Write-Host ""
    Write-Host "Service Management:" -ForegroundColor Yellow
    Write-Host "- Use 'services.msc' to manage the service" -ForegroundColor Gray
    Write-Host "- Check service logs in Event Viewer" -ForegroundColor Gray
    Write-Host "- Restart: Restart-Service PortfolioBackend" -ForegroundColor Gray
}
