# Same Domain Deployment Guide

Perfect! Hosting frontend and backend together is the **simplest and most common** approach. This guide shows you exactly how to set this up.

## 🏗️ **Architecture Overview**

```
Your Domain: https://yoursite.com
├── / (Root)           → Frontend (React/Vue/Angular)
├── /about             → Frontend routes  
├── /contact           → Frontend routes
├── /api/              → Backend API endpoints
├── /api/auth/login    → Backend authentication
├── /api/products      → Backend data endpoints
└── /health            → Backend health checks
```

## 🎯 **Benefits of Same Domain Hosting**

✅ **Simpler Configuration** - No CORS complexity  
✅ **Easier SSL Setup** - Single certificate for everything  
✅ **Better SEO** - Everything under one domain  
✅ **Lower Cost** - Single hosting service  
✅ **Easier Deployment** - Deploy once, everything works  
✅ **No Cross-Domain Issues** - Frontend can call API directly  

## 📋 **Your Environment Variables (Super Simple!)**

Just replace `yoursite.com` with your actual domain:

```bash
# 🔐 SECURITY
JWT_KEY=gD73HWcT/5WUpQGXin4O7FeVxf8KDfLEYhOFvjm+FjY=  # Already generated!
ADMIN_EMAIL=admin@yoursite.com                           # Your email
ADMIN_PASSWORD=YourSecurePassword123!                    # Your password

# 🌐 SAME DOMAIN SETUP (Replace yoursite.com with your domain)
FRONTEND_BASE_URL=https://yoursite.com
API_BASE_URL=https://yoursite.com/api  
JWT_ISSUER=https://yoursite.com
JWT_AUDIENCE=https://yoursite.com
CORS_ALLOWED_ORIGINS=https://yoursite.com,https://www.yoursite.com

# 📧 EMAIL (Gmail setup)
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USERNAME=your.email@gmail.com
EMAIL_PASSWORD=your-gmail-app-password   # From Gmail App Passwords
EMAIL_FROM=your.email@gmail.com
```

That's it! **Much simpler** than separate domains.

## 🚀 **Deployment Options**

### **Option 1: Azure App Service (Recommended)**

Perfect for same-domain hosting:

```bash
# Deploy both frontend and backend to single Azure App Service
az webapp create --name yoursite --resource-group portfolio-rg

# Set environment variables
az webapp config appsettings set \
  --name yoursite \
  --resource-group portfolio-rg \
  --settings \
    FRONTEND_BASE_URL="https://yoursite.azurewebsites.net" \
    API_BASE_URL="https://yoursite.azurewebsites.net/api" \
    JWT_ISSUER="https://yoursite.azurewebsites.net" \
    CORS_ALLOWED_ORIGINS="https://yoursite.azurewebsites.net"
```

### **Option 2: Single VPS/Server**

Deploy everything to one server with nginx:

```nginx
# nginx configuration
server {
    server_name yoursite.com;
    
    # Frontend (React build)
    location / {
        root /var/www/frontend/build;
        try_files $uri $uri/ /index.html;
    }
    
    # Backend API
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    # Backend health checks
    location /health {
        proxy_pass http://localhost:5000;
    }
}
```

### **Option 3: Docker Compose (Development/Testing)**

```yaml
version: '3.8'
services:
  portfolio-app:
    build: .
    ports:
      - "80:80"
      - "443:443"
    environment:
      - FRONTEND_BASE_URL=https://localhost
      - API_BASE_URL=https://localhost/api
      - CORS_ALLOWED_ORIGINS=https://localhost
    volumes:
      - ./frontend/build:/app/wwwroot
```

## 🌍 **Real-World Examples**

### **Custom Domain**
```bash
FRONTEND_BASE_URL=https://johnsmith.dev
API_BASE_URL=https://johnsmith.dev/api
CORS_ALLOWED_ORIGINS=https://johnsmith.dev,https://www.johnsmith.dev
```

### **Azure App Service**
```bash
FRONTEND_BASE_URL=https://johnsmith.azurewebsites.net
API_BASE_URL=https://johnsmith.azurewebsites.net/api
CORS_ALLOWED_ORIGINS=https://johnsmith.azurewebsites.net
```

### **Heroku**
```bash
FRONTEND_BASE_URL=https://johnsmith-portfolio.herokuapp.com
API_BASE_URL=https://johnsmith-portfolio.herokuapp.com/api
CORS_ALLOWED_ORIGINS=https://johnsmith-portfolio.herokuapp.com
```

