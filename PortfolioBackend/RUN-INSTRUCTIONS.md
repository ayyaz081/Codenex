# How to Run Your Portfolio Application

## ✅ Correct Ways to Run on HTTP:7150

### Method 1: Use Development Profile (Recommended)
```bash
cd C:\Users\Az\source\repos\ayyaz081\Portfolio\PortfolioBackend
dotnet run --launch-profile http
```

### Method 2: Specify URLs Explicitly
```bash
cd C:\Users\Az\source\repos\ayyaz081\Portfolio\PortfolioBackend
dotnet run --urls "http://localhost:7150"
```

### Method 3: Use Environment Variables
```bash
cd C:\Users\Az\source\repos\ayyaz081\Portfolio\PortfolioBackend
$env:ASPNETCORE_URLS="http://localhost:7150"
dotnet run
```

### Method 4: For Production Deployment
```bash
cd C:\Users\Az\source\repos\ayyaz081\Portfolio\PortfolioBackend
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:ASPNETCORE_URLS="http://localhost:7150"
dotnet run
```

## 🔍 Debug Steps

1. **Check if backend is running correctly:**
   - Run one of the methods above
   - You should see: `Now listening on: http://localhost:7150`
   - Open browser to: http://localhost:7150

2. **Test backend connection:**
   - Go to: http://localhost:7150/debug-backend.html
   - This will show you environment detection and API connectivity

3. **Check health endpoint:**
   - Go to: http://localhost:7150/health
   - Should return JSON with status information

4. **Test API directly:**
   - Go to: http://localhost:7150/api/solutions
   - Should return solutions data (may be empty initially)

## ❌ Why Port 5000 Was Used

When you run `dotnet run` without specifying a profile or URLs, ASP.NET Core defaults to:
- Port 5000 for HTTP 
- Port 5001 for HTTPS

This happens because:
1. No launch profile was specified
2. No URLs were configured in the base appsettings.json (now fixed)
3. The application fell back to ASP.NET Core defaults

## ✅ What I Fixed

1. **Added URLs to appsettings.json:**
   ```json
   "Urls": "http://localhost:7150"
   ```

2. **Added URLs to appsettings.Production.json:**
   ```json
   "Urls": "http://localhost:7150"
   ```

3. **Improved frontend detection:**
   - Added port 5000 to development detection
   - Made localhost detection more robust
   - Frontend will always try HTTP:7150 for localhost

4. **Created debug page:**
   - http://localhost:7150/debug-backend.html
   - Shows environment detection and API connectivity

## 🌐 Frontend Configuration

The frontend is now configured to:
- **Always use HTTP:7150** when running on localhost (any port)
- **Auto-detect** in production environments
- **Fall back gracefully** if backend is not available

## 🚀 Quick Start

```bash
# 1. Navigate to the backend directory
cd C:\Users\Az\source\repos\ayyaz081\Portfolio\PortfolioBackend

# 2. Run with the correct profile
dotnet run --launch-profile http

# 3. Open in browser
start http://localhost:7150

# 4. Test the debug page
start http://localhost:7150/debug-backend.html
```

## 📋 Verification Checklist

- [ ] Backend starts on http://localhost:7150
- [ ] Health check works: http://localhost:7150/health
- [ ] API works: http://localhost:7150/api/solutions  
- [ ] Debug page shows green status: http://localhost:7150/debug-backend.html
- [ ] Solutions page loads data: http://localhost:7150/solutions.html
- [ ] Admin login works: http://localhost:7150/Admin.html

If any of these fail, check the console output for errors and ensure the backend is running on the correct port.
