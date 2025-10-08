#!/bin/bash
# CodeNex Deployment Script
# This script automates the deployment process

set -e  # Exit on any error

# Configuration
APP_NAME="codenex"
APP_DIR="/var/www/codenex"
SERVICE_NAME="codenex"
NGINX_SITE_NAME="codenex"
PUBLISH_DIR="./publish"

echo "================================"
echo "CodeNex Deployment Script"
echo "================================"
echo ""

# Check if running as root
if [ "$EUID" -eq 0 ]; then 
    echo "Warning: Do not run this script as root. Run as your user with sudo privileges."
    exit 1
fi

# Step 1: Build and publish the application
echo "Step 1: Building and publishing application..."
cd "$(dirname "$0")/.."
dotnet publish -c Release -o "$PUBLISH_DIR" --self-contained false

if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Error: Publish directory not found!"
    exit 1
fi

echo "✓ Build complete"

# Step 2: Stop the service if running
echo ""
echo "Step 2: Stopping application service..."
sudo systemctl stop $SERVICE_NAME || echo "Service not running"

# Step 3: Backup existing deployment (if exists)
if [ -d "$APP_DIR" ]; then
    echo ""
    echo "Step 3: Creating backup..."
    BACKUP_DIR="$APP_DIR-backup-$(date +%Y%m%d-%H%M%S)"
    sudo cp -r "$APP_DIR" "$BACKUP_DIR"
    echo "✓ Backup created at: $BACKUP_DIR"
fi

# Step 4: Copy files to deployment directory
echo ""
echo "Step 4: Copying files to $APP_DIR..."
sudo mkdir -p "$APP_DIR"
sudo cp -r "$PUBLISH_DIR"/* "$APP_DIR/"

# Step 5: Copy .env file
echo ""
echo "Step 5: Copying .env file..."
if [ -f ".env" ]; then
    sudo cp .env "$APP_DIR/.env"
    sudo chmod 600 "$APP_DIR/.env"
    echo "✓ .env file copied"
else
    echo "Warning: .env file not found in current directory"
    echo "Make sure to manually copy your .env file to $APP_DIR/.env"
fi

# Step 6: Set permissions
echo ""
echo "Step 6: Setting permissions..."
sudo chown -R www-data:www-data "$APP_DIR"
sudo chmod -R 755 "$APP_DIR"
sudo chmod 600 "$APP_DIR/.env" 2>/dev/null || true

# Step 7: Install/update systemd service
echo ""
echo "Step 7: Installing systemd service..."
if [ -f "deployment/codenex.service" ]; then
    sudo cp deployment/codenex.service /etc/systemd/system/
    sudo systemctl daemon-reload
    echo "✓ Systemd service installed"
else
    echo "Warning: deployment/codenex.service not found"
fi

# Step 8: Install/update Nginx configuration
echo ""
echo "Step 8: Checking Nginx configuration..."
if [ -f "deployment/nginx-codenex.conf" ]; then
    if [ ! -f "/etc/nginx/sites-available/$NGINX_SITE_NAME" ]; then
        echo "Installing Nginx configuration..."
        sudo cp deployment/nginx-codenex.conf /etc/nginx/sites-available/$NGINX_SITE_NAME
        sudo ln -sf /etc/nginx/sites-available/$NGINX_SITE_NAME /etc/nginx/sites-enabled/$NGINX_SITE_NAME
        echo "✓ Nginx configuration installed"
    else
        echo "Nginx configuration already exists (not overwriting)"
    fi
    
    # Test Nginx configuration
    sudo nginx -t
else
    echo "Warning: deployment/nginx-codenex.conf not found"
fi

# Step 9: Start the service
echo ""
echo "Step 9: Starting application service..."
sudo systemctl enable $SERVICE_NAME
sudo systemctl start $SERVICE_NAME

# Wait a few seconds for service to start
sleep 3

# Check service status
echo ""
echo "Step 10: Checking service status..."
if sudo systemctl is-active --quiet $SERVICE_NAME; then
    echo "✓ Service is running"
    sudo systemctl status $SERVICE_NAME --no-pager -l
else
    echo "✗ Service failed to start"
    echo "Checking logs..."
    sudo journalctl -u $SERVICE_NAME -n 50 --no-pager
    exit 1
fi

# Step 11: Reload Nginx
echo ""
echo "Step 11: Reloading Nginx..."
sudo systemctl reload nginx

# Step 12: Check health endpoint
echo ""
echo "Step 12: Checking application health..."
sleep 2
if curl -f http://localhost:7150/health/live > /dev/null 2>&1; then
    echo "✓ Application health check passed"
else
    echo "Warning: Health check failed (this might be normal if the app is still starting)"
fi

echo ""
echo "================================"
echo "Deployment Complete!"
echo "================================"
echo ""
echo "Service Status:"
sudo systemctl status $SERVICE_NAME --no-pager -l | head -n 10
echo ""
echo "View logs with: sudo journalctl -u $SERVICE_NAME -f"
echo "Check health: curl https://codenex.live/health"
echo ""
