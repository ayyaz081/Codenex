# ⚡ Azure Deployment - Quick Start

**Get your app running on Azure in under 15 minutes**

---

## ✅ **Files Created for You**

Your project now has:
- ✅ `appsettings.Azure.json` - Azure-specific configuration
- ✅ `.deployment` - Azure build configuration
- ✅ `.github/workflows/azure-deploy.yml` - CI/CD pipeline
- ✅ `AZURE-DEPLOY.md` - Complete deployment guide
- ✅ `DEPLOYMENT-COMPARISON.md` - Azure vs Linux comparison
- ✅ `web.config` - Already existed (IIS configuration)

---

## 🚀 **Fastest Way: Visual Studio 2022**

1. **Right-click** `CodeNex` project → **Publish**
2. Select **Azure** → **Azure App Service (Windows/Linux)**
3. Sign in to Azure (or create free account)
4. Click **Create New** App Service
5. Configure:
   - Name: `codenex-app` (unique)
   - Subscription: Your subscription
   - Resource Group: `codenex-rg` (new)
   - Hosting Plan: **B1 Basic** ($13/month) or **F1 Free**
6. Click **Create** → **Publish**

**Done!** Your app is live at `https://codenex-app.azurewebsites.net`

---

## 🔧 **Configure Environment Variables**

After deployment, add these in Azure Portal:

1. Go to [portal.azure.com](https://portal.azure.com)
2. Find your App Service: `codenex-app`
3. Go to **Configuration** → **Application settings**
4. Add these settings:

```plaintext
ASPNETCORE_ENVIRONMENT = Azure
DATABASE_CONNECTION_STRING = [Your SQL Server connection string]
ADMIN_EMAIL = admin@yourdomain.com
ADMIN_PASSWORD = [Strong password]
JWT_KEY = [64-character random string]
JWT_ISSUER = CodeNexAPI
JWT_AUDIENCE = CodeNexAPI
JWT_EXPIRY_HOURS = 24
EmailSettings__Host = smtp.gmail.com
EmailSettings__Port = 587
EmailSettings__FromEmail = your-email@gmail.com
EmailSettings__Username = your-email@gmail.com
EmailSettings__Password = [Gmail app password]
EmailSettings__EnableSsl = true
API_BASE_URL = https://codenex-app.azurewebsites.net
FRONTEND_URL = https://codenex-app.azurewebsites.net
REQUIRE_EMAIL_CONFIRMATION = true
```

**Save** → App will restart automatically

---

## ✅ **Verify Deployment**

Open browser:
```
https://codenex-app.azurewebsites.net
https://codenex-app.azurewebsites.net/health
https://codenex-app.azurewebsites.net/swagger
```

---

## 🤖 **Setup Auto-Deploy (Optional)**

Enable GitHub Actions for auto-deployment:

1. In Azure Portal → Your App Service → **Deployment Center**
2. Select **GitHub**
3. Authorize GitHub
4. Select your repo: `ayyaz081/Codenex`
5. Branch: `main` or `master`
6. Click **Save**

**Now every push to GitHub auto-deploys to Azure!** 🎉

Or use the workflow file already created in `.github/workflows/azure-deploy.yml`:
1. Get publish profile from Azure Portal (Download publish profile)
2. Add it to GitHub Secrets as `AZURE_WEBAPP_PUBLISH_PROFILE`
3. Update `AZURE_WEBAPP_NAME` in workflow file
4. Push to trigger deployment

---

## 💰 **Pricing Options**

| Tier | Cost | Best For |
|------|------|----------|
| **F1 (Free)** | $0/month | Testing only (60 min/day) |
| **B1 (Basic)** | ~$13/month | **Production** (recommended) |
| **S1 (Standard)** | ~$70/month | High traffic + staging slots |
| **P1V2 (Premium)** | ~$73/month | Enterprise + auto-scaling |

**Start with F1 Free to test, then upgrade to B1 for production.**

---

## 🌐 **Add Custom Domain (Optional)**

1. Azure Portal → Your App Service → **Custom domains**
2. Click **Add custom domain**
3. Enter: `codenex.live` (or your domain)
4. Follow DNS instructions (add CNAME record)
5. Validate → Add
6. Go to **TLS/SSL settings** → **Create certificate** (FREE!)

**Free SSL included!**

---

## 📊 **What's Different from Linux?**

| Feature | Linux | Azure |
|---------|-------|-------|
| **Environment** | `.env` file | App Settings (portal) |
| **Web Server** | Nginx | IIS/Kestrel (built-in) |
| **Process Manager** | systemd | Platform managed |
| **SSL** | Certbot (manual) | Managed (automatic) |
| **Deployment** | SSH + git pull | Git push / VS / CLI |
| **Logs** | journalctl | Log Stream / App Insights |
| **Updates** | SSH + scripts | Push code |

---

## 🚨 **Important Notes**

### **✅ No Code Changes Required**
Your app works as-is! The following already work:
- Static files in `wwwroot` ✅
- API endpoints ✅
- Health checks ✅
- JWT authentication ✅
- Database migrations ✅
- Email service ✅

### **✅ Your Linux Deployment Still Works**
Deploying to Azure doesn't break your Linux setup. You can:
- Keep Linux as primary
- Use Azure as secondary
- Or switch entirely
- Run both simultaneously!

---

## 🔄 **Update Your App**

### **Method 1: Visual Studio**
1. Make changes
2. Right-click project → **Publish**
3. Click **Publish** button

### **Method 2: PowerShell/CLI**
```powershell
cd C:\Users\Az\source\repos\ayyaz081\Codenex
dotnet publish -c Release -o .\publish
Compress-Archive -Path .\publish\* -DestinationPath .\deploy.zip -Force
az webapp deploy --resource-group codenex-rg --name codenex-app --src-path .\deploy.zip
```

### **Method 3: GitHub Actions**
```bash
git add .
git commit -m "Update"
git push
# Auto-deploys!
```

---

## 📖 **More Information**

- **Full Guide:** Read `AZURE-DEPLOY.md`
- **Comparison:** Read `DEPLOYMENT-COMPARISON.md`
- **Linux Guide:** `PRODUCTION-DEPLOY.md` (still valid!)

---

## 🆘 **Quick Troubleshooting**

### **App won't start**
1. Check logs: Azure Portal → Log Stream
2. Verify environment variables are set
3. Check connection string format

### **Static files not loading**
1. Ensure `wwwroot` in publish output
2. Check `web.config` exists ✅ (already there)

### **Database connection fails**
1. Check firewall rules allow Azure services
2. Verify connection string format
3. Test from Azure Cloud Shell

---

## ✅ **Next Steps**

1. ☑️ Deploy to Azure (15 minutes)
2. ☑️ Configure environment variables
3. ☑️ Test endpoints (/health, /swagger)
4. ☑️ Setup custom domain (optional)
5. ☑️ Enable GitHub Actions (optional)
6. ☑️ Configure Application Insights (optional)

---

## 🎉 **You're Ready!**

Your .NET 8.0 app with static frontend is **100% Azure-ready** with:
- Zero code changes
- All features working
- Easy deployment
- Free SSL
- Auto-scaling capability

**Start with the free tier and upgrade when ready!** 🚀

---

## 💡 **Pro Tips**

1. **Test locally first** - Ensure app builds and runs
2. **Start with Free tier** - Test Azure deployment
3. **Use staging slots** - Deploy to staging first (S1+ tier)
4. **Enable monitoring** - Application Insights is free up to 5GB/month
5. **Backup .env values** - Store in password manager before migrating

---

**Questions? Check AZURE-DEPLOY.md for detailed instructions!**
