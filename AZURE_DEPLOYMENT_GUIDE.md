# Azure Web App Deployment Guide

## Overview
This guide explains how to properly configure your Portfolio application for Azure Web App deployment, ensuring API calls work correctly in production.

## 🚀 Azure App Service Configuration

### Step 1: Set Up Environment Variables in Azure

In your Azure Web App, go to **Configuration** → **Application settings** and add these environment variables:

#### Required Environment Variables:

```
CORS_ALLOWED_ORIGINS = https://YOUR_APP_NAME.azurewebsites.net
API_BASE_URL = https://YOUR_APP_NAME.azurewebsites.net
EMAIL_PASSWORD = your_gmail_app_password_here
ASPNETCORE_ENVIRONMENT = Production
```

#### Optional but Recommended:

```
Jwt__Key = your-super-secure-jwt-key-at-least-256-bits-long-for-production
Jwt__Issuer = https://YOUR_APP_NAME.azurewebsites.net
Jwt__Audience = https://YOUR_APP_NAME.azurewebsites.net
```

### Step 2: Replace Placeholders

Replace `YOUR_APP_NAME` with your actual Azure Web App name. For example:
- If your app URL is `https://myportfolio-app.azurewebsites.net`
- Then `YOUR_APP_NAME` = `myportfolio-app`

### Step 3: Additional Azure Configuration

1. **Enable HTTPS Only**:
   - Go to **TLS/SSL settings**
   - Turn on **HTTPS Only**

2. **Configure Custom Domain** (Optional):
   - If you have a custom domain, add it in **Custom domains**
   - Update the environment variables to use your custom domain instead

3. **Enable Application Insights** (Recommended):
   - For monitoring and debugging API calls

## 🔧 How It Works

### Frontend Configuration
Your `config.js` file automatically detects the environment and configures API endpoints:

- **Development**: Uses `http://localhost:7150` or `https://localhost:7151`
- **Production**: Uses the current hostname (your Azure Web App URL)
- **Override**: Can be overridden using the `API_BASE_URL` environment variable

### Backend CORS Configuration
Your `Program.cs` file now:

1. **Reads CORS origins** from `CORS_ALLOWED_ORIGINS` environment variable
2. **Falls back** to configuration files if environment variable is not set
3. **Allows any origin** in development for easier testing
4. **Restricts to specific origins** in production for security

### API Base URL Injection
The backend injects the API base URL into your HTML:

1. **Checks** `API_BASE_URL` environment variable first
2. **Falls back** to configuration if not found
3. **Injects** `window.API_BASE_URL` into your HTML before serving it

## 🛠️ Deployment Steps

### Method 1: Visual Studio Publish
1. Right-click your project → **Publish**
2. Choose **Azure** → **Azure App Service (Windows)**
3. Select your subscription and Web App
4. Click **Publish**

### Method 2: GitHub Actions (Recommended)
Your repository should have a `.github/workflows` folder with deployment scripts.

### Method 3: Azure DevOps
Set up a build and release pipeline in Azure DevOps.

## 🐛 Troubleshooting

### API Calls Not Working

1. **Check CORS Configuration**:
   ```bash
   # In browser console, check if you see CORS errors
   # Verify CORS_ALLOWED_ORIGINS environment variable is set correctly
   ```

2. **Verify API Base URL**:
   ```javascript
   // In browser console
   console.log('Environment:', PortfolioConfig.environment);
   console.log('API Base URL:', PortfolioConfig.api.getBaseUrl());
   console.log('Window API_BASE_URL:', window.API_BASE_URL);
   ```

3. **Check Network Tab**:
   - Open browser DevTools → Network tab
   - Try to make an API call
   - Check if the request URL is correct
   - Look for any 404, 401, or CORS errors

### Common Issues

1. **Mixed Content Errors**:
   - Ensure all API calls use HTTPS in production
   - Your app automatically handles this

2. **CORS Errors**:
   - Double-check the `CORS_ALLOWED_ORIGINS` environment variable
   - Ensure it matches your app's URL exactly (including https://)

3. **Database Errors**:
   - Your app creates the SQLite database automatically
   - Check Azure logs for any database creation errors

4. **Email Not Working**:
   - Verify `EMAIL_PASSWORD` environment variable is set
   - Use Gmail App Password, not your regular Gmail password

## 📊 Monitoring

### View Logs
1. Go to your Azure Web App
2. **Monitoring** → **Log stream**
3. Watch for startup logs and errors

### Application Insights
If enabled, you can monitor:
- API response times
- Error rates
- User sessions
- Custom events

## 🔐 Security Best Practices

1. **Environment Variables**: Never commit secrets to source control
2. **HTTPS Only**: Always enable HTTPS-only mode in Azure
3. **CORS**: Only allow your specific domain origins
4. **JWT Keys**: Use strong, randomly generated keys for production

## 📝 Environment Variables Reference

| Variable | Description | Example |
|----------|-------------|---------|
| `CORS_ALLOWED_ORIGINS` | Comma-separated list of allowed origins | `https://myapp.azurewebsites.net` |
| `API_BASE_URL` | Override for API base URL | `https://myapp.azurewebsites.net` |
| `EMAIL_PASSWORD` | Gmail app password for contact forms | `abcd efgh ijkl mnop` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` |
| `Jwt__Key` | JWT signing key | `your-super-secure-key` |
| `Jwt__Issuer` | JWT issuer | `https://myapp.azurewebsites.net` |
| `Jwt__Audience` | JWT audience | `https://myapp.azurewebsites.net` |

## ✅ Final Checklist

- [ ] Azure Web App created
- [ ] Environment variables configured
- [ ] HTTPS-only enabled
- [ ] Application deployed
- [ ] API calls working (test in browser)
- [ ] Contact form working (if applicable)
- [ ] Authentication working (if applicable)
- [ ] All static files loading correctly

---

**Need Help?**
- Check Azure Web App logs
- Use browser DevTools to debug API calls
- Verify environment variables are set correctly
