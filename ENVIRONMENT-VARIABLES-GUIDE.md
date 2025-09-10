# Environment Variables - Complete Setup Guide

This guide shows you **exactly where to get each environment variable** and **how to set them up** for your portfolio backend.

## 📋 **All Environment Variables Explained**

### 🔐 **Security & Authentication Variables**

#### **JWT_KEY** - JWT Secret Key
**What it is**: Secret key used to sign JWT tokens for user authentication.

**How to get it:**
```bash
# Option 1: Generate a secure random key (Recommended)
openssl rand -base64 64

# Option 2: Use online generator
# Visit: https://www.allkeysgenerator.com/Random/Security-Encryption-Key-Generator.aspx
# Select: 256-bit key

# Option 3: PowerShell (Windows)
[System.Web.Security.Membership]::GeneratePassword(64, 10)

# Option 4: Use our setup script
.\setup-admin.ps1 -GeneratePassword
```

**Example:**
```bash
JWT_KEY=aBc123XyZ789PqR456MnO234GhI567UvW890EfG123HiJ456KlM789NoPq234RsT567
```

**⚠️ Important**: 
- Must be at least 32 characters
- Keep this SECRET - never share it
- Use different keys for dev/staging/production

---

#### **JWT_ISSUER & JWT_AUDIENCE** - JWT Claims
**What it is**: Identifies who issued the JWT token and who it's intended for.

**How to get it**: Use your API domain
```bash
# If your API is at api.yoursite.com
JWT_ISSUER=https://api.yoursite.com
JWT_AUDIENCE=https://api.yoursite.com

# If your API is at yoursite.com/api  
JWT_ISSUER=https://yoursite.com
JWT_AUDIENCE=https://yoursite.com

# For local development
JWT_ISSUER=http://localhost:7150
JWT_AUDIENCE=http://localhost:7150
```

---

### 🌐 **Domain & URL Variables**

#### **FRONTEND_BASE_URL** - Your Frontend Domain
**What it is**: The domain where your React/Vue/Angular frontend is hosted.

**How to get it**: This is YOUR frontend domain
```bash
# Examples:
FRONTEND_BASE_URL=https://johnsmith.com           # Custom domain
FRONTEND_BASE_URL=https://johnsmith.netlify.app   # Netlify
FRONTEND_BASE_URL=https://johnsmith.vercel.app    # Vercel  
FRONTEND_BASE_URL=https://johnsmith.github.io     # GitHub Pages
FRONTEND_BASE_URL=https://johnsmith.azurewebsites.net  # Azure Static Web Apps

# For local development
FRONTEND_BASE_URL=http://localhost:3000           # React default
FRONTEND_BASE_URL=http://localhost:5173           # Vite default
FRONTEND_BASE_URL=http://localhost:4200           # Angular default
```

---

#### **API_BASE_URL** - Your Backend Domain  
**What it is**: The domain where this backend API is hosted.

**How to get it**: This depends on where you deploy the backend
```bash
# Examples:
API_BASE_URL=https://api.johnsmith.com            # Custom subdomain
API_BASE_URL=https://johnsmith.com/api            # Same domain, /api path
API_BASE_URL=https://johnsmith-api.azurewebsites.net    # Azure App Service
API_BASE_URL=https://johnsmith-api.herokuapp.com        # Heroku
API_BASE_URL=https://1234567890.execute-api.us-east-1.amazonaws.com  # AWS API Gateway

# For local development
API_BASE_URL=http://localhost:7150               # Your local backend
```

---

#### **CORS_ALLOWED_ORIGINS** - Allowed Frontend Domains
**What it is**: Comma-separated list of domains allowed to make requests to your API.

**How to get it**: Use your frontend domain(s)
```bash
# Single domain
CORS_ALLOWED_ORIGINS=https://johnsmith.com

# Multiple domains (no spaces after commas!)
CORS_ALLOWED_ORIGINS=https://johnsmith.com,https://www.johnsmith.com

# Development + Production
CORS_ALLOWED_ORIGINS=http://localhost:3000,https://johnsmith.com

# All domains (NOT recommended for production)
CORS_ALLOWED_ORIGINS=*
```

---

### 👤 **Admin User Variables**

#### **ADMIN_EMAIL & ADMIN_PASSWORD** - Your Admin Account
**What it is**: Credentials for the admin user that will be created automatically.

