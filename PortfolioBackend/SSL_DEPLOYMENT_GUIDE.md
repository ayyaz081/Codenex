# SSL/HTTPS Deployment Guide for Portfolio Backend

This guide covers SSL certificate setup and HTTPS deployment for the Portfolio Backend application across different platforms and environments.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Environment Configuration](#environment-configuration)
3. [SSL Certificate Management](#ssl-certificate-management)
4. [Deployment Scenarios](#deployment-scenarios)
5. [Troubleshooting](#troubleshooting)
6. [Security Best Practices](#security-best-practices)

## Quick Start

### Development (Local)
```bash
# Use development certificates (self-signed)
dotnet dev-certs https --trust
dotnet run --environment Development
```
Access: `https://localhost:7151` or `http://localhost:7150`

### Production (Docker)
```bash
# Copy environment template
cp .env.example .env
# Edit .env with your domain and email
docker-compose up -d
```

## Environment Configuration

### Development Environment
- **File**: `appsettings.Development.json`
- **HTTPS Port**: 7151
- **HTTP Port**: 7150
- **Certificate**: Development certificate (auto-generated)
- **HTTPS Redirection**: Disabled for convenience

### Staging Environment
- **File**: `appsettings.Staging.json`
- **HTTPS Port**: 5001
- **HTTP Port**: 5000
- **Certificate**: Self-signed or staging certificate
- **HTTPS Redirection**: Enabled

### Production Environment
- **File**: `appsettings.Production.json`
- **HTTPS Port**: 443
- **HTTP Port**: 80
- **Certificate**: Let's Encrypt or purchased certificate
- **HTTPS Redirection**: Enabled with HSTS

## SSL Certificate Management

### 1. Development Certificates

#### Windows
```powershell
# Generate self-signed certificate for development
.\scripts\generate-ssl-cert.ps1 -Domain "localhost"
```

#### Linux/macOS
```bash
# Generate self-signed certificate for development
./scripts/generate-ssl-cert.sh
```

### 2. Production Certificates (Let's Encrypt)

#### Automatic Setup (Recommended)
```bash
# Run the Let's Encrypt setup script
sudo ./scripts/letsencrypt-setup.sh yourdomain.com admin@yourdomain.com
```

#### Manual Setup
```bash
# Install certbot
sudo apt-get update
sudo apt-get install snapd
sudo snap install --classic certbot

# Get certificate
sudo certbot certonly --standalone -d yourdomain.com

# Convert to PFX format
sudo openssl pkcs12 -export \
  -out /opt/portfolio-backend/ssl/certificate.pfx \
  -inkey /etc/letsencrypt/live/yourdomain.com/privkey.pem \
  -in /etc/letsencrypt/live/yourdomain.com/cert.pem \
  -certfile /etc/letsencrypt/live/yourdomain.com/chain.pem \
  -password pass:""
```

### 3. Certificate Renewal

Automatic renewal is configured via:
- **Cron job**: `0 12 * * * certbot renew --quiet`
- **Renewal hook**: `/etc/letsencrypt/renewal-hooks/post/portfolio-backend`

## Deployment Scenarios

### 1. Docker Deployment

#### Basic Setup
```bash
# Clone the repository
git clone <repository-url>
cd Portfolio/PortfolioBackend

# Build and run with Docker Compose
docker-compose up -d
```

#### With Custom Domain
```bash
# Set environment variables
export DOMAIN=yourdomain.com
export LETSENCRYPT_EMAIL=admin@yourdomain.com
export USE_LETS_ENCRYPT=true

# Deploy with SSL
docker-compose up -d
```

#### Environment Variables
```bash
# .env file
DOMAIN=yourdomain.com
LETSENCRYPT_EMAIL=admin@yourdomain.com
USE_LETS_ENCRYPT=true
SSL_CERT_PASSWORD=
EMAIL_PASSWORD=your_email_app_password
```

### 2. Linux Server Deployment

#### Ubuntu/Debian
```bash
# Run the deployment script
sudo ./scripts/deploy-linux.sh Production yourdomain.com admin@yourdomain.com
```

#### Manual Steps
```bash
# 1. Install .NET 8 Runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0

# 2. Copy application files
sudo cp -r . /opt/portfolio-backend/

# 3. Set up SSL certificate (see certificate management section)

# 4. Install systemd service
sudo cp scripts/portfolio-backend.service /etc/systemd/system/
sudo systemctl enable portfolio-backend
sudo systemctl start portfolio-backend
```

### 3. Windows Server Deployment (IIS)

#### PowerShell Script
```powershell
# Run as Administrator
.\scripts\deploy-windows.ps1 -Domain "yourdomain.com" -Environment "Production"
```

#### Manual Setup
1. **Install Prerequisites**:
   - IIS with ASP.NET Core Module
   - .NET 8 Hosting Bundle

2. **SSL Certificate**:
   - Use IIS Manager to import certificate
   - Or use win-acme for Let's Encrypt

3. **Application Pool**:
   - Create new application pool
   - Set .NET CLR version to "No Managed Code"

4. **Website Configuration**:
   - Create new website
   - Configure HTTPS binding with certificate
   - Set physical path to published application

### 4. Cloud Deployments

#### Azure App Service
```bash
# Deploy to Azure App Service with SSL
az webapp create --name portfolio-backend --resource-group myResourceGroup
az webapp config ssl upload --certificate-file certificate.pfx --certificate-password ""
az webapp config ssl bind --certificate-thumbprint <thumbprint> --ssl-type SNI
```

#### AWS Elastic Beanstalk
```bash
# Deploy with Application Load Balancer and ACM certificate
eb init portfolio-backend
eb create production --elb-type application
# Configure SSL certificate through AWS Certificate Manager
```

#### Google Cloud Run
```bash
# Deploy with Cloud Run and Cloud Load Balancing
gcloud run deploy portfolio-backend --image gcr.io/project/portfolio-backend
# Configure SSL through Cloud Load Balancer
```

### 5. FTP Deployment

#### Using Publish Profile
```bash
# Publish to FTP server
dotnet publish -p:PublishProfile=FTP
```

#### Manual FTP Upload
1. Publish locally: `dotnet publish -c Release`
2. Upload files to web server
3. Configure SSL certificate on web server
4. Update `appsettings.Production.json` with correct paths

## Troubleshooting

### Common SSL Issues

#### "SSL connection could not be established"
```bash
# Check certificate validity
openssl x509 -in /path/to/certificate.crt -text -noout

# Test SSL connection
openssl s_client -connect yourdomain.com:443

# Check certificate chain
curl -I https://yourdomain.com
```

#### "Certificate not trusted"
- **Development**: Run `dotnet dev-certs https --trust`
- **Production**: Ensure certificate is from trusted CA
- **Self-signed**: Add certificate to trusted store

#### "Mixed content warnings"
- Ensure all resources (CSS, JS, images) use HTTPS
- Check `config.js` for proper protocol detection
- Update any hardcoded HTTP URLs to HTTPS

#### Port binding issues
```bash
# Check port usage
netstat -tlnp | grep :443
lsof -i :443

# Kill processes using port
sudo fuser -k 443/tcp
```

### Application-Specific Issues

#### Backend API not accessible
1. **Check firewall**: Ports 80 and 443 should be open
2. **Check binding**: Application should bind to `https://+:443`
3. **Check certificate**: Verify certificate path and permissions
4. **Check logs**: `journalctl -u portfolio-backend -f`

#### Frontend HTTPS detection
1. **Check config.js**: Verify protocol detection logic
2. **Check shared-components.js**: Verify backend URL generation
3. **Browser console**: Look for mixed content warnings

### Debug Commands

#### Check SSL Configuration
```bash
# Test SSL connection
curl -I https://yourdomain.com

# Check certificate details
openssl s_client -connect yourdomain.com:443 -servername yourdomain.com

# Test HTTP to HTTPS redirect
curl -I http://yourdomain.com
```

#### Check Application Health
```bash
# Health endpoint
curl https://yourdomain.com/health

# Application logs
journalctl -u portfolio-backend -f

# System resources
systemctl status portfolio-backend
```

## Security Best Practices

### 1. Certificate Security
- **Use strong passwords** for certificate files (if required)
- **Restrict file permissions**: `chmod 600` for certificate files
- **Regular renewal**: Automate certificate renewal
- **Monitor expiration**: Set up alerts for certificate expiration

### 2. HTTPS Configuration
- **Enable HSTS**: Force HTTPS for all connections
- **Secure headers**: X-Content-Type-Options, X-Frame-Options, etc.
- **TLS version**: Use TLS 1.2 or higher only
- **Strong ciphers**: Configure secure cipher suites

### 3. Network Security
- **Firewall rules**: Only allow necessary ports (80, 443, 22)
- **Rate limiting**: Configure to prevent abuse
- **IP filtering**: Restrict access if needed
- **Regular updates**: Keep system and dependencies updated

### 4. Application Security
- **Environment variables**: Use for sensitive configuration
- **Secrets management**: Don't store secrets in code
- **Content Security Policy**: Configure CSP headers
- **CORS policy**: Restrict to trusted domains only

## Configuration Examples

### Docker Compose with Traefik
```yaml
version: '3.8'
services:
  portfolio-backend:
    build: .
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.portfolio.rule=Host(`yourdomain.com`)"
      - "traefik.http.routers.portfolio.entrypoints=websecure"
      - "traefik.http.routers.portfolio.tls.certresolver=letsencrypt"
```

### Nginx Reverse Proxy
```nginx
server {
    listen 443 ssl http2;
    server_name yourdomain.com;
    
    ssl_certificate /etc/ssl/certs/yourdomain.com.crt;
    ssl_certificate_key /etc/ssl/private/yourdomain.com.key;
    
    location / {
        proxy_pass https://localhost:5001;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Environment Variables
```bash
# Production environment variables
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
SSL_CERT_PASSWORD=
EMAIL_PASSWORD=your_app_password
CORS_ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com
```

## Support

For additional help:
1. Check application logs for specific error messages
2. Verify all configuration files are properly formatted
3. Test SSL certificate validity using online tools
4. Ensure DNS records point to your server
5. Check firewall and network configuration

Remember to replace `yourdomain.com` and email addresses with your actual domain and contact information throughout this guide.
