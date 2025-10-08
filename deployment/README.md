# Deployment Files

This folder contains all necessary files and scripts for deploying CodeNex to Ubuntu 22.04.

## 📁 Files Overview

| File | Purpose |
|------|---------|
| `DEPLOYMENT-GUIDE.md` | **START HERE** - Complete step-by-step deployment guide |
| `setup-vm.sh` | Automated VM setup script (installs .NET, Nginx, Certbot, etc.) |
| `deploy.sh` | Automated deployment script (builds, publishes, and deploys app) |
| `codenex.service` | Systemd service configuration |
| `nginx-codenex.conf` | Nginx reverse proxy configuration with SSL |

## 🚀 Quick Start

### 1. Initial VM Setup (One Time)
```bash
# Upload and run on fresh Ubuntu 22.04 VM
scp deployment/setup-vm.sh user@your-vm-ip:~/
ssh user@your-vm-ip
chmod +x setup-vm.sh
sudo ./setup-vm.sh
```

### 2. Deploy Application
```bash
# From your project directory on Windows
Compress-Archive -Path * -DestinationPath codenex-deploy.zip
scp codenex-deploy.zip user@your-vm-ip:~/

# On VM
mkdir ~/codenex-source && cd ~/codenex-source
unzip ~/codenex-deploy.zip
chmod +x deployment/deploy.sh
./deployment/deploy.sh
```

### 3. Setup SSL Certificate
```bash
# On VM
sudo certbot --nginx -d codenex.live -d www.codenex.live
```

## 📖 Documentation

**Read the full guide:** [`DEPLOYMENT-GUIDE.md`](./DEPLOYMENT-GUIDE.md)

It includes:
- ✅ VM requirements and specifications
- ✅ Complete installation instructions
- ✅ SSL/TLS certificate setup with Certbot
- ✅ Troubleshooting guide
- ✅ Maintenance and update procedures
- ✅ Security best practices

## 🔧 Key Fix: .env File Loading

The `Program.cs` has been updated to properly load the `.env` file in production:

```csharp
var envFilePath = Path.Combine(AppContext.BaseDirectory, ".env");
if (File.Exists(envFilePath))
{
    Env.Load(envFilePath);
    Console.WriteLine($"Loaded .env file from: {envFilePath}");
}
```

This ensures the `.env` file is read from `/var/www/codenex/.env` when deployed.

## ✅ Verification

After deployment, check that `.env` is being loaded:
```bash
sudo journalctl -u codenex -n 100 | grep ".env"
```

You should see: `Loaded .env file from: /var/www/codenex/.env`

## 🆘 Need Help?

1. Check logs: `sudo journalctl -u codenex -f`
2. Check service: `sudo systemctl status codenex`
3. View full guide: [`DEPLOYMENT-GUIDE.md`](./DEPLOYMENT-GUIDE.md)
