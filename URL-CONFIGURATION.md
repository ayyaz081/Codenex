# URL Configuration Guide

This guide explains how URLs are configured dynamically in the Portfolio Backend to avoid hardcoded URLs in production.

## 🔧 **Dynamic URL Configuration**

The application now uses **environment variables** to configure all URLs, making it production-ready for any domain.

## 📋 **Environment Variables for URLs**

### **Core URL Configuration**
```bash
# Frontend domain (where your React/Angular/Vue app is hosted)
FRONTEND_BASE_URL=https://your-frontend-domain.com

# API domain (where this backend is hosted)  
API_BASE_URL=https://your-api-domain.com

# JWT issuer and audience (should match your API domain)
JWT_ISSUER=https://your-api-domain.com
JWT_AUDIENCE=https://your-api-domain.com

# CORS allowed origins (comma-separated, no spaces)
CORS_ALLOWED_ORIGINS=https://your-frontend-domain.com,https://www.your-frontend-domain.com
```

### **Email Link Paths**
```bash
# Email verification page path on your frontend
EMAIL_VERIFICATION_PATH=/auth/verify

# Password reset page path on your frontend  
PASSWORD_RESET_PATH=/auth/reset
```

## 🌍 **How It Works**

### **Email Links Generation**
When users register or request password resets, the backend generates emails with links like:

**Registration Verification:**
```
{FRONTEND_BASE_URL}{EMAIL_VERIFICATION_PATH}?userId=123&token=abc123
# Example: https://yourapp.com/auth/verify?userId=123&token=abc123
```

**Password Reset:**
```
{FRONTEND_BASE_URL}{PASSWORD_RESET_PATH}?userId=123&token=xyz789
# Example: https://yourapp.com/auth/reset?userId=123&token=xyz789
```

### **Fallback Behavior**
If `FRONTEND_BASE_URL` is not set:
- **Development**: Uses `http://localhost:3000` (configurable in launch settings)
- **Production**: Uses the current request's scheme and host (`Request.Scheme://Request.Host`)

## 🚀 **Deployment Examples**

### **Local Development**
```bash
# .env.local or environment variables
FRONTEND_BASE_URL=http://localhost:3000
EMAIL_VERIFICATION_PATH=/auth/verify
PASSWORD_RESET_PATH=/auth/reset
```

### **Production - Same Domain**
```bash
# Frontend and backend on same domain
FRONTEND_BASE_URL=https://myportfolio.com
API_BASE_URL=https://myportfolio.com/api
```

### **Production - Separate Domains**
```bash
# Frontend and backend on different domains
FRONTEND_BASE_URL=https://myportfolio.com
API_BASE_URL=https://api.myportfolio.com
CORS_ALLOWED_ORIGINS=https://myportfolio.com,https://www.myportfolio.com
```

### **Production - CDN/Subdomain Setup**
```bash
# Frontend on CDN, API on subdomain
FRONTEND_BASE_URL=https://app.myportfolio.com
API_BASE_URL=https://api.myportfolio.com
JWT_ISSUER=https://api.myportfolio.com
JWT_AUDIENCE=https://api.myportfolio.com
```

## 🐳 **Docker Configuration**

### **docker-compose.yml**
```yaml
environment:
  - FRONTEND_BASE_URL=https://yourapp.com
  - API_BASE_URL=https://api.yourapp.com
  - EMAIL_VERIFICATION_PATH=/auth/verify
  - PASSWORD_RESET_PATH=/auth/reset
  - CORS_ALLOWED_ORIGINS=https://yourapp.com,https://www.yourapp.com
```

### **Docker Run Command**
```bash
docker run -d \
  -p 8080:8080 \
  -e FRONTEND_BASE_URL=https://yourapp.com \
  -e API_BASE_URL=https://api.yourapp.com \
  -e EMAIL_VERIFICATION_PATH=/auth/verify \
  -e PASSWORD_RESET_PATH=/auth/reset \
  portfolio-backend
```

## ☁️ **Cloud Platform Configuration**

### **Azure App Service**
```bash
az webapp config appsettings set \
  --name your-app-name \
  --resource-group your-rg \
  --settings \
    FRONTEND_BASE_URL="https://yourapp.com" \
    API_BASE_URL="https://api.yourapp.com" \
    EMAIL_VERIFICATION_PATH="/auth/verify" \
    PASSWORD_RESET_PATH="/auth/reset"
```

