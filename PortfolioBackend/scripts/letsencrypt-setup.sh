#!/usr/bin/env bash
set -euo pipefail

# Let's Encrypt SSL Certificate Setup Script
# Usage: ./letsencrypt-setup.sh domain.com admin@domain.com [staging]

DOMAIN=${1:-}
EMAIL=${2:-}
STAGING=${3:-false}
WEBROOT=${WEBROOT:-/var/www/html}
CERT_DIR=${CERT_DIR:-/opt/portfolio-backend/ssl}
RENEWAL_HOOK=${RENEWAL_HOOK:-"systemctl reload portfolio-backend"}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Let's Encrypt Certificate Setup ===${NC}"

if [[ -z "$DOMAIN" ]] || [[ -z "$EMAIL" ]]; then
    echo -e "${RED}Usage: $0 <domain> <email> [staging]${NC}"
    echo -e "${YELLOW}Example: $0 yourdomain.com admin@yourdomain.com${NC}"
    echo -e "${YELLOW}For staging: $0 yourdomain.com admin@yourdomain.com staging${NC}"
    exit 1
fi

echo "Domain: $DOMAIN"
echo "Email: $EMAIL"
echo "Staging: $STAGING"
echo "Webroot: $WEBROOT"
echo "Certificate Directory: $CERT_DIR"

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}This script must be run as root${NC}"
   exit 1
fi

# Update package list
echo -e "${YELLOW}Updating package list...${NC}"
apt-get update

# Install certbot if not present
if ! command -v certbot &> /dev/null; then
    echo -e "${YELLOW}Installing Certbot...${NC}"
    apt-get install -y snapd
    snap install core
    snap refresh core
    snap install --classic certbot
    ln -sf /snap/bin/certbot /usr/bin/certbot
fi

# Create certificate directory
mkdir -p "$CERT_DIR"

# Determine certbot arguments
CERTBOT_ARGS="--non-interactive --agree-tos --email $EMAIL"

if [[ "$STAGING" == "staging" ]]; then
    CERTBOT_ARGS="$CERTBOT_ARGS --staging"
    echo -e "${YELLOW}Using Let's Encrypt staging environment (for testing)${NC}"
fi

# Stop web server temporarily
echo -e "${YELLOW}Stopping web services temporarily...${NC}"
systemctl stop nginx apache2 portfolio-backend || true
sleep 2

# Function to cleanup and restart services
cleanup() {
    echo -e "${YELLOW}Restarting services...${NC}"
    systemctl start portfolio-backend || true
    systemctl start nginx || true
}
trap cleanup EXIT

# Request certificate using standalone mode
echo -e "${YELLOW}Requesting SSL certificate for $DOMAIN...${NC}"
if certbot certonly --standalone $CERTBOT_ARGS -d "$DOMAIN"; then
    echo -e "${GREEN}Certificate obtained successfully!${NC}"
    
    # Convert certificate to PFX format for ASP.NET Core
    echo -e "${YELLOW}Converting certificate to PFX format...${NC}"
    
    CERT_PATH="/etc/letsencrypt/live/$DOMAIN"
    PFX_PATH="$CERT_DIR/certificate.pfx"
    CRT_PATH="$CERT_DIR/certificate.crt"
    KEY_PATH="$CERT_DIR/certificate.key"
    
    # Create PFX file (no password for simplicity)
    openssl pkcs12 -export \
        -out "$PFX_PATH" \
        -inkey "$CERT_PATH/privkey.pem" \
        -in "$CERT_PATH/cert.pem" \
        -certfile "$CERT_PATH/chain.pem" \
        -password pass:""
    
    # Copy certificate files
    cp "$CERT_PATH/cert.pem" "$CRT_PATH"
    cp "$CERT_PATH/privkey.pem" "$KEY_PATH"
    cp "$CERT_PATH/fullchain.pem" "$CERT_DIR/fullchain.crt"
    
    # Set proper permissions
    chmod 600 "$CERT_DIR"/*
    chown -R www-data:www-data "$CERT_DIR"
    
    echo -e "${GREEN}Certificate files created:${NC}"
    echo "  PFX: $PFX_PATH"
    echo "  CRT: $CRT_PATH"
    echo "  KEY: $KEY_PATH"
    echo "  Fullchain: $CERT_DIR/fullchain.crt"
    
    # Set up automatic renewal
    echo -e "${YELLOW}Setting up automatic certificate renewal...${NC}"
    
    # Create renewal hook script
    cat > /etc/letsencrypt/renewal-hooks/post/portfolio-backend << EOF
#!/bin/bash
# Portfolio Backend certificate renewal hook

DOMAIN="$DOMAIN"
CERT_DIR="$CERT_DIR"
CERT_PATH="/etc/letsencrypt/live/\$DOMAIN"

# Convert renewed certificate to PFX format
openssl pkcs12 -export \\
    -out "\$CERT_DIR/certificate.pfx" \\
    -inkey "\$CERT_PATH/privkey.pem" \\
    -in "\$CERT_PATH/cert.pem" \\
    -certfile "\$CERT_PATH/chain.pem" \\
    -password pass:""

# Copy certificate files
cp "\$CERT_PATH/cert.pem" "\$CERT_DIR/certificate.crt"
cp "\$CERT_PATH/privkey.pem" "\$CERT_DIR/certificate.key"
cp "\$CERT_PATH/fullchain.pem" "\$CERT_DIR/fullchain.crt"

# Set proper permissions
chmod 600 "\$CERT_DIR"/*
chown -R www-data:www-data "\$CERT_DIR"

# Restart application
$RENEWAL_HOOK

echo "Certificate renewed and application restarted"
EOF
    
    chmod +x /etc/letsencrypt/renewal-hooks/post/portfolio-backend
    
    # Add cron job for renewal (certbot comes with its own timer, but this is a backup)
    (crontab -l 2>/dev/null | grep -v 'certbot renew'; echo "0 12 * * * certbot renew --quiet") | crontab -
    
    # Test certificate renewal
    echo -e "${YELLOW}Testing certificate renewal...${NC}"
    if certbot renew --dry-run; then
        echo -e "${GREEN}Certificate renewal test passed!${NC}"
    else
        echo -e "${RED}Certificate renewal test failed. Check configuration.${NC}"
    fi
    
    # Display certificate information
    echo -e "${GREEN}Certificate Information:${NC}"
    openssl x509 -in "$CRT_PATH" -text -noout | grep -E "(Subject:|Issuer:|Not Before:|Not After :)"
    
    echo ""
    echo -e "${GREEN}=== Setup Complete! ===${NC}"
    echo "Your SSL certificate has been installed and configured."
    echo "Certificate files are located in: $CERT_DIR"
    echo "Automatic renewal is configured."
    echo ""
    echo "Next steps:"
    echo "1. Update your application configuration to use the certificate"
    echo "2. Test HTTPS access to your domain"
    echo "3. Check that HTTP redirects to HTTPS"
    
else
    echo -e "${RED}Failed to obtain SSL certificate${NC}"
    echo "Please check:"
    echo "1. Domain DNS points to this server"
    echo "2. Port 80 is accessible from the internet"
    echo "3. No other web server is running on port 80"
    echo "4. Email address is valid"
    exit 1
fi
