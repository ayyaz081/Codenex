# CodeNex Solutions

.NET 8 Web API for CodeNex Solutions platform with JWT authentication, email services, and admin management.

## Environment Setup

### 1. Database Configuration

The application requires a database connection string. Copy `.env.example` to `.env` and configure:

```bash
cp .env.example .env
```

Edit `.env` and set your database connection:

```env
DATABASE_CONNECTION_STRING=Server=your_server;Database=your_database;User Id=your_username;Password=your_password;TrustServerCertificate=true;
```

**Supported Databases:**
- SQL Server (Azure SQL Database, SQL Server Express, LocalDB)
- Connection string examples:
  - SQL Server: `Server=localhost;Database=CodeNex;Integrated Security=true;TrustServerCertificate=true;`
  - Azure SQL: `Server=tcp:codenex.database.windows.net,1433;Initial Catalog=codenex;User ID=username;Password=password;Encrypt=True;`

### 2. Admin User Configuration

Set admin credentials in `.env`:

```env
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=YourSecurePassword123!
```

The admin user will be automatically created on startup and can login via `/api/auth/login` with JWT tokens.

### 3. Email Service Configuration

For email functionality (user verification, password reset), configure SMTP:

```env
EmailSettings__Host=smtp.gmail.com
EmailSettings__Port=587
EmailSettings__FromEmail=your_email@gmail.com
EmailSettings__FromName=CodeNex Solutions - Your Name
EmailSettings__Username=your_email@gmail.com
EmailSettings__Password=your_gmail_app_password
EmailSettings__EnableSsl=true
```

**Gmail Setup:**
1. Enable 2-Factor Authentication
2. Generate App Password (not your regular password)
3. Use the App Password in `EmailSettings__Password`

### 4. JWT Configuration

Set a secure JWT secret key:

```env
JWT_KEY=your-256-bit-secret-jwt-key-replace-this-in-production
```

## Running the Application

### Option 1: Using PowerShell Script (Recommended)

```powershell
# Load environment variables from .env
. .\load-env.ps1

# Run the application
dotnet run
```

### Option 2: Manual Environment Variables

Set environment variables manually in PowerShell:

```powershell
$env:DATABASE_CONNECTION_STRING = "your_connection_string"
$env:ADMIN_EMAIL = "admin@example.com"
$env:ADMIN_PASSWORD = "YourPassword123!"
# ... other variables

dotnet run
```

### Option 3: Using setx (Persistent)

```powershell
setx DATABASE_CONNECTION_STRING "your_connection_string"
setx ADMIN_EMAIL "admin@example.com"
# ... other variables
# Restart terminal, then:
dotnet run
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login (returns JWT token)
- `POST /api/auth/register` - Register new user
- `POST /api/auth/logout` - Logout
- `GET /api/auth/profile` - Get user profile (requires auth)

### Admin Features (requires Admin role)
- `GET /api/auth/users` - List all users
- `PUT /api/auth/users/{id}` - Update user
- `DELETE /api/auth/users/{id}` - Delete user
- `POST /api/auth/test-email` - Test email functionality

### Health Checks
- `GET /health` - Detailed health status
- `GET /health/admin` - Admin user status

## Database Migrations

Migrations are automatically applied on startup. The application uses Entity Framework Core with SQL Server.

## Development vs Production

- **Development**: Uses `appsettings.Development.json` + environment variables
- **Production**: Uses `appsettings.Production.json` + environment variables
- Environment variables always take precedence over appsettings

## Security Notes

- Never commit `.env` files (protected by `.gitignore`)
- Use strong passwords and secure JWT keys
- Enable HTTPS in production (handled by reverse proxy/deployment)
- Email passwords should be App Passwords, not regular account passwords

## Troubleshooting

### Database Connection Issues
```bash
# Check if DATABASE_CONNECTION_STRING is set
echo $env:DATABASE_CONNECTION_STRING

# Test connection via health endpoint
curl http://localhost:7150/health
```

### Email Issues
```bash
# Check email configuration via health endpoint
curl http://localhost:7150/health

# Test email sending (as admin)
curl -X POST http://localhost:7150/api/auth/test-email \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'
```

### Admin User Issues
```bash
# Check if admin exists
curl http://localhost:7150/health/admin

# Login as admin
curl -X POST http://localhost:7150/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@yourdomain.com","password":"YourPassword123!"}'
```
