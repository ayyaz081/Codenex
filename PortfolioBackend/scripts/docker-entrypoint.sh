#!/usr/bin/env bash
set -euo pipefail

echo "Starting Portfolio Backend..."

# Generate SSL certificate if needed
if [[ "${USE_LETS_ENCRYPT:-false}" == "true" ]]; then
    echo "Let's Encrypt is enabled, attempting to get certificate..."
    # Use certbot or similar tool for Let's Encrypt
    # For now, fall back to self-signed if certbot fails
    if ! command -v certbot &> /dev/null; then
        echo "certbot not found, falling back to self-signed certificate"
        /app/scripts/generate-ssl-cert.sh
    else
        # Try to get Let's Encrypt certificate
        DOMAINS="${LETSENCRYPT_DOMAINS:-localhost}"
        EMAIL="${LETSENCRYPT_EMAIL:-admin@localhost}"
        
        # Split domains by comma and create --domain args
        IFS=',' read -ra DOMAIN_ARRAY <<< "$DOMAINS"
        DOMAIN_ARGS=""
        for domain in "${DOMAIN_ARRAY[@]}"; do
            DOMAIN_ARGS="$DOMAIN_ARGS --domain $domain"
        done
        
        if certbot certonly --standalone --non-interactive --agree-tos --email "$EMAIL" $DOMAIN_ARGS; then
            # Convert Let's Encrypt cert to PFX
            FIRST_DOMAIN=$(echo "$DOMAINS" | cut -d',' -f1)
            openssl pkcs12 -export \
                -out /app/ssl/certificate.pfx \
                -inkey /etc/letsencrypt/live/$FIRST_DOMAIN/privkey.pem \
                -in /etc/letsencrypt/live/$FIRST_DOMAIN/cert.pem \
                -certfile /etc/letsencrypt/live/$FIRST_DOMAIN/chain.pem \
                -password pass:"${SSL_CERT_PASSWORD:-}"
            echo "Let's Encrypt certificate converted to PFX format"
        else
            echo "Let's Encrypt failed, falling back to self-signed certificate"
            /app/scripts/generate-ssl-cert.sh
        fi
    fi
else
    echo "Using self-signed certificate for SSL..."
    /app/scripts/generate-ssl-cert.sh
fi

# Set correct permissions
chmod -R 600 /app/ssl/
chmod 644 /app/ssl/certificate.crt

# Ensure database directory exists
mkdir -p /app/data

echo "SSL setup complete. Starting application..."

# Start the application
exec dotnet PortfolioBackend.dll
