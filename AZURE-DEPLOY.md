# 🚀 Azure App Service Deployment Guide

**Deploy your .NET 8.0 + Static Frontend app to Azure with minimal configuration**

---

## ✅ **What You Get**

- **Free Tier Available** - Start with F1 (Free) or B1 (Basic) tier
- **Auto HTTPS/SSL** - Free SSL certificate with custom domain
- **Zero Infrastructure** - No server management, Nginx, or systemd
- **Easy Updates** - Deploy via Git, VS, VS Code, or GitHub Actions
- **Built-in Monitoring** - Application Insights integration
- **Auto-scaling** - Scale up/out as needed

---

## 📋 **Prerequisites**

1. **Azure Account** - [Sign up for free](https://azure.microsoft.com/free/) ($200 credit)
2. **Azure SQL Database** (optional) - Or use existing SQL Server
3. **Domain Name** (optional) - For custom domain

---

## 🎯 **Deployment Options**

### **Option 1: Azure Portal (Easiest)**
### **Option 2: Visual Studio 2022**
### **Option 3: Azure CLI**
### **Option 4: GitHub Actions (CI/CD)**

---

## 🔧 **Option 1: Azure Portal Deployment**

### **Step 1: Create Azure App Service**

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **"Create a resource"** → **"Web App"**
3. Configure:
   - **Subscription**: Your subscription
   - **Resource Group**: Create new `codenex-rg`
   - **Name**: `codenex-app` (must be unique - becomes `codenex-app.azurewebsites.net`)
   - **Publish**: **Code**
   - **Runtime stack**: **.NET 8 (LTS)**
   - **Operating System**: **Linux** or **Windows**
   - **Region**: Choose closest to your users
   - **Pricing Plan**: 
     - F1 (Free) - For testing
     - B1 (Basic) - $13/month - Recommended for production
     - P1V2 (Premium) - $73/month - For high traffic

4. Click **"Review + Create"** → **"Create"**

### **Step 2: Configure Environment Variables**

1. Go to your App Service → **Configuration** → **Application Settings**
2. Click **"New application setting"** and add these:

```plaintext
ASPNETCORE_ENVIRONMENT = Azure
DATABASE_CONNECTION_STRING = Server=tcp:YOUR-SERVER.database.windows.net,1433;Database=codenex;User ID=YOUR-USER;Password=YOUR-PASSWORD;Encrypt=True;
ADMIN_EMAIL = admin@yourdomain.com
ADMIN_PASSWORD = YourStrongPassword123!
JWT_KEY = your-secret-jwt-key-at-least-32-characters-long-recommended-64
JWT_ISSUER = CodeNexAPI
JWT_AUDIENCE = CodeNexAPI
JWT_EXPIRY_HOURS = 24
EmailSettings__Host = smtp.gmail.com
EmailSettings__Port = 587
EmailSettings__FromEmail = your-email@gmail.com
EmailSettings__FromName = CodeNex Solutions
EmailSettings__Username = your-email@gmail.com
EmailSettings__Password = your-app-password
EmailSettings__EnableSsl = true
API_BASE_URL = https://codenex-app.azurewebsites.net
FRONTEND_URL = https://codenex-app.azurewebsites.net
REQUIRE_EMAIL_CONFIRMATION = true
```

3. Click **"Save"** at the top

### **Step 3: Deploy Your Code**

#### **Method A: Direct ZIP Deploy (Fastest)**

**On Windows (PowerShell):**

```powershell
# Navigate to your project
cd C:\Users\Az\source\repos\ayyaz081\Codenex

# Build and publish
dotnet publish CodeNex.csproj -c Release -o .\publish

# Create ZIP file
Compress-Archive -Path .\publish\* -DestinationPath .\deploy.zip -Force

# Deploy to Azure (install Azure CLI first if needed)
az webapp deploy --resource-group codenex-rg --name codenex-app --src-path .\deploy.zip --type zip
```

#### **Method B: Git Deployment**

1. In Azure Portal → Your App Service → **Deployment Center**
2. Select **"GitHub"** or **"Local Git"**
3. Authorize and select your repository
4. Azure will build and deploy automatically on push

#### **Method C: FTP/FTPS**

1. In Azure Portal → Your App Service → **Deployment Center** → **FTPS credentials**
2. Use FileZilla or WinSCP to upload the `publish` folder

### **Step 4: Verify Deployment**

```bash
# Check health
curl https://codenex-app.azurewebsites.net/health

# Check admin status
curl https://codenex-app.azurewebsites.net/health/admin

# Open in browser
https://codenex-app.azurewebsites.net
```

---

## 💻 **Option 2: Visual Studio 2022 Deployment**

1. **Right-click** on `CodeNex` project → **Publish**
2. Select **"Azure"** → **Next**
3. Select **"Azure App Service (Windows)"** or **(Linux)** → **Next**
4. Sign in to Azure
5. Select existing App Service or **"Create New"**
6. Click **"Finish"** → **"Publish"**

**Done!** VS will build and deploy automatically.

---

## 🖥️ **Option 3: Azure CLI Deployment**

### **Install Azure CLI**

**Windows:**
```powershell
winget install Microsoft.AzureCLI
```

**Or download:** https://aka.ms/installazurecliwindows

### **Deploy Script**

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="codenex-rg"
APP_NAME="codenex-app"
LOCATION="eastus"
PLAN_NAME="codenex-plan"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan (B1 tier)
az appservice plan create --name $PLAN_NAME --resource-group $RESOURCE_GROUP --sku B1 --is-linux

# Create Web App
az webapp create --resource-group $RESOURCE_GROUP --plan $PLAN_NAME --name $APP_NAME --runtime "DOTNET|8.0"

# Configure environment variables
az webapp config appsettings set --resource-group $RESOURCE_GROUP --name $APP_NAME --settings \
  ASPNETCORE_ENVIRONMENT="Azure" \
  DATABASE_CONNECTION_STRING="YourConnectionString" \
  ADMIN_EMAIL="admin@yourdomain.com" \
  ADMIN_PASSWORD="YourPassword" \
  JWT_KEY="your-64-char-jwt-key"

# Build and deploy
cd C:\Users\Az\source\repos\ayyaz081\Codenex
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
az webapp deploy --resource-group $RESOURCE_GROUP --name $APP_NAME --src-path ../deploy.zip --type zip
```

---

## 🤖 **Option 4: GitHub Actions (CI/CD)**

### **Create `.github/workflows/azure-deploy.yml`**

I'll create this file for you:

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: codenex-app
  AZURE_WEBAPP_PACKAGE_PATH: '.'
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore CodeNex.csproj
    
    - name: Build
      run: dotnet build CodeNex.csproj -c Release --no-restore
    
    - name: Publish
      run: dotnet publish CodeNex.csproj -c Release -o ./publish --no-build
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### **Setup GitHub Secrets**

1. In Azure Portal → Your App Service → **Get publish profile**
2. Copy the XML content
3. In GitHub → Your Repo → **Settings** → **Secrets and variables** → **Actions**
4. Create new secret: `AZURE_WEBAPP_PUBLISH_PROFILE` (paste XML)

**Now every push to `main` auto-deploys!** 🎉

---

## 🗄️ **Database Setup**

### **Option A: Azure SQL Database**

```bash
# Create Azure SQL Server
az sql server create --name codenex-sql --resource-group codenex-rg --location eastus --admin-user sqladmin --admin-password YourPassword123!

# Create database
az sql db create --resource-group codenex-rg --server codenex-sql --name codenex --service-objective Basic

# Allow Azure services
az sql server firewall-rule create --resource-group codenex-rg --server codenex-sql --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# Get connection string
az sql db show-connection-string --server codenex-sql --name codenex --client ado.net
```

### **Option B: Use Existing SQL Server**

Just update the connection string in App Service configuration.

---

## 🌐 **Custom Domain & SSL**

### **Add Custom Domain**

1. Azure Portal → Your App Service → **Custom domains**
2. Click **"Add custom domain"**
3. Enter your domain (e.g., `codenex.live`)
4. Add DNS records as instructed:
   - **A Record** or **CNAME** to App Service IP/URL
   - **TXT Record** for verification
5. Click **"Validate"** → **"Add"**

### **Enable SSL**

1. Azure Portal → Your App Service → **TLS/SSL settings**
2. **Private Key Certificates** → **Create App Service Managed Certificate**
3. Select your custom domain
4. **Free SSL certificate** is auto-created and renewed!

---

## 📊 **Monitoring & Logging**

### **View Logs**

**Azure Portal:**
1. Your App Service → **Log stream** (real-time logs)
2. **Monitoring** → **Logs** (query logs)

**Azure CLI:**
```bash
az webapp log tail --resource-group codenex-rg --name codenex-app
```

### **Enable Application Insights** (Recommended)

1. Azure Portal → Your App Service → **Application Insights**
2. Click **"Turn on Application Insights"**
3. **Create new** or select existing
4. Get detailed performance metrics, errors, dependencies

---

## 🔄 **Update Workflow**

### **Manual Update (Portal/CLI)**

```powershell
# On Windows - Build and deploy
cd C:\Users\Az\source\repos\ayyaz081\Codenex
git pull
dotnet publish -c Release -o .\publish
Compress-Archive -Path .\publish\* -DestinationPath .\deploy.zip -Force
az webapp deploy --resource-group codenex-rg --name codenex-app --src-path .\deploy.zip --type zip
```

### **Automatic Update (GitHub Actions)**

1. Make changes locally
2. Commit and push to GitHub
3. **GitHub Actions auto-deploys to Azure** ✅

---

## 💰 **Cost Estimates**

| Tier | Price/Month | RAM | Storage | Features |
|------|-------------|-----|---------|----------|
| **F1 (Free)** | $0 | 1 GB | 1 GB | Good for testing |
| **B1 (Basic)** | ~$13 | 1.75 GB | 10 GB | **Recommended** for production |
| **P1V2 (Premium)** | ~$73 | 3.5 GB | 250 GB | High traffic, auto-scaling |

**Azure SQL Database:**
- Basic: $5/month (2GB)
- Standard S0: $15/month (250GB)

---

## ⚖️ **Azure vs Manual Linux Deployment**

| Feature | Azure App Service | Manual Linux |
|---------|------------------|--------------|
| **Setup Time** | 10 minutes | 1-2 hours |
| **SSL/HTTPS** | Auto (free) | Manual (Certbot) |
| **Server Management** | None | Full (Nginx, systemd) |
| **Scaling** | Click to scale | Manual setup |
| **Monitoring** | Built-in | DIY (logs) |
| **Updates** | Git/CI/CD | SSH + scripts |
| **Cost** | $13/month+ | $5-20/month VPS |
| **Reliability** | 99.95% SLA | Depends on VPS |

---

## 🚨 **Important: No Breaking Changes**

✅ **Your existing Linux deployment still works!**  
✅ **wwwroot files are served automatically**  
✅ **Environment variables replace .env**  
✅ **Health checks work out of the box**  
✅ **No code changes required**

---

## 🔑 **Key Differences from Linux**

1. **.env file** → **Azure App Configuration** (environment variables)
2. **systemd service** → **Azure manages process**
3. **Nginx** → **Built-in IIS/Kestrel**
4. **Certbot SSL** → **Free managed SSL**
5. **SSH updates** → **Git push or CI/CD**

---

## 📝 **Quick Checklist**

- [ ] Create Azure App Service
- [ ] Configure environment variables (DATABASE_CONNECTION_STRING, JWT_KEY, etc.)
- [ ] Deploy code (ZIP, Git, or VS)
- [ ] Test `/health` endpoint
- [ ] Configure custom domain (optional)
- [ ] Enable SSL certificate
- [ ] Setup GitHub Actions for CI/CD (optional)
- [ ] Enable Application Insights monitoring
- [ ] Update DNS records (if using custom domain)

---

## 🎉 **Next Steps**

1. **Test deployment locally first** to ensure everything works
2. **Start with Free tier** to test Azure deployment
3. **Upgrade to B1** when ready for production
4. **Setup GitHub Actions** for automated deployments
5. **Enable monitoring** to track performance

---

## 🆘 **Troubleshooting**

### **App won't start**
```bash
# Check logs
az webapp log tail --resource-group codenex-rg --name codenex-app

# Verify environment variables
az webapp config appsettings list --resource-group codenex-rg --name codenex-app
```

### **Database connection fails**
- Check connection string format
- Verify firewall rules allow Azure services
- Test connection from Azure Cloud Shell

### **Static files not loading**
- Verify `wwwroot` folder is included in publish
- Check `web.config` is present
- Ensure `UseStaticFiles()` is in Program.cs ✅

---

**Your app is already Azure-ready! No code changes needed.** 🚀

**Questions?** Check Azure documentation or open an issue.