**How to get it**: You decide these!
```bash
# Your admin email (use your real email)
ADMIN_EMAIL=your.email@gmail.com
ADMIN_EMAIL=admin@yourcompany.com
ADMIN_EMAIL=john.smith@yoursite.com

# Your admin password (make it secure!)
ADMIN_PASSWORD=MySecureAdminPassword123!
ADMIN_PASSWORD=AdminPass2024@Strong
```

**⚠️ Password Requirements**:
- At least 8 characters
- Contains uppercase letters
- Contains lowercase letters  
- Contains digits
- Special characters recommended

---

#### **ADMIN_FIRST_NAME & ADMIN_LAST_NAME** - Your Name
**What it is**: Your display name for the admin account.

**How to get it**: Use your actual name
```bash
ADMIN_FIRST_NAME=John
ADMIN_LAST_NAME=Smith

ADMIN_FIRST_NAME=Admin
ADMIN_LAST_NAME=User
```

---

### 📧 **Email Service Variables**

#### **EMAIL_HOST, EMAIL_PORT, EMAIL_USERNAME, EMAIL_PASSWORD** - SMTP Settings
**What it is**: Settings to send emails (user registration, password reset, etc.).

#### **Option 1: Gmail (Easiest)**
1. **Enable 2-Factor Authentication** on your Gmail account
2. **Generate App Password**:
   - Go to: Google Account → Security → 2-Step Verification → App passwords
   - Generate password for "Mail"
   - Copy the 16-character password

```bash
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USERNAME=your.email@gmail.com
EMAIL_PASSWORD=abcd efgh ijkl mnop    # The app password from Google
EMAIL_ENABLE_SSL=true
EMAIL_FROM=your.email@gmail.com
EMAIL_FROM_NAME=Your Portfolio
```

#### **Option 2: Outlook/Hotmail**
```bash
EMAIL_HOST=smtp-mail.outlook.com
EMAIL_PORT=587  
EMAIL_USERNAME=your.email@outlook.com
EMAIL_PASSWORD=your-outlook-password
EMAIL_ENABLE_SSL=true
EMAIL_FROM=your.email@outlook.com
EMAIL_FROM_NAME=Your Portfolio
```

#### **Option 3: SendGrid (Professional)**
1. **Sign up**: https://sendgrid.com (free tier: 100 emails/day)
2. **Create API Key**: Settings → API Keys → Create API Key
3. **Verify Domain**: Settings → Sender Authentication

```bash
EMAIL_HOST=smtp.sendgrid.net
EMAIL_PORT=587
EMAIL_USERNAME=apikey
EMAIL_PASSWORD=SG.your-sendgrid-api-key-here
EMAIL_ENABLE_SSL=true
EMAIL_FROM=noreply@yourdomain.com
EMAIL_FROM_NAME=Your Portfolio
```

---

### 🗄️ **Database Variables**

#### **CONNECTION_STRING** - Database Connection
**What it is**: How to connect to your database.

#### **Option 1: SQLite (Default/Development)**
```bash
# Local development
CONNECTION_STRING=Data Source=./wwwroot/App_Data/PortfolioDB.sqlite

# Production with persistent storage
CONNECTION_STRING=Data Source=/app/data/PortfolioDB.sqlite
DATABASE_PROVIDER=sqlite
```

#### **Option 2: Azure SQL Database**
1. **Create Azure SQL Database**
2. **Get connection string** from Azure portal

```bash
CONNECTION_STRING=Server=yourserver.database.windows.net;Database=PortfolioDB;User Id=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
DATABASE_PROVIDER=sqlserver
```

#### **Option 3: PostgreSQL**
```bash
CONNECTION_STRING=Host=yourhost;Database=PortfolioDB;Username=yourusername;Password=yourpassword;SSL Mode=Require;
DATABASE_PROVIDER=postgresql
```

---

### 🎥 **External Services (Optional)**

#### **YOUTUBE_API_KEY & YOUTUBE_CHANNEL_ID**
**What it is**: To display YouTube videos on your portfolio.

**How to get it**:
1. **Go to**: https://console.developers.google.com
2. **Create project** or select existing
3. **Enable YouTube Data API v3**
4. **Create credentials** → API Key
5. **Get Channel ID**: Go to your YouTube channel → View Page Source → Search for "channelId"

```bash
YOUTUBE_API_KEY=AIzaSyABC123xyz789...
YOUTUBE_CHANNEL_ID=UC1234567890abcdef...
```

---

## 🚀 **Where to Set These Variables**

### **Local Development**

