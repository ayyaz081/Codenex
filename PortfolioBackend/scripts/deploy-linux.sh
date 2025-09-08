#!/usr/bin/env bash
set -euo pipefail

# Portfolio Backend Linux Deployment Script with SSL Support
# Usage: ./deploy-linux.sh [environment] [domain] [email]

ENVIRONMENT=${1:-Production}
DOMAIN=${2:-localhost}
EMAIL=${3:-admin@localhost}
INSTALL_DIR="/opt/portfolio-backend"
SSL_DIR="$INSTALL_DIR/ssl"
DATA_DIR="$INSTALL_DIR/data"
UPLOAD_DIR="$INSTALL_DIR/uploads"
LOG_DIR="$INSTALL_DIR/logs"

echo "=== Portfolio Backend Linux Deployment ==="
echo "Environment: $ENVIRONMENT"
echo "Domain: $DOMAIN"
echo "Email: $EMAIL"
echo "Install Directory: $INSTALL_DIR"

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   echo "This script should not be run as root. Please run as a regular user with sudo privileges."
   exit 1
fi

# Install dependencies
echo "Installing dependencies..."
sudo apt-get update
sudo apt-get install -y wget curl apt-transport-https software-properties-common

# Install .NET 8 Runtime if not present
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET 8 Runtime..."
    wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y aspnetcore-runtime-8.0
    rm packages-microsoft-prod.deb
fi

# Install Certbot for Let's Encrypt if needed
if [[ "$ENVIRONMENT" == "Production" ]] && [[ "$DOMAIN" != "localhost" ]]; then
    if ! command -v certbot &> /dev/null; then
        echo "Installing Certbot for Let's Encrypt..."
        sudo apt-get install -y snapd
        sudo snap install core; sudo snap refresh core
        sudo snap install --classic certbot
        sudo ln -sf /snap/bin/certbot /usr/bin/certbot
    fi
fi

# Create application directories
echo "Creating application directories..."
sudo mkdir -p "$INSTALL_DIR" "$SSL_DIR" "$DATA_DIR" "$UPLOAD_DIR" "$LOG_DIR"

# Create www-data user if it doesn't exist
if ! id "www-data" &>/dev/null; then
    sudo useradd -r -s /bin/false www-data
fi

# Copy application files (assumes current directory contains published files)
echo "Copying application files..."
sudo cp -r . "$INSTALL_DIR/"

# Set permissions
echo "Setting permissions..."
sudo chown -R www-data:www-data "$INSTALL_DIR"
sudo chmod +x "$INSTALL_DIR/PortfolioBackend"
sudo chmod +x "$INSTALL_DIR/scripts"/*.sh

# Generate or obtain SSL certificate
echo "Setting up SSL certificate..."
if [[ "$ENVIRONMENT" == "Production" ]] && [[ "$DOMAIN" != "localhost" ]]; then
    echo "Obtaining Let's Encrypt certificate for $DOMAIN..."
    
    # Stop any existing web servers temporarily
    sudo systemctl stop nginx apache2 || true
    
    # Get certificate
    sudo certbot certonly --standalone --non-interactive --agree-tos --email "$EMAIL" -d "$DOMAIN"
    
    # Convert to PFX format for .NET
    sudo openssl pkcs12 -export \
        -out "$SSL_DIR/certificate.pfx" \
        -inkey "/etc/letsencrypt/live/$DOMAIN/privkey.pem" \
        -in "/etc/letsencrypt/live/$DOMAIN/cert.pem" \
        -certfile "/etc/letsencrypt/live/$DOMAIN/chain.pem" \
        -password pass:""
        
    # Copy certificate files
    sudo cp "/etc/letsencrypt/live/$DOMAIN/cert.pem" "$SSL_DIR/certificate.crt"
    sudo cp "/etc/letsencrypt/live/$DOMAIN/privkey.pem" "$SSL_DIR/certificate.key"
    
    # Set up certificate renewal
    echo "Setting up certificate auto-renewal..."
    sudo crontab -l 2>/dev/null | grep -v 'certbot renew' | sudo crontab -
    (sudo crontab -l 2>/dev/null; echo "0 12 * * * certbot renew --quiet && systemctl reload portfolio-backend") | sudo crontab -
else
    echo "Generating self-signed certificate for development/testing..."
    sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout "$SSL_DIR/certificate.key" \
        -out "$SSL_DIR/certificate.crt" \
        -subj "/C=US/ST=Dev/L=Dev/O=LocalDev/OU=Dev/CN=$DOMAIN"
    
    sudo openssl pkcs12 -export \
        -out "$SSL_DIR/certificate.pfx" \
        -inkey "$SSL_DIR/certificate.key" \
        -in "$SSL_DIR/certificate.crt" \
        -password pass:""
fi

# Set certificate permissions
sudo chmod 600 "$SSL_DIR"/*
sudo chown www-data:www-data "$SSL_DIR"/*

# Install systemd service
echo "Installing systemd service..."
sudo cp "$INSTALL_DIR/scripts/portfolio-backend.service" "/etc/systemd/system/"

# Update service file with correct paths
sudo sed -i "s|/opt/portfolio-backend|$INSTALL_DIR|g" "/etc/systemd/system/portfolio-backend.service"

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable portfolio-backend
sudo systemctl start portfolio-backend

# Install Nginx reverse proxy (optional)
read -p "Do you want to install Nginx reverse proxy? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    if ! command -v nginx &> /dev/null; then
        echo "Installing Nginx..."
        sudo apt-get install -y nginx
    fi
    
    # Configure Nginx
    sudo cp "$INSTALL_DIR/nginx/nginx.conf" "/etc/nginx/sites-available/portfolio-backend"
    sudo ln -sf "/etc/nginx/sites-available/portfolio-backend" "/etc/nginx/sites-enabled/"
    sudo rm -f "/etc/nginx/sites-enabled/default"
    
    # Update Nginx config with actual domain and SSL paths
    sudo sed -i "s|server_name _|server_name $DOMAIN|g" "/etc/nginx/sites-available/portfolio-backend"
    sudo sed -i "s|/etc/nginx/ssl|$SSL_DIR|g" "/etc/nginx/sites-available/portfolio-backend"
    
    # Test and reload Nginx
    sudo nginx -t && sudo systemctl reload nginx
fi

# Configure firewall
echo "Configuring firewall..."
if command -v ufw &> /dev/null; then
    sudo ufw allow 22/tcp
    sudo ufw allow 80/tcp
    sudo ufw allow 443/tcp
    sudo ufw --force enable
fi

# Display deployment information
echo ""
echo "=== Deployment Complete! ==="
echo "Application: $INSTALL_DIR"
echo "SSL Certificates: $SSL_DIR"
echo "Database: $DATA_DIR"
echo "Service: portfolio-backend"
echo ""
echo "Service status:"
sudo systemctl status portfolio-backend --no-pager -l
echo ""
echo "To check logs: sudo journalctl -u portfolio-backend -f"
echo "To restart: sudo systemctl restart portfolio-backend"
echo ""
echo "Your application should be available at:"
if [[ "$ENVIRONMENT" == "Production" ]]; then
    echo "  https://$DOMAIN"
else
    echo "  https://$DOMAIN:443 (self-signed certificate)"
fi

echo ""
echo "Don't forget to:"
echo "1. Update your DNS records to point to this server"
echo "2. Configure your application settings in appsettings.Production.json"
echo "3. Set environment variables for sensitive data"
echo "4. Test SSL certificate validity"
