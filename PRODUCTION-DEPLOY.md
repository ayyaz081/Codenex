# CodeNex Production Deployment

**For production server only - You develop on Windows, deploy on Linux**

---

## ⚡ First Time Setup

### 1. Install Everything

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y wget curl git nano nginx certbot python3-certbot-nginx
```

### 2. Install .NET 8

```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
rm dotnet-install.sh
```

### 3. Setup Firewall

```bash
sudo ufw allow ssh
sudo ufw allow 'Nginx Full'
sudo ufw --force enable
```

### 4. Deploy from GitHub

```bash
cd /tmp
git clone https://github.com/ayyaz081/Codenex.git
cd Codenex
dotnet publish CodeNex.csproj -c Release -o /tmp/codenex-build --self-contained false
sudo mkdir -p /var/www/codenex
sudo cp -r /tmp/codenex-build/* /var/www/codenex/
```

### 5. Create .env File

```bash
sudo nano /var/www/codenex/.env
```

**Paste your production values:**

```env
ASPNETCORE_ENVIRONMENT=Production
DATABASE_CONNECTION_STRING=Server=tcp:codenex.database.windows.net,1433;Initial Catalog=codenex;User ID=codenex;Password=YOUR_PASSWORD;Encrypt=True;
ADMIN_EMAIL=admin@codenex.live
ADMIN_PASSWORD=YOUR_PASSWORD
EmailSettings__Host=smtp.gmail.com
EmailSettings__Port=587
EmailSettings__FromEmail=your-email@gmail.com
EmailSettings__FromName=CodeNex Solutions
EmailSettings__Username=your-email@gmail.com
EmailSettings__Password=YOUR_APP_PASSWORD
EmailSettings__EnableSsl=true
EMAIL_HOST=smtp.gmail.com
REQUIRE_EMAIL_CONFIRMATION=true
JWT_KEY=YOUR-64-CHAR-KEY
JWT_ISSUER=CodeNexAPI
JWT_AUDIENCE=CodeNexAPI
JWT_EXPIRY_HOURS=1
API_BASE_URL=https://codenex.live
FRONTEND_URL=https://codenex.live
FRONTEND_BASE_URL=https://codenex.live
EMAIL_VERIFICATION_PATH=/EmailVerified
PASSWORD_RESET_PATH=/Auth
```

**Save:** `Ctrl+X` → `Y` → `Enter`

### 6. Set Permissions

```bash
sudo chown -R www-data:www-data /var/www/codenex
sudo chmod 755 /var/www/codenex
sudo chmod 600 /var/www/codenex/.env
```

### 7. Create Service

```bash
sudo nano /etc/systemd/system/codenex.service
```

**Paste:**

```ini
[Unit]
Description=CodeNex .NET Web Application
After=network.target

[Service]
Type=simple
User=www-data
Group=www-data
WorkingDirectory=/var/www/codenex
ExecStart=/usr/bin/dotnet /var/www/codenex/CodeNex.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

**Save:** `Ctrl+X` → `Y` → `Enter`

### 8. Start App

```bash
sudo systemctl daemon-reload
sudo systemctl enable codenex
sudo systemctl start codenex
sudo journalctl -u codenex -n 20
```

### 9. Setup Nginx

```bash
sudo nano /etc/nginx/sites-available/codenex
```

**Paste:**

```nginx
upstream codenex_backend {
    server 127.0.0.1:7150;
}

server {
    listen 80;
    server_name codenex.live www.codenex.live;
    location ^~ /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }
    location / {
        return 301 https://$host$request_uri;
    }
}

server {
    listen 443 ssl http2;
    server_name codenex.live www.codenex.live;
    ssl_certificate /etc/letsencrypt/live/codenex.live/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/codenex.live/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    client_max_body_size 100M;
    location / {
        proxy_pass http://codenex_backend;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

**Save:** `Ctrl+X` → `Y` → `Enter`

### 10. Enable Nginx

```bash
sudo mkdir -p /var/www/certbot
sudo ln -sf /etc/nginx/sites-available/codenex /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

### 11. Get SSL

```bash
sudo certbot --nginx -d codenex.live -d www.codenex.live
```

### 12. Done!

```bash
curl https://codenex.live/health
```

---

## 🔄 Update Production (After Git Push)

**On Windows:** Make changes → Commit → Push to GitHub

**On Linux server:**

```bash
cd /tmp
rm -rf Codenex codenex-build
git clone https://github.com/ayyaz081/Codenex.git
cd Codenex
sudo cp /var/www/codenex/.env /tmp/.env.backup
sudo systemctl stop codenex
dotnet publish CodeNex.csproj -c Release -o /tmp/codenex-build --self-contained false
sudo rm -rf /var/www/codenex/*
sudo cp -r /tmp/codenex-build/* /var/www/codenex/
sudo cp /tmp/.env.backup /var/www/codenex/.env
sudo chown -R www-data:www-data /var/www/codenex
sudo chmod 755 /var/www/codenex
sudo chmod 600 /var/www/codenex/.env
sudo systemctl start codenex
cd /tmp
rm -rf Codenex codenex-build
```

---

## 📝 Common Commands

```bash
# View logs
sudo journalctl -u codenex -f

# Restart app
sudo systemctl restart codenex

# Check status
sudo systemctl status codenex

# Edit .env
sudo nano /var/www/codenex/.env
sudo systemctl restart codenex

# Test app
curl http://localhost:7150/health
```

---

## 🎯 Workflow

1. **Develop on Windows** (Visual Studio 2022)
2. **Commit & Push** to GitHub
3. **Run update script** on Linux server
4. **Done!**

**Live at:** `https://codenex.live` 🚀
