# Portfolio Application

A full-stack portfolio application with ASP.NET Core backend and HTML/CSS/JavaScript frontend.

## Project Structure

```
Portfolio/
├── PortfolioBackend/           # Backend ASP.NET Core project
│   ├── Controllers/           # API controllers
│   ├── Data/                 # Database context and migrations
│   ├── Models/               # Data models
│   ├── Services/             # Business logic services
│   └── Program.cs            # Application startup
├── *.html                    # Frontend HTML pages
├── css/                      # Stylesheets
├── js/                       # JavaScript files
├── components/               # Reusable HTML components
└── content/                  # Static content
```

## Features

- ASP.NET Core 8.0 Backend API
- JWT Authentication
- Entity Framework Core with SQLite
- Email services
- Static file serving
- Production-ready configuration

## Local Development

1. Navigate to the PortfolioBackend directory:
   ```bash
   cd PortfolioBackend
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Access the application at `http://localhost:5000`

## Azure Deployment

This application is configured for Azure App Service deployment:

1. **Environment Variables**: Set the following in Azure App Service configuration:
   - `EMAIL_PASSWORD`: Your email service password
   - `ASPNETCORE_ENVIRONMENT`: Set to `Production`

2. **Database**: The application uses SQLite which will be created automatically

3. **HTTPS**: The application uses Azure's managed HTTPS certificates

4. **Static Files**: Frontend files are served from the project root for optimal performance

## Configuration

- **appsettings.json**: Development configuration
- **appsettings.Production.json**: Production configuration
- **web.config**: IIS/Azure deployment configuration
- **.deployment**: Azure deployment settings

## Changes Made for Cloud Deployment

1. ✅ Removed custom SSL certificate generation service
2. ✅ Removed publishing profiles
3. ✅ Moved frontend files to project root
4. ✅ Updated static file serving configuration
5. ✅ Removed hardcoded URLs and credentials
6. ✅ Configured CORS for cloud deployment
7. ✅ Added Azure deployment configuration files

## Security Notes

- Email passwords should be set via environment variables
- JWT secrets should be configured in production
- Database connection strings are configured for SQLite by default
- HTTPS redirection is handled by Azure App Service

## Troubleshooting

If you encounter issues with Azure deployment:

1. Check the Azure App Service logs
2. Verify environment variables are set correctly  
3. Ensure the database file permissions are correct
4. Check that static files are being served from the correct path
