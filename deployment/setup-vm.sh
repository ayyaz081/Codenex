#!/bin/bash
# CodeNex VM Setup Script for Ubuntu 22.04
# This script installs all required dependencies

set -e  # Exit on any error

echo "================================"
echo "CodeNex VM Setup for Ubuntu 22.04"
echo "================================"

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run this script with sudo"
    exit 1
fi

# Update system
echo ""
echo "Step 1: Updating system packages..."
apt update && apt upgrade -y

# Install required dependencies
echo ""
echo "Step 2: Installing required dependencies..."
apt install -y wget curl git nano ufw software-properties-common

# Install .NET 8 SDK and Runtime
echo ""
echo "Step 3: Installing .NET 8..."
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
rm dotnet-install.sh

# Create symlinks for dotnet
ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet

# Verify .NET installation
echo ""
echo "Verifying .NET installation..."
dotnet --version

# Install Nginx
echo ""
echo "Step 4: Installing Nginx..."
apt install -y nginx

# Install Certbot for Let's Encrypt
echo ""
echo "Step 5: Installing Certbot..."
apt install -y certbot python3-certbot-nginx

# Create application directory
echo ""
echo "Step 6: Creating application directory..."
mkdir -p /var/www/codenex
mkdir -p /var/www/certbot

# Set permissions
chown -R www-data:www-data /var/www/codenex
chmod -R 755 /var/www/codenex

# Configure firewall
echo ""
echo "Step 7: Configuring firewall..."
ufw --force enable
ufw default deny incoming
ufw default allow outgoing
ufw allow ssh
ufw allow 'Nginx Full'
ufw allow 80/tcp
ufw allow 443/tcp

echo ""
echo "Step 8: Firewall status:"
ufw status

# Enable services
echo ""
echo "Step 9: Enabling Nginx..."
systemctl enable nginx
systemctl start nginx

echo ""
echo "================================"
echo "VM Setup Complete!"
echo "================================"
echo ""
echo "Installed:"
echo "  - .NET 8 SDK and Runtime"
echo "  - Nginx"
echo "  - Certbot"
echo "  - UFW Firewall"
echo ""
echo "Next steps:"
echo "  1. Upload your application to /var/www/codenex"
echo "  2. Copy your .env file to /var/www/codenex/.env"
echo "  3. Configure Nginx: sudo cp deployment/nginx-codenex.conf /etc/nginx/sites-available/codenex"
echo "  4. Enable site: sudo ln -s /etc/nginx/sites-available/codenex /etc/nginx/sites-enabled/"
echo "  5. Install SSL: sudo certbot --nginx -d codenex.live -d www.codenex.live"
echo "  6. Install systemd service: sudo cp deployment/codenex.service /etc/systemd/system/"
echo "  7. Start application: sudo systemctl enable codenex && sudo systemctl start codenex"
echo ""