### **AWS ECS/Elastic Beanstalk**
```json
{
  "FRONTEND_BASE_URL": "https://yourapp.com",
  "API_BASE_URL": "https://api.yourapp.com", 
  "EMAIL_VERIFICATION_PATH": "/auth/verify",
  "PASSWORD_RESET_PATH": "/auth/reset"
}
```

### **Heroku**
```bash
heroku config:set FRONTEND_BASE_URL=https://yourapp.com
heroku config:set API_BASE_URL=https://api.yourapp.com
heroku config:set EMAIL_VERIFICATION_PATH=/auth/verify
heroku config:set PASSWORD_RESET_PATH=/auth/reset
```

## 🔒 **Security Considerations**

### **CORS Configuration**
```bash
# Specific origins (recommended)
CORS_ALLOWED_ORIGINS=https://yourapp.com,https://www.yourapp.com

# Wildcard (only for public APIs)
CORS_ALLOWED_ORIGINS=*
```

### **JWT Configuration**
```bash
# Should match your API domain
JWT_ISSUER=https://api.yourapp.com
JWT_AUDIENCE=https://api.yourapp.com
```

## 🧪 **Testing URL Configuration**

### **Health Check**
```bash
curl https://your-api-domain.com/health
# Should return status of all configurations including JWT
```

### **Admin Status**
```bash
curl https://your-api-domain.com/health/admin  
# Shows admin user status
```

### **Test Email Generation**
1. Register a new user via API
2. Check logs for generated email verification URL
3. Verify the URL uses your configured `FRONTEND_BASE_URL`

## ⚠️ **Common Issues & Solutions**

### **Issue: Email links point to localhost in production**
**Solution:** Set `FRONTEND_BASE_URL` environment variable

### **Issue: CORS errors in production**
**Solution:** Set `CORS_ALLOWED_ORIGINS` to your frontend domain(s)

### **Issue: JWT validation fails**
**Solution:** Ensure `JWT_ISSUER` and `JWT_AUDIENCE` match your API domain

### **Issue: Email verification/reset doesn't work**
**Solution:** Verify `EMAIL_VERIFICATION_PATH` and `PASSWORD_RESET_PATH` match your frontend routes

## 🔧 **Development vs Production**

| Setting | Development | Production |
|---------|-------------|------------|
| `FRONTEND_BASE_URL` | `http://localhost:3000` | `https://yourapp.com` |
| `API_BASE_URL` | `http://localhost:7150` | `https://api.yourapp.com` |
| `CORS_ALLOWED_ORIGINS` | `http://localhost:3000` | `https://yourapp.com` |
| `EMAIL_VERIFICATION_PATH` | `/auth/verify` | `/auth/verify` |
| `PASSWORD_RESET_PATH` | `/auth/reset` | `/auth/reset` |

## 📝 **Frontend Integration**

Your frontend should handle these routes:

**Email Verification Route:**
```javascript
// /auth/verify?userId=123&token=abc123
app.get('/auth/verify', async (req, res) => {
  const { userId, token } = req.query;
  // Call API: POST /api/auth/verify-email?userId={userId}&token={token}
});
```

**Password Reset Route:**
```javascript
// /auth/reset?userId=123&token=xyz789
app.get('/auth/reset', async (req, res) => {
  const { userId, token } = req.query;
  // Show password reset form
  // Call API: POST /api/auth/reset-password
});
```

## ✅ **Verification Checklist**

Before deploying to production:

- [ ] `FRONTEND_BASE_URL` is set to your frontend domain
- [ ] `API_BASE_URL` is set to your backend domain
- [ ] `CORS_ALLOWED_ORIGINS` includes your frontend domain(s)
- [ ] `JWT_ISSUER` and `JWT_AUDIENCE` are set correctly
- [ ] `EMAIL_VERIFICATION_PATH` matches your frontend route
- [ ] `PASSWORD_RESET_PATH` matches your frontend route
- [ ] No hardcoded localhost URLs remain in configuration
- [ ] Frontend handles verification and reset routes properly

Your application is now **fully dynamic** and **production-ready** for any domain configuration! 🎉
