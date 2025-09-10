# Azure Web App Deployment Fix Guide

## 🚨 **Issues Fixed**
- ✅ **web.config DLL name**: `PortfolioBackend.dll` → `CodeNex.dll`
- ✅ **JWT fallback values**: `PortfolioAPI` → `CodeNexAPI`
- ✅ **Enabled detailed logging**: `stdoutLogEnabled="true"`

## 🔧 **Required Azure Environment Variables**

### **Step 1: Set Environment Variables in Azure Portal**

Go to **Azure Portal → App Service → Configuration → Application settings** and add:

```
DATABASE_CONNECTION_STRING = Server=tcp:codenex.database.windows.net,1433;Initial Catalog=codenex;Persist Security Info=False;User ID=codenex;Password=Az_55270;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

JWT_KEY = 9JbO%[lS>Xw6y:Dr^NIUp_]8W-!On0WF%3dcz>E@)ts>4y2S9}iMYrP{OC(xn#c[

JWT_ISSUER = CodeNexAPI

JWT_AUDIENCE = CodeNexAPI

JWT_EXPIRY_HOURS = 24

ADMIN_EMAIL = admin@codenex.com

ADMIN_PASSWORD = Admin123!@#

EmailSettings__Host = smtp.gmail.com

EmailSettings__Port = 587

EmailSettings__FromEmail = ayyaz081@gmail.com

EmailSettings__FromName = CodeNex Solutions

EmailSettings__Username = ayyaz081@gmail.com

EmailSettings__Password = your_gmail_app_password

EmailSettings__EnableSsl = true

ASPNETCORE_ENVIRONMENT = Production
```

### **Step 2: Alternative - Set via Azure CLI**

```bash
az webapp config appsettings set \
  --resource-group your-resource-group \
  --name codenex \
  --settings \
  DATABASE_CONNECTION_STRING="Server=tcp:codenex.database.windows.net,1433;Initial Catalog=codenex;Persist Security Info=False;User ID=codenex;Password=Az_55270;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
  JWT_KEY="9JbO%[lS>Xw6y:Dr^NIUp_]8W-!On0WF%3dcz>E@)ts>4y2S9}iMYrP{OC(xn#c[" \
  JWT_ISSUER="CodeNexAPI" \
  JWT_AUDIENCE="CodeNexAPI" \
  ASPNETCORE_ENVIRONMENT="Production"
```

## 🔍 **Troubleshooting Steps**

### **Step 1: Check Azure Logs**

**Azure Portal Method:**
1. Go to **App Service → Monitoring → Log stream**
2. Or **App Service → Monitoring → App Service logs**
3. Enable **Application logging (Filesystem)** and **Detailed error messages**

**Kudu Method:**
1. Go to `https://codenex.scm.azurewebsites.net/`
2. **Debug console → CMD**
3. Navigate to `LogFiles\Application`
4. Check recent log files

### **Step 2: Test Application Health**

```bash
# Test health endpoint
curl https://codenex.azurewebsites.net/health

# Test basic API
curl https://codenex.azurewebsites.net/api/auth/users

# Test Swagger
https://codenex.azurewebsites.net/swagger
```

### **Step 3: Check Database Connection**

The app will fail if it can't connect to the database. Verify:

1. **Azure SQL Server firewall rules** allow Azure services
2. **Connection string** is correctly set in Application Settings
3. **Database exists** and is accessible

### **Step 4: Verify Build Output**

Check that the correct files are deployed:
- ✅ `CodeNex.dll` (not PortfolioBackend.dll)
- ✅ `web.config` has correct DLL reference
- ✅ All dependencies present

## 🚀 **Redeploy Steps**

### **Option 1: GitHub Actions (Automatic)**
```bash
# Push your changes
git add .
git commit -m "Fix Azure deployment issues - update DLL name and JWT config"
git push origin master
```

### **Option 2: Manual Deploy via Azure CLI**
```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group your-resource-group \
  --name codenex \
  --src ./publish.zip
```

### **Option 3: Visual Studio Publish**
1. Right-click project → **Publish**
2. Select **Azure** → **Azure App Service (Windows)**
3. Choose your **codenex** app service
4. Click **Publish**

## ⚡ **Quick Fix Checklist**

- [ ] Fixed `web.config` DLL name to `CodeNex.dll`
- [ ] Updated JWT fallback values to `CodeNexAPI`
- [ ] Set all required environment variables in Azure
- [ ] Enabled detailed logging in Azure
- [ ] Redeployed application
- [ ] Checked logs for startup errors
- [ ] Tested `/health` endpoint

## 🔍 **Common Error Messages & Solutions**

### "Could not load file or assembly"
→ **Fix**: Update `web.config` with correct DLL name

### "No connection string found"  
→ **Fix**: Set `DATABASE_CONNECTION_STRING` in Azure Application Settings

### "JWT configuration error"
→ **Fix**: Set `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE` in Azure

### "500 Internal Server Error"
→ **Fix**: Check Azure logs for detailed error information

---

**After making these changes, redeploy and check `https://codenex.azurewebsites.net/health`**