#### **Option 1: PowerShell Script (Easiest)**
```bash
# Use our setup script
.\setup-admin.ps1

# Or set manually
[Environment]::SetEnvironmentVariable("JWT_KEY", "your-jwt-key", [EnvironmentVariableTarget]::User)
[Environment]::SetEnvironmentVariable("ADMIN_EMAIL", "admin@yoursite.com", [EnvironmentVariableTarget]::User)
```

#### **Option 2: .env File** 
Create `.env` file in your backend directory:
```bash
JWT_KEY=your-jwt-key-here
ADMIN_EMAIL=admin@yoursite.com
ADMIN_PASSWORD=YourSecurePassword123!
FRONTEND_BASE_URL=http://localhost:3000
# ... add all other variables
```

#### **Option 3: launchSettings.json**
Edit `Properties/launchSettings.json`:
```json
{
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "JWT_KEY": "your-jwt-key",
    "ADMIN_EMAIL": "admin@yoursite.com"
  }
}
```

---

### **Production Deployment**

#### **Azure App Service**
```bash
# Azure CLI
az webapp config appsettings set \
  --name your-app-name \
  --resource-group your-resource-group \
  --settings \
    JWT_KEY="your-jwt-key" \
    ADMIN_EMAIL="admin@yoursite.com" \
    FRONTEND_BASE_URL="https://yoursite.com"

# Or via Azure Portal:
# App Service → Configuration → Application settings → New application setting
```

#### **Heroku**
```bash
# Heroku CLI
heroku config:set JWT_KEY=your-jwt-key
heroku config:set ADMIN_EMAIL=admin@yoursite.com
heroku config:set FRONTEND_BASE_URL=https://yourapp.herokuapp.com

# Or via Heroku Dashboard:
# App → Settings → Config Vars
```

#### **AWS/Docker**
```bash
# Docker run
docker run -e JWT_KEY=your-jwt-key -e ADMIN_EMAIL=admin@yoursite.com your-app

# Docker Compose (edit docker-compose.yml environment section)
# AWS (edit environment variables in your deployment configuration)
```

---

## 📝 **Quick Setup Checklist**

### **1. Required for Basic Setup**
```bash
✅ JWT_KEY=your-generated-secret-key
✅ ADMIN_EMAIL=your-admin@email.com  
✅ ADMIN_PASSWORD=YourSecurePassword123!
✅ FRONTEND_BASE_URL=https://yourfrontend.com
✅ CORS_ALLOWED_ORIGINS=https://yourfrontend.com
```

### **2. Required for Email Features**
```bash
✅ EMAIL_HOST=smtp.gmail.com
✅ EMAIL_PORT=587
✅ EMAIL_USERNAME=your.email@gmail.com
✅ EMAIL_PASSWORD=your-app-password
✅ EMAIL_FROM=your.email@gmail.com
```

### **3. Optional but Recommended**
```bash
□ API_BASE_URL=https://yourapi.com
□ JWT_ISSUER=https://yourapi.com  
□ JWT_AUDIENCE=https://yourapi.com
□ CONNECTION_STRING=your-production-db-connection
```

---

## ⚡ **Quick Start Commands**

### **Generate JWT Key**
```bash
# Windows PowerShell
$bytes = New-Object byte[] 32; (New-Object Random).NextBytes($bytes); [Convert]::ToBase64String($bytes)

# Or online: https://www.allkeysgenerator.com/Random/Security-Encryption-Key-Generator.aspx
```

### **Set Up Gmail App Password**
1. Gmail → Manage Account → Security → 2-Step Verification → App passwords
2. Generate password for "Mail"
3. Use the 16-character password as `EMAIL_PASSWORD`

### **Test Your Setup**
```bash
# After setting variables, test the app
dotnet run

# Check health endpoint
curl http://localhost:7150/health
curl http://localhost:7150/health/admin
```

---

## 🆘 **Common Issues**

### **"JWT key missing" Error**
**Solution**: Set `JWT_KEY` environment variable with at least 32 characters

### **"No admin user found" Error**  
**Solution**: Set `ADMIN_EMAIL` and `ADMIN_PASSWORD` environment variables

### **Email not sending**
**Solution**: Check `EMAIL_HOST`, `EMAIL_USERNAME`, `EMAIL_PASSWORD` are correct

### **CORS errors in frontend**
**Solution**: Set `CORS_ALLOWED_ORIGINS` to your frontend domain

---

**Remember**: 
- 🔐 **Never commit secrets to Git**
- 🔒 **Use different values for dev/staging/production**  
- 📧 **Test email sending in development first**
- 🌐 **Double-check domain names (no typos!)**

Your portfolio backend is now ready for production! 🎉
