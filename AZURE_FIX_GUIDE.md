# Azure Web App API Fix Guide

## Problem Identified
Your Azure Web App is successfully deployed but API endpoints are returning HTTP 500 Internal Server Error. The health check works, indicating the app starts correctly, but database-dependent operations are failing.

## Root Causes
1. **SQLite Database Issues**: Azure Web Apps don't provide persistent local storage, so SQLite files get recreated/lost
2. **Database Migration**: Database may not be properly initialized/migrated
3. **Missing Configuration**: Environment variables and connection strings not properly configured

## Solutions

### 1. Fix SQLite Database Path (Immediate Fix)

Update your `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=D:\\home\\site\\wwwroot\\PortfolioDB.sqlite"
  }
}
```

**Note**: Azure Web Apps store application files in `D:\\home\\site\\wwwroot\\`. However, this is still not ideal for production.

### 2. Add Database Initialization Code

Create a database initialization method in `Program.cs` (add before `app.Run()`):

```csharp
// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        context.Database.EnsureCreated();
        // OR use migrations: context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while ensuring the database was created.");
    }
}
```

### 3. Azure App Service Configuration

Add these Application Settings in Azure Portal:

- `ASPNETCORE_ENVIRONMENT` = `Production`
- `WEBSITE_NODE_DEFAULT_VERSION` = `~18`
- `EMAIL_PASSWORD` = `{{your_email_password}}`
- `Jwt__Key` = `{{your_jwt_secret_key}}`
- `Jwt__Issuer` = `CodenexSolutions`
- `Jwt__Audience` = `CodenexSolutions`

### 4. Recommended Long-term Solution: Move to Azure SQL Database

SQLite is not recommended for production Azure Web Apps. Consider:

1. **Azure SQL Database** (recommended)
2. **Azure Database for PostgreSQL**
3. **Azure Cosmos DB**

## Immediate Testing Steps

1. Apply the SQLite path fix
2. Add database initialization code
3. Redeploy the application
4. Run the test script again

## Files to Update

### Update `appsettings.Production.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Error"
    }
  },
  "AllowedHosts": "*",
  "AllowedOrigins": [],
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=D:\\\\home\\\\site\\\\wwwroot\\\\PortfolioDB.sqlite"
  },
  "Security": {
    "RequireHttps": true,
    "UseHSTS": true,
    "HSTSMaxAge": 31536000,
    "UseHttpsRedirection": true
  },
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 465,
    "FromEmail": "ayyaz081@gmail.com",
    "FromName": "Portfolio",
    "Username": "ayyaz081@gmail.com",
    "Password": "",
    "EnableSsl": true
  }
}
```

### Update `Program.cs` (add before `app.Run()`)
```csharp
// Ensure database is created and migrated in production
if (app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Ensuring database is created...");
            context.Database.EnsureCreated();
            logger.LogInformation("Database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while ensuring the database was created.");
            // Don't throw - let the app start and show meaningful error messages
        }
    }
}
```

## Testing Commands

After applying fixes, test with:
```powershell
.\test-api.ps1
```

Expected result: HTTP 200 responses from all API endpoints.
