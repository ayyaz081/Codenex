# CodeNex Deployment Guide for Ubuntu 22.04

Complete guide for deploying your .NET 8 application with Nginx, SSL/TLS, and systemd on Ubuntu 22.04.

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [VM Requirements](#vm-requirements)
3. [Initial VM Setup](#initial-vm-setup)
4. [SSL Certificate Setup](#ssl-certificate-setup)
5. [Application Deployment](#application-deployment)
6. [Verification](#verification)
7. [Troubleshooting](#troubleshooting)
8. [Maintenance](#maintenance)

---

## Prerequisites

### Local Machine (Windows)
- ✅ .NET 8 SDK installed
- ✅ Git installed
- ✅ SSH client (built into Windows 10+)

### Domain Configuration
- ✅ Domain: `codenex.live` and `www.codenex.live`
- ✅ DNS A Records pointing to your VM's public IP address

### VM Access
- ✅ Ubuntu 22.04 LTS server
- ✅ SSH access with sudo privileges
- ✅ Public IP address

---

## VM Requirements

### Minimum Specifications
- **OS**: Ubuntu 22.04 LTS (64-bit)
- **CPU**: 2 vCPUs
- **RAM**: 2 GB minimum (4 GB recommended)
- **Storage**: 20 GB minimum (40 GB recommended)
- **Network**: Public IP address with ports 22, 80, 443 open

### Recommended Specifications (Production)
- **CPU**: 4 vCPUs
- **RAM**: 8 GB
- **Storage**: 50-100 GB SSD
- **Network**: Static public IP

---

## Initial VM Setup

### Step 1: Connect to Your VM

From Windows PowerShell or Command Prompt:
```powershell
ssh your-username@your-vm-ip
```

### Step 2: Update System
```bash
sudo apt update && sudo apt upgrade -y
```

### Step 3: Run Automated Setup Script

Upload the setup script to your VM:
```powershell
# From Windows (PowerShell) - Upload setup script
scp deployment/setup-vm.sh your-username@your-vm-ip:~/
```

Then on the VM, run:
```bash
chmod +x setup-vm.sh
sudo ./setup-vm.sh
```

This script will install:
- ✅ .NET 8 SDK and Runtime
- ✅ Nginx web server
- ✅ Certbot for SSL certificates
- ✅ UFW firewall (configured)
- ✅ Required dependencies

**⏱️ This will take 5-10 minutes to complete.**

### Step 4: Verify Installations

Check that everything is installed correctly:
```bash
# Check .NET version
dotnet --version
# Should show: 8.0.x

# Check Nginx status
sudo systemctl status nginx

# Check firewall status
sudo ufw status
```

---

## SSL Certificate Setup

### Step 1: Verify DNS Configuration

Before requesting SSL certificates, ensure your domain DNS is properly configured:
```bash
# Check if domain resolves to your VM
dig codenex.live
dig www.codenex.live
```

Both should return your VM's public IP address.

### Step 2: Configure Nginx for HTTP (Temporary)

Create a temporary Nginx configuration for initial setup:
```bash
sudo nano /etc/nginx/sites-available/codenex-temp
```

Paste this content:
```nginx
server {
    listen 80;
    listen [::]:80;
    server_name codenex.live www.codenex.live;

    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    location / {
        return 200 'Server is ready for SSL setup';
        add_header Content-Type text/plain;
    }
}
```

Enable it:
```bash
sudo ln -s /etc/nginx/sites-available/codenex-temp /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default  # Remove default site
sudo nginx -t
sudo systemctl reload nginx
```

### Step 3: Request SSL Certificate

Use Certbot to obtain a free Let's Encrypt SSL certificate:
```bash
sudo certbot certonly --nginx -d codenex.live -d www.codenex.live --email your-email@example.com --agree-tos --no-eff-email
```

You should see:
```
Successfully received certificate.
Certificate is saved at: /etc/letsencrypt/live/codenex.live/fullchain.pem
Key is saved at: /etc/letsencrypt/live/codenex.live/privkey.pem
```

### Step 4: Set Up Auto-Renewal

Certbot automatically creates a renewal timer. Verify it:
```bash
sudo systemctl status certbot.timer
sudo certbot renew --dry-run
```

---

## Application Deployment

### Method 1: Automated Deployment (Recommended)

#### From Your Windows Machine:

**Step 1:** Prepare your project
```powershell
cd C:\Users\Az\source\repos\ayyaz081\Codenex

# Make sure .env file exists and has production values
notepad .env
```

**Step 2:** Upload project to VM
```powershell
# Create a zip of your project (excluding bin, obj, node_modules)
Compress-Archive -Path * -DestinationPath codenex-deploy.zip -Force

# Upload to VM
scp codenex-deploy.zip your-username@your-vm-ip:~/
```

**Step 3:** On the VM, extract and deploy
```bash
# Extract files
mkdir -p ~/codenex-source
cd ~/codenex-source
unzip ~/codenex-deploy.zip

# Make deploy script executable
chmod +x deployment/deploy.sh

# Run deployment
./deployment/deploy.sh
```

The script will:
- ✅ Build and publish your application
- ✅ Copy files to `/var/www/codenex`
- ✅ Copy `.env` file
- ✅ Set correct permissions
- ✅ Install systemd service
- ✅ Install Nginx configuration
- ✅ Start the application

### Method 2: Manual Deployment

If you prefer manual deployment or the script fails:

**Step 1:** Build on Windows
```powershell
cd C:\Users\Az\source\repos\ayyaz081\Codenex
dotnet publish -c Release -o ./publish --self-contained false
```

**Step 2:** Upload to VM
```powershell
# Upload published files
scp -r publish/* your-username@your-vm-ip:/tmp/codenex-publish/

# Upload .env file
scp .env your-username@your-vm-ip:/tmp/codenex-publish/

# Upload deployment configs
scp deployment/* your-username@your-vm-ip:/tmp/codenex-deploy/
```

**Step 3:** Install on VM
```bash
# Create application directory
sudo mkdir -p /var/www/codenex

# Copy files
sudo cp -r /tmp/codenex-publish/* /var/www/codenex/

# Copy .env file
sudo cp /tmp/codenex-publish/.env /var/www/codenex/.env
sudo chmod 600 /var/www/codenex/.env

# Set permissions
sudo chown -R www-data:www-data /var/www/codenex
sudo chmod -R 755 /var/www/codenex

# Install systemd service
sudo cp /tmp/codenex-deploy/codenex.service /etc/systemd/system/
sudo systemctl daemon-reload

# Install Nginx configuration (remove temp config first)
sudo rm /etc/nginx/sites-enabled/codenex-temp
sudo cp /tmp/codenex-deploy/nginx-codenex.conf /etc/nginx/sites-available/codenex
sudo ln -s /etc/nginx/sites-available/codenex /etc/nginx/sites-enabled/

# Test Nginx configuration
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx

# Start application
sudo systemctl enable codenex
sudo systemctl start codenex
```

---

## Verification

### Step 1: Check Service Status
```bash
# Check if service is running
sudo systemctl status codenex

# View live logs
sudo journalctl -u codenex -f
```

Look for the line:
```
Loaded .env file from: /var/www/codenex/.env
```

This confirms your `.env` file is being read!

### Step 2: Check Application Health
```bash
# Local health check
curl http://localhost:7150/health/live

# Public health check
curl https://codenex.live/health
```

### Step 3: Test in Browser

Open your browser and navigate to:
- `https://codenex.live` - Should show your application
- `https://codenex.live/health` - Should show health status
- `https://codenex.live/swagger` - Should show API documentation

### Step 4: Verify SSL Certificate
```bash
# Check certificate details
echo | openssl s_client -servername codenex.live -connect codenex.live:443 2>/dev/null | openssl x509 -noout -dates
```

---

## Troubleshooting

### Issue 1: .env File Not Being Read

**Check if file exists:**
```bash
ls -la /var/www/codenex/.env
```

**Check logs for .env loading message:**
```bash
sudo journalctl -u codenex -n 100 | grep "\.env"
```

You should see:
```
Loaded .env file from: /var/www/codenex/.env
```

**If not found, verify Program.cs has the fix:**
```bash
cat /var/www/codenex/Program.cs | grep -A 10 "Load .env"
```

### Issue 2: Database Connection Failed

**Check environment variable:**
```bash
sudo cat /var/www/codenex/.env | grep DATABASE_CONNECTION_STRING
```

**Test database connectivity:**
```bash
# From VM, test SQL Server connection
nc -zv codenex.database.windows.net 1433
```

### Issue 3: Application Not Starting

**View detailed logs:**
```bash
sudo journalctl -u codenex -xe --no-pager
```

**Check for common issues:**
```bash
# Check if port is already in use
sudo netstat -tulpn | grep 7150

# Check file permissions
ls -la /var/www/codenex/

# Check .NET runtime
dotnet --list-runtimes
```

### Issue 4: SSL Certificate Issues

**Check certificate status:**
```bash
sudo certbot certificates
```

**Test renewal:**
```bash
sudo certbot renew --dry-run
```

### Issue 5: Nginx Errors

**Test configuration:**
```bash
sudo nginx -t
```

**View Nginx logs:**
```bash
sudo tail -f /var/log/nginx/codenex-error.log
sudo tail -f /var/log/nginx/codenex-access.log
```

---

## Maintenance

### Updating Your Application

When you make changes to your code:

**Quick Update (from VM):**
```bash
cd ~/codenex-source
git pull origin main  # If using git
./deployment/deploy.sh
```

**Or upload new version from Windows:**
```powershell
# Build locally
dotnet publish -c Release -o ./publish

# Upload
scp -r publish/* your-username@your-vm-ip:/tmp/codenex-update/

# On VM
sudo systemctl stop codenex
sudo cp -r /tmp/codenex-update/* /var/www/codenex/
sudo chown -R www-data:www-data /var/www/codenex
sudo systemctl start codenex
```

### Viewing Logs

**Application logs:**
```bash
# Live logs (follow)
sudo journalctl -u codenex -f

# Last 100 lines
sudo journalctl -u codenex -n 100

# Today's logs
sudo journalctl -u codenex --since today

# Errors only
sudo journalctl -u codenex -p err
```

**Nginx logs:**
```bash
sudo tail -f /var/log/nginx/codenex-access.log
sudo tail -f /var/log/nginx/codenex-error.log
```

### Updating .env File

```bash
# Edit .env file
sudo nano /var/www/codenex/.env

# Set correct permissions
sudo chmod 600 /var/www/codenex/.env
sudo chown www-data:www-data /var/www/codenex/.env

# Restart application
sudo systemctl restart codenex
```

### Backup Your Application

**Create backup:**
```bash
# Backup application and .env
sudo tar -czf /tmp/codenex-backup-$(date +%Y%m%d).tar.gz /var/www/codenex/

# Download to your machine
scp your-username@your-vm-ip:/tmp/codenex-backup-*.tar.gz ./
```

**Restore backup:**
```bash
sudo systemctl stop codenex
sudo tar -xzf /tmp/codenex-backup-YYYYMMDD.tar.gz -C /
sudo systemctl start codenex
```

### SSL Certificate Renewal

Certbot automatically renews certificates, but you can force renewal:
```bash
sudo certbot renew --force-renewal
sudo systemctl reload nginx
```

### Monitoring Resource Usage

```bash
# CPU and memory usage
htop

# Disk usage
df -h

# Application resource usage
ps aux | grep dotnet
```

---

## Security Best Practices

1. **Never commit .env file to git**
   ```bash
   # Add to .gitignore
   echo ".env" >> .gitignore
   ```

2. **Secure .env file permissions**
   ```bash
   sudo chmod 600 /var/www/codenex/.env
   ```

3. **Keep system updated**
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

4. **Monitor logs regularly**
   ```bash
   sudo journalctl -u codenex --since "1 hour ago"
   ```

5. **Use strong passwords** in your `.env` file

6. **Enable fail2ban** (optional but recommended)
   ```bash
   sudo apt install fail2ban -y
   sudo systemctl enable fail2ban
   sudo systemctl start fail2ban
   ```

---

## Common Commands Reference

| Task | Command |
|------|---------|
| Start application | `sudo systemctl start codenex` |
| Stop application | `sudo systemctl stop codenex` |
| Restart application | `sudo systemctl restart codenex` |
| View status | `sudo systemctl status codenex` |
| View logs | `sudo journalctl -u codenex -f` |
| Reload Nginx | `sudo systemctl reload nginx` |
| Test Nginx config | `sudo nginx -t` |
| Renew SSL | `sudo certbot renew` |
| Check firewall | `sudo ufw status` |

---

## Support & Next Steps

### Your Application is Now:
- ✅ Running on Ubuntu 22.04
- ✅ Secured with SSL/TLS (HTTPS)
- ✅ Reading `.env` file from `/var/www/codenex/.env`
- ✅ Proxied through Nginx
- ✅ Managed by systemd
- ✅ Auto-restarting on failure

### Access Your Application:
- **Website**: https://codenex.live
- **API Health**: https://codenex.live/health
- **API Docs**: https://codenex.live/swagger

### Monitoring:
- **Service Status**: `sudo systemctl status codenex`
- **Live Logs**: `sudo journalctl -u codenex -f`
- **Nginx Logs**: `/var/log/nginx/codenex-*.log`

---

## Questions?

If you encounter any issues:
1. Check the [Troubleshooting](#troubleshooting) section
2. View application logs: `sudo journalctl -u codenex -n 200`
3. Verify .env file is loaded: Look for "Loaded .env file from:" in logs
4. Test health endpoint: `curl http://localhost:7150/health/live`

**Remember**: The `.env` file MUST be in `/var/www/codenex/.env` with correct permissions (600) and owned by www-data.
