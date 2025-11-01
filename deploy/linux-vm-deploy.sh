#!/bin/bash
# ============================================
# Deploy CodeNex to Linux VM (Ubuntu/Debian)
# ============================================
# Run this script on your Linux VM to deploy CodeNex

set -e  # Exit on error

echo "=========================================="
echo "CodeNex Linux VM Deployment"
echo "=========================================="

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo "This script should be run as root (use sudo)" 
   exit 1
fi

# Update system
echo "Updating system packages..."
apt-get update
apt-get upgrade -y

# Install dependencies
echo "Installing dependencies..."
apt-get install -y curl wget git unzip

# Install Docker (if not installed)
if ! command -v docker &> /dev/null; then
    echo "Installing Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sh get-docker.sh
    systemctl enable docker
    systemctl start docker
    rm get-docker.sh
else
    echo "Docker is already installed"
fi

# Install Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo "Installing Docker Compose..."
    curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
else
    echo "Docker Compose is already installed"
fi

# Create application directory
APP_DIR="/opt/codenex"
echo "Creating application directory: $APP_DIR"
mkdir -p $APP_DIR
cd $APP_DIR

# Clone or copy application files
echo ""
echo "=========================================="
echo "Application files setup"
echo "=========================================="
echo "You need to copy your application files to $APP_DIR"
echo ""
echo "Options:"
echo "1. Clone from Git: git clone <your-repo-url> ."
echo "2. Copy files manually via SCP/SFTP"
echo "3. Download from URL"
echo ""
read -p "Do you want to clone from Git? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    read -p "Enter Git repository URL: " REPO_URL
    git clone $REPO_URL .
fi

# Create .env file from template
if [ ! -f .env ]; then
    if [ -f .env.example ]; then
        echo "Creating .env file from .env.example..."
        cp .env.example .env
        echo ""
        echo "IMPORTANT: Edit .env file with your configuration:"
        echo "  nano .env"
        echo ""
        read -p "Press Enter to edit .env file now..." 
        nano .env
    else
        echo "Warning: .env.example not found. You'll need to create .env manually"
    fi
fi

# Build and start containers
echo ""
echo "=========================================="
echo "Building and starting containers..."
echo "=========================================="
docker-compose down || true
docker-compose build
docker-compose up -d

# Setup systemd service for auto-restart
echo "Setting up systemd service..."
cat > /etc/systemd/system/codenex.service <<EOF
[Unit]
Description=CodeNex Application
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$APP_DIR
ExecStart=/usr/local/bin/docker-compose up -d
ExecStop=/usr/local/bin/docker-compose down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable codenex.service

# Setup Nginx reverse proxy (optional)
read -p "Do you want to setup Nginx reverse proxy? (y/n): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    apt-get install -y nginx
    
    read -p "Enter your domain name (e.g., codenex.example.com): " DOMAIN_NAME
    
    cat > /etc/nginx/sites-available/codenex <<EOF
server {
    listen 80;
    server_name $DOMAIN_NAME;

    location / {
        proxy_pass http://localhost:7150;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        client_max_body_size 100M;
    }
}
EOF

    ln -sf /etc/nginx/sites-available/codenex /etc/nginx/sites-enabled/
    nginx -t
    systemctl restart nginx
    
    echo ""
    echo "Nginx configured successfully!"
    echo ""
    echo "To enable SSL with Let's Encrypt, run:"
    echo "  apt-get install certbot python3-certbot-nginx"
    echo "  certbot --nginx -d $DOMAIN_NAME"
fi

# Setup firewall
echo ""
echo "Configuring firewall..."
if command -v ufw &> /dev/null; then
    ufw allow 22/tcp
    ufw allow 80/tcp
    ufw allow 443/tcp
    ufw allow 7150/tcp
    ufw --force enable
    echo "Firewall configured"
fi

# Display status
echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Application is running at:"
echo "  - Direct: http://$(curl -s ifconfig.me):7150"
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "  - Domain: http://$DOMAIN_NAME"
fi
echo ""
echo "Useful commands:"
echo "  - View logs: docker-compose logs -f"
echo "  - Restart: systemctl restart codenex"
echo "  - Stop: docker-compose down"
echo "  - Update: git pull && docker-compose build && docker-compose up -d"
echo ""
echo "Health check: curl http://localhost:7150/health"
echo ""