### **Netlify + Functions**
```bash
FRONTEND_BASE_URL=https://johnsmith.netlify.app
API_BASE_URL=https://johnsmith.netlify.app/.netlify/functions
CORS_ALLOWED_ORIGINS=https://johnsmith.netlify.app
```

## 🔧 **Frontend Configuration**

Since you're on the same domain, your frontend API calls are super simple:

### **React Example**
```javascript
// No CORS issues! Same domain
const API_BASE = '/api';  // Relative path works!

// Login
const login = async (email, password) => {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });
  return response.json();
};

// Get products
const getProducts = async () => {
  const response = await fetch(`${API_BASE}/products`);
  return response.json();
};
```

### **Environment Variables for Frontend**
```bash
# .env (React)
REACT_APP_API_URL=/api

# .env (Vue)
VUE_APP_API_URL=/api

# .env (Angular)
NG_APP_API_URL=/api
```

## 📁 **Project Structure**

```
Portfolio/
├── PortfolioBackend/          # Your .NET backend
│   ├── Controllers/
│   ├── Models/
│   ├── wwwroot/              # Where frontend build goes
│   └── Program.cs
├── PortfolioFrontend/         # Your frontend (React/Vue/Angular)
│   ├── src/
│   ├── public/
│   └── package.json
└── .env                       # Same-domain environment variables
```

## 🚀 **Build & Deploy Process**

### **Step 1: Build Frontend**
```bash
# React
cd PortfolioFrontend
npm run build
cp -r build/* ../PortfolioBackend/wwwroot/

# Vue  
npm run build
cp -r dist/* ../PortfolioBackend/wwwroot/

# Angular
ng build --prod
cp -r dist/* ../PortfolioBackend/wwwroot/
```

### **Step 2: Deploy Backend (with Frontend files)**
```bash
cd PortfolioBackend
dotnet publish -c Release
# Deploy the published files (which include your frontend)
```

## 🧪 **Local Development**

For local development, you can either:

### **Option A: Develop Separately (Recommended)**
```bash
# Backend (Terminal 1)
cd PortfolioBackend
dotnet run  # Runs on https://localhost:7151

# Frontend (Terminal 2)  
cd PortfolioFrontend
npm start   # Runs on http://localhost:3000

# Use these environment variables for development:
FRONTEND_BASE_URL=http://localhost:3000
API_BASE_URL=https://localhost:7151
CORS_ALLOWED_ORIGINS=http://localhost:3000,https://localhost:7151
```

### **Option B: Build Frontend into Backend**
```bash
# Build frontend into backend's wwwroot
cd PortfolioFrontend
npm run build
cp -r build/* ../PortfolioBackend/wwwroot/

# Run backend (serves both)
cd ../PortfolioBackend  
dotnet run  # Everything on https://localhost:7151

# Use these environment variables:
FRONTEND_BASE_URL=https://localhost:7151
API_BASE_URL=https://localhost:7151/api
CORS_ALLOWED_ORIGINS=https://localhost:7151
```

## ✅ **Quick Setup Checklist**

### **For Local Development:**
1. Copy `.env.same-domain` to `.env`
2. Change `yoursite.com` to `localhost:7151` 
3. Set your `ADMIN_EMAIL` and `ADMIN_PASSWORD`
4. Run: `dotnet run`

### **For Production:**
1. Copy `.env.same-domain` to `.env`
2. Change `yoursite.com` to your actual domain
3. Set up Gmail app password for email
4. Deploy to your hosting service
5. Verify health checks work

## 🔍 **Testing Your Setup**

```bash
# Health checks
curl https://yoursite.com/health
curl https://yoursite.com/health/admin

# API endpoints  
curl https://yoursite.com/api/auth/login

# Frontend should load at root
curl https://yoursite.com/
```

## ⚠️ **Common Issues & Solutions**

### **Issue: API routes conflict with frontend routes**
**Solution**: Use `/api/` prefix for all backend routes (already configured!)

### **Issue: Frontend routing doesn't work**  
**Solution**: Configure fallback route in backend (already done in Program.cs!)

### **Issue: Assets not loading**
**Solution**: Build frontend into `wwwroot` folder

### **Issue: CORS errors locally**
**Solution**: Make sure CORS includes your frontend URL

## 🎯 **Bottom Line**

Same domain hosting is **much simpler**:

- ✅ **1 Domain** to manage instead of 2
- ✅ **No CORS complexity** 
- ✅ **1 SSL Certificate**
- ✅ **1 Deployment** process
- ✅ **Lower hosting costs**
- ✅ **Easier configuration**

Just replace `yoursite.com` with your domain and you're ready to deploy! 🚀
