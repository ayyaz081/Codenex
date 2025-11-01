# SmarterASP.NET Deployment Guide for CodeNex

Complete step-by-step guide for deploying your .NET 8.0 CodeNex application to SmarterASP.NET hosting.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Step 1: Prepare Your Application](#step-1-prepare-your-application)
- [Step 2: Publish Your Application](#step-2-publish-your-application)
- [Step 3: Setup SQL Server Database](#step-3-setup-sql-server-database)
- [Step 4: Configure Application Settings](#step-4-configure-application-settings)
- [Step 5: Upload Files via FTP](#step-5-upload-files-via-ftp)
- [Step 6: Configure Web.config](#step-6-configure-webconfig)
- [Step 7: Set Environment Variables](#step-7-set-environment-variables)
- [Step 8: Test Your Deployment](#step-8-test-your-deployment)
- [Troubleshooting](#troubleshooting)
- [Post-Deployment](#post-deployment)

---

## Prerequisites

### SmarterASP.NET Account Requirements
- ✅ **Hosting Plan**: Any plan supporting .NET 8.0 (check compatibility)
- ✅ **SQL Server Database**: Included in most plans
- ✅ **FTP Access**: Provided in control panel
- ✅ **Custom Domain** (optional): Can use subdomain provided

### Local Requirements
- Visual Studio 2022 or .NET 8.0 SDK
- FTP Client (FileZilla recommended, or VS built-in)
- SQL Server Management Studio (SSMS) or Azure Data Studio

---

## Step 1: Prepare Your Application

### 1.1 Update appsettings.Production.json

Ensure production settings are optimized:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Key": "",
    "Issuer": "CodeNexAPI",
    "Audience": "CodeNexAPI",
    "ExpiryHours": "24"
  }
}
```

### 1.2 Verify web.config

Ensure you have a proper `web.config` (create if missing):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\CodeNex.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ASPNETCORE_URLS" value="http://+:7150" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

---

## Step 2: Publish Your Application

### 2.1 Using Visual Studio

1. **Right-click** on `CodeNex` project → **Publish**
2. Select **Folder** as target
3. Choose folder location: `C:\Publish\CodeNex`
4. Click **Publish**

### 2.2 Using Command Line

```powershell
# Navigate to project directory
cd C:\Users\Az\source\repos\ayyaz081\Codenex

# Publish in Release mode
dotnet publish -c Release -o C:\Publish\CodeNex

# Verify output
dir C:\Publish\CodeNex
```

### 2.3 What Gets Published

Your publish folder should contain:
- `CodeNex.dll`
- `CodeNex.deps.json`
- `CodeNex.runtimeconfig.json`
- `appsettings.json`, `appsettings.Production.json`
- `web.config`
- `wwwroot/` folder (all static files)
- All dependency DLLs

---

## Step 3: Setup SQL Server Database

### 3.1 Create Database in SmarterASP.NET Control Panel

1. **Login** to SmarterASP.NET Control Panel
2. Navigate to **Databases** → **MS SQL**
3. Click **Create New Database**
4. Note down:
   - Database Name: `YourUsername_CodeNexDB`
   - Server: `SQL####.smarterasp.net`
   - Username: `YourUsername_CodeNexDB`
   - Password: [Set a strong password]

### 3.2 Build Connection String

```
Server=SQL####.smarterasp.net;Database=YourUsername_CodeNexDB;User Id=YourUsername_CodeNexDB;Password=YourPassword;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;
```

### 3.3 Test Database Connection

Using SSMS or Azure Data Studio:
1. Connect to `SQL####.smarterasp.net`
2. Use SQL Server Authentication
3. Username: `YourUsername_CodeNexDB`
4. Password: [Your password]
5. Test connection successful

### 3.4 Database Migrations

**Option A: Automatic (Recommended)**
- Migrations run automatically when app starts (already configured in `Program.cs`)

**Option B: Manual Migration**
```powershell
# Update connection string in appsettings.json temporarily
# Then run:
dotnet ef database update --connection "Server=SQL####.smarterasp.net;..."
```

---

## Step 4: Configure Application Settings

### 4.1 Update appsettings.Production.json in Publish Folder

Before uploading, edit `C:\Publish\CodeNex\appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL####.smarterasp.net;Database=YourUsername_CodeNexDB;User Id=YourUsername;Password=YourPassword;Encrypt=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "your-minimum-32-character-secret-key-here",
    "Issuer": "CodeNexAPI",
    "Audience": "CodeNexAPI",
    "ExpiryHours": "24"
  },
  "EmailSettings": {
    "Host": "mail.smarterasp.net",
    "Port": 587,
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "CodeNex",
    "Username": "your-email@yourdomain.com",
    "Password": "your-email-password",
    "EnableSsl": true
  }
}
```

### 4.2 Generate JWT Secret Key

```powershell
# Generate secure key (PowerShell)
$bytes = New-Object Byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

---

## Step 5: Upload Files via FTP

### 5.1 Get FTP Credentials from Control Panel

1. Login to SmarterASP.NET Control Panel
2. Go to **FTP Accounts**
3. Note:
   - FTP Server: `ftp.yourdomain.com` or `ftp####.smarterasp.net`
   - Username: Your hosting username
   - Password: Your hosting password
   - Port: 21 (standard FTP) or 22 (SFTP)

### 5.2 Upload Using FileZilla

1. **Open FileZilla**
2. **Connect**:
   - Host: `ftp.yourdomain.com`
   - Username: [Your username]
   - Password: [Your password]
   - Port: 21

3. **Navigate to Root Directory**:
   - Usually `/` or `/wwwroot/` or `/httpdocs/`

4. **Upload All Files**:
   - Select all files from `C:\Publish\CodeNex`
   - Drag to remote directory
   - **Important**: Upload in BINARY mode (not ASCII)
   - This may take 10-20 minutes depending on file size

### 5.3 Upload Using Visual Studio

1. Right-click project → **Publish**
2. Create new profile → **FTP**
3. Enter FTP details:
   - Server: `ftp://ftp.yourdomain.com`
   - Site path: `/`
   - Username & Password
4. Click **Publish**

### 5.4 Verify Upload

After upload completes, verify these files exist on server:
- `/CodeNex.dll`
- `/web.config`
- `/appsettings.Production.json`
- `/wwwroot/` folder with all static files

---

## Step 6: Configure Web.config

### 6.1 Update Web.config on Server

If not already uploaded, create/edit `web.config` in root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <!-- Enable HTTPS redirect if you have SSL -->
      <rewrite>
        <rules>
          <rule name="HTTPS Redirect" stopProcessing="true">
            <match url="(.*)" />
            <conditions>
              <add input="{HTTPS}" pattern="^OFF$" />
            </conditions>
            <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
          </rule>
        </rules>
      </rewrite>

      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      
      <aspNetCore processPath="dotnet" 
                  arguments=".\CodeNex.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>

      <!-- Increase upload size limits -->
      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="104857600" /> <!-- 100MB -->
        </requestFiltering>
      </security>
    </system.webServer>
  </location>
</configuration>
```

### 6.2 Create Logs Directory

Using FTP client:
1. Create folder: `/logs/`
2. Set permissions to allow write access

---

## Step 7: Set Environment Variables

### 7.1 Using SmarterASP.NET Control Panel

1. Login to Control Panel
2. Go to **Configuration** → **Application Settings** (if available)
3. Add these variables:

```
DATABASE_CONNECTION_STRING = [Your SQL connection string]
JWT_KEY = [Your 32+ character secret]
JWT_ISSUER = CodeNexAPI
JWT_AUDIENCE = CodeNexAPI
JWT_EXPIRY_HOURS = 24
ADMIN_EMAIL = admin@yourdomain.com
ADMIN_PASSWORD = [Strong password]
API_BASE_URL = https://yourdomain.com
ASPNETCORE_ENVIRONMENT = Production
```

### 7.2 Alternative: Use appsettings.Production.json

If control panel doesn't support environment variables, all settings should be in `appsettings.Production.json` (already done in Step 4).

---

## Step 8: Test Your Deployment

### 8.1 Access Your Application

1. Open browser
2. Navigate to: `https://yourdomain.com` or `https://yoursubdomain.smarterasp.net`

### 8.2 Verify Health Endpoints

Test these URLs:
- `https://yourdomain.com/health` - Should return JSON with status
- `https://yourdomain.com/health/live` - Should return "Live"
- `https://yourdomain.com/` - Should load homepage

### 8.3 Check Database Migrations

Monitor application startup:
1. Check `/logs/` folder for `stdout` logs
2. Look for migration messages
3. Verify database tables created in SSMS

### 8.4 Test Admin Login

1. Go to `/Admin` or `/Auth`
2. Login with admin credentials from environment variables
3. Should successfully authenticate

### 8.5 Test File Uploads

1. Login as admin
2. Try uploading a product image
3. Verify file saves to `/wwwroot/Uploads/products/`

---

## Troubleshooting

### Issue: 500.19 Error - "Cannot read configuration file"

**Solution**:
- Ensure `web.config` is in root directory
- Check file permissions (should be readable)
- Verify XML is valid (no syntax errors)

### Issue: 500.30 Error - "Failed to start application"

**Solution**:
```xml
<!-- Enable detailed errors in web.config -->
<aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
```
- Check `/logs/stdout_[timestamp].log` for errors
- Common causes:
  - Missing DLL files
  - Database connection failure
  - Invalid JWT configuration

### Issue: Database Connection Failed

**Solutions**:
1. Verify connection string format
2. Check SQL Server firewall allows your IP
3. Test connection using SSMS
4. Ensure TrustServerCertificate=True is set
5. Contact SmarterASP.NET support to whitelist your database IP

### Issue: Static Files (CSS/JS) Not Loading

**Solutions**:
1. Verify `/wwwroot/` folder uploaded correctly
2. Check file permissions (should be readable)
3. Test direct URL: `https://yourdomain.com/css/shared-styles.css`
4. Clear browser cache

### Issue: Uploads Folder Not Writable

**Solution**:
Using FTP client:
1. Right-click `/wwwroot/Uploads/` folder
2. Set permissions to allow write (usually 755 or 775)
3. Create subfolder: `/wwwroot/Uploads/products/`

### Issue: JWT Authentication Not Working

**Solutions**:
1. Ensure JWT_KEY is minimum 32 characters
2. Verify case-sensitive: JWT_KEY vs Jwt:Key
3. Check environment variables are loaded
4. Test with `/health/admin` endpoint

### Issue: Email Not Sending

**Solutions**:
1. Use SmarterASP.NET SMTP: `mail.smarterasp.net`
2. Port: 587 with SSL
3. Use email account created in control panel
4. Check SMTP logs in control panel

### Issue: Application Recycles Frequently

**Causes**:
- Memory limit exceeded
- Too many requests (CPU limit)
- Application pool timeout

**Solutions**:
1. Optimize database queries
2. Enable response caching
3. Contact hosting support to increase limits

---

## Post-Deployment

### 1. Setup Custom Domain

1. **Purchase Domain** (if not done)
2. **Add Domain in Control Panel**:
   - Domains → Add Domain
   - Enter your domain name
   - Update DNS records with your registrar

3. **DNS Configuration**:
   ```
   A Record:  @  →  [SmarterASP.NET IP]
   CNAME:     www  →  yourdomain.com
   ```

### 2. Enable SSL Certificate

1. Go to **SSL Certificates** in control panel
2. Request **Free SSL** (Let's Encrypt)
3. Or upload custom certificate
4. Enable HTTPS redirect in web.config (already configured)

### 3. Configure Email Accounts

1. **Control Panel** → **Email Accounts**
2. Create: `noreply@yourdomain.com`
3. Update `EmailSettings` in appsettings

### 4. Setup Backups

1. **Database Backup**:
   - Control Panel → Databases → Backup
   - Schedule daily backups

2. **File Backup**:
   - Download files via FTP regularly
   - Use version control (Git)

### 5. Monitor Application

1. **Check Logs Regularly**:
   - `/logs/stdout_*.log`
   - Download and review errors

2. **Monitor Health**:
   - Setup uptime monitoring (UptimeRobot, etc.)
   - Monitor `/health` endpoint

3. **Database Monitoring**:
   - Check database size in control panel
   - Optimize slow queries

### 6. Performance Optimization

1. **Enable Response Caching** (already configured)
2. **Use CDN for Static Files** (optional):
   - CloudFlare
   - Azure CDN
   - AWS CloudFront

3. **Optimize Images**:
   - Compress uploads
   - Use WebP format
   - Implement lazy loading

---

## Quick Deployment Checklist

- [ ] Publish application to local folder
- [ ] Create SQL Server database in control panel
- [ ] Update connection string in appsettings.Production.json
- [ ] Generate and set JWT secret key
- [ ] Configure email settings
- [ ] Upload all files via FTP
- [ ] Verify web.config is correct
- [ ] Create /logs/ directory with write permissions
- [ ] Set /wwwroot/Uploads/ folder permissions
- [ ] Test /health endpoint
- [ ] Test admin login
- [ ] Test file upload functionality
- [ ] Configure custom domain (optional)
- [ ] Enable SSL certificate
- [ ] Setup automated backups
- [ ] Configure monitoring

---

## Useful Commands

### PowerShell Commands

```powershell
# Publish application
dotnet publish -c Release -o C:\Publish\CodeNex

# Generate JWT key
$bytes = New-Object Byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)

# Test connection string
dotnet ef database update --connection "Server=SQL####.smarterasp.net;..."
```

### FTP Commands (Command Line)

```cmd
# Connect via FTP
ftp ftp.yourdomain.com

# Login
[username]
[password]

# Upload file
put CodeNex.dll

# Upload directory recursively
mput *.*
```

---

## Support Resources

- **SmarterASP.NET Support**: https://www.smarterasp.net/support
- **Knowledge Base**: https://www.smarterasp.net/kb
- **Live Chat**: Available in control panel
- **ASP.NET Core Hosting Guide**: https://www.smarterasp.net/support/kb/a950/aspnet-core-hosting.aspx

---

## Security Checklist

- [ ] Change default admin password after first login
- [ ] Use strong JWT secret key (32+ characters)
- [ ] Enable HTTPS and force redirect
- [ ] Set secure SMTP credentials
- [ ] Configure CORS properly
- [ ] Enable email confirmation in production
- [ ] Regularly update dependencies
- [ ] Monitor application logs for suspicious activity
- [ ] Keep database credentials secure (don't commit)
- [ ] Enable request rate limiting
- [ ] Use Content Security Policy headers (already configured)

---

**Deployment Time Estimate**: 30-60 minutes (first time)  
**Difficulty**: Medium  
**Framework**: .NET 8.0  
**Hosting**: SmarterASP.NET  
**Database**: SQL Server

Good luck with your deployment! 🚀
