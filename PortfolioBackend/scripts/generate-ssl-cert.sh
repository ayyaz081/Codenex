#!/usr/bin/env bash
set -euo pipefail

CERT_DIR=${CERT_DIR:-/app/ssl}
PFX_PATH="$CERT_DIR/certificate.pfx"
CRT_PATH="$CERT_DIR/certificate.crt"
KEY_PATH="$CERT_DIR/certificate.key"
PFX_PASSWORD="${SSL_CERT_PASSWORD:-}"

mkdir -p "$CERT_DIR"

if [[ ! -f "$PFX_PATH" ]];
then
  echo "No PFX certificate found, generating a self-signed cert for development..."
  openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout "$KEY_PATH" \
    -out "$CRT_PATH" \
    -subj "/C=US/ST=NA/L=NA/O=LocalDev/OU=Dev/CN=localhost"

  # Create PFX from CRT and KEY
  openssl pkcs12 -export -out "$PFX_PATH" -inkey "$KEY_PATH" -in "$CRT_PATH" -password pass:"$PFX_PASSWORD"
  echo "Generated self-signed certificate at $PFX_PATH"
else
  echo "PFX certificate already exists at $PFX_PATH"
fi

