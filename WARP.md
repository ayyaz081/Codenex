# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

**CodeNex** is a .NET 8 Web API + SPA application for managing software products, publications, repositories, and user purchases. It features JWT authentication, email verification, payment processing via Stripe, and GitHub repository access management.

## Common Commands

### Build & Run
```powershell
# Build the project
dotnet build

# Run in development mode
dotnet run

# Run with specific environment
$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run

# Publish for production
dotnet publish -c Release -o ./publish
```

### Database Operations
```powershell
# Add a new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# List all migrations
dotnet ef migrations list

# Generate SQL script for specific migration
dotnet ef migrations script
```

### Testing & Diagnostics
```powershell
# Check health endpoint
curl http://localhost:7150/health

# View logs (when running as systemd service on Linux)
sudo journalctl -u codenex -f

# Check database connection
dotnet ef database update --verbose

# Access Swagger UI (when running)
# Navigate to: http://localhost:7150/swagger
```

### Development Workflow
```powershell
# Run with hot reload (development)
dotnet watch run

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

## Architecture Overview

### Application Structure
- **Program.cs**: Main application entry point with complete service configuration, middleware pipeline, health checks, JWT setup, and security headers
- **AppDbContext.cs**: EF Core DbContext with all entity configurations and relationship mappings
- **Controllers/**: RESTful API endpoints (Auth, Products, Publications, Repositories, Users, Payments, etc.)
- **Services/**: Business logic (TokenService, EmailService, GitHubService)
- **Models/**: Entity models extending ASP.NET Identity
- **DTOs/**: Data transfer objects for API requests/responses
- **wwwroot/**: Static files and SPA assets

### Key Architectural Patterns

**Layered Architecture**:
- Controllers handle HTTP concerns and validation
- Services encapsulate business logic
- AppDbContext manages data persistence
- DTOs separate API contracts from domain models

**Authentication & Authorization**:
- ASP.NET Core Identity for user management
- JWT bearer tokens for stateless authentication
- Role-based authorization (Admin, Manager, User)
- Email verification workflow with tokens

**Configuration Hierarchy** (Priority order):
1. Environment variables (highest priority)
2. .env file (via DotNetEnv)
3. appsettings.{Environment}.json
4. appsettings.json (lowest priority)

**Database Architecture**:
- SQL Server only (via Entity Framework Core)
- ASP.NET Identity tables for users/roles
- Core entities: Product, Solution, Publication, Repository, Payment, UserPurchase
- Relationship patterns:
  - Products → Repositories (one-to-many)
  - Solutions → Publications (one-to-many)
  - Publications → Comments/Ratings (one-to-many)
  - Restrict deletes on user-related entities to prevent orphaned data

**GitHub Integration**:
- Octokit library for GitHub API access
- Automated repository access management
- Invite users to private repositories after purchase
- Verify and revoke access programmatically

**Email System**:
- MailKit for SMTP operations
- HTML email templates embedded in EmailService
- Verification emails, password resets, contact form notifications
- Configurable SMTP settings via environment variables

### Security Implementation

**Content Security Policy**:
- Comprehensive CSP configured in Program.cs middleware
- Allows Stripe, Google, YouTube embeds, and CDN resources
- Override via CSP_DIRECTIVES environment variable

**Headers** (production only):
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- Strict-Transport-Security (via web.config)
- X-XSS-Protection, Referrer-Policy, Permissions-Policy

**Data Protection**:
- Passwords hashed via Identity (PBKDF2)
- JWT secrets must be 32+ characters
- .env file excluded from version control
- SQL injection protection via parameterized queries (EF Core)

### Static File & Routing Strategy

The application serves both API endpoints and SPA pages:
- `/api/*` → API controllers
- `/health/*` → Health check endpoints
- `/swagger/*` → OpenAPI documentation
- Clean URLs mapped to HTML files (e.g., `/About` → `About.html`)
- Fallback to index.html for SPA routing support
- API_BASE_URL injected into HTML at runtime for flexible deployments

### Health Checks

Multiple health endpoints configured:
- `/health` → Comprehensive JSON report (database, JWT config, email config)
- `/health/live` → Basic liveness probe
- `/health/ready` → Readiness probe for load balancers
- `/health/admin` → Check if admin user exists

## Environment Configuration

### Required Variables
```bash
DATABASE_CONNECTION_STRING  # SQL Server connection string
JWT_KEY                    # Min 32 characters, use: openssl rand -base64 32
ADMIN_EMAIL                # Initial admin user email
ADMIN_PASSWORD             # Initial admin user password
```

### Email Configuration (Required for production)
```bash
EmailSettings__Host        # SMTP host (e.g., smtp.gmail.com)
EmailSettings__Port        # SMTP port (587 for TLS, 465 for SSL)
EmailSettings__FromEmail   # Sender email address
EmailSettings__Username    # SMTP username
EmailSettings__Password    # SMTP password (use app-specific password for Gmail)
EmailSettings__EnableSsl   # true/false
```

### Optional Integrations
```bash
GITHUB_TOKEN               # GitHub Personal Access Token for repository management
Stripe__SecretKey          # Stripe secret key for payments
Stripe__PublishableKey     # Stripe publishable key
REQUIRE_EMAIL_CONFIRMATION # Set to "true" in production
```

## Development Guidelines

### Adding New Entities
1. Create model in `Models/` directory
2. Add DbSet property to `AppDbContext.cs`
3. Configure relationships in `OnModelCreating()`
4. Create migration: `dotnet ef migrations add AddEntityName`
5. Apply migration: `dotnet ef database update`

### Creating API Endpoints
1. Create DTOs in `DTOs/` for request/response
2. Create controller in `Controllers/` inheriting `ControllerBase`
3. Add `[ApiController]` and `[Route("api/[controller]")]` attributes
4. Use `[Authorize(Roles = "...")]` for protected endpoints
5. Follow existing logging patterns with `ILogger<T>`

### Working with Authentication
- Use `[Authorize]` attribute for authenticated endpoints
- Use `[Authorize(Roles = "Admin,Manager")]` for role-based access
- Access current user via `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- JWT tokens expire in 24 hours (configurable via JWT_EXPIRY_HOURS)

### Email Templates
Email HTML templates are embedded in `EmailService.cs`. To modify:
1. Update template methods: `GetEmailVerificationTemplate()`, `GetPasswordResetTemplate()`
2. Keep inline CSS for email client compatibility
3. Test with multiple email clients

### File Uploads
- Products and publications support image uploads
- Files stored in `wwwroot/uploads/{category}/`
- Use GUID filenames to avoid conflicts
- Validate file types and sizes in controller
- Clean up old files when updating

## Deployment Notes

### Production Checklist
- Set `ASPNETCORE_ENVIRONMENT=Production`
- Configure strong JWT_KEY (32+ characters)
- Set REQUIRE_EMAIL_CONFIRMATION=true
- Configure proper CORS origins
- Use managed SQL Server (Azure SQL or AWS RDS)
- Enable SSL/TLS (Nginx reverse proxy or Azure/AWS native)
- Set up log aggregation (Application Insights, CloudWatch)

### Deployment Targets
Comprehensive deployment guides available in:
- `DEPLOYMENT.md` - Multi-platform deployment (Azure, AWS, Docker, Linux VMs)
- `PRODUCTION-DEPLOY.md` - Streamlined Linux production deployment

The application is designed for:
- **Azure**: Web App, Container Apps, or VMs
- **AWS**: ECS Fargate, Elastic Beanstalk, or EC2
- **Linux VMs**: Systemd service with Nginx reverse proxy
- **Docker**: Ready for containerization (no Dockerfile included, but compatible)

### Migration Strategy
Migrations run automatically on application startup via `context.Database.Migrate()` in Program.cs. For production:
- Test migrations in staging first
- Consider manual migration with downtime for large databases
- Use `dotnet ef migrations script` to review SQL before applying

## Important Files

- **Program.cs**: Core application setup - modify for new services, middleware, or global configuration
- **AppDbContext.cs**: Database schema - update for new entities or relationships
- **.env.example**: Template for environment variables
- **web.config**: IIS/Windows Server deployment configuration
- **appsettings.json**: Base configuration (override with environment variables)
- **appsettings.Production.json**: Production-specific settings

## Database Schema Notes

### Core Domain Models
- **User** (ASP.NET Identity extended): FirstName, LastName, Role, IsBlocked, LastLoginDate
- **Product**: Software products with repositories
- **Solution**: Solution categories
- **Publication**: Technical publications linked to solutions
- **Repository**: GitHub repositories associated with products (Price, GitHubRepoName)
- **Payment**: Stripe payment records
- **UserPurchase**: Links users to purchased repositories

### Delete Behavior Patterns
- Comments/Ratings on Publications: Cascade (delete with publication)
- User-related entities: Restrict (prevent accidental orphaning)
- ContactForms: SetNull (preserve form even if user deleted)
- CommentLikes: Cascade with comment, Restrict with user

## Troubleshooting

### Common Issues
- **Database connection fails**: Verify connection string, firewall rules, SQL Server accessibility
- **JWT authentication fails**: Ensure JWT_KEY is 32+ characters and matches across instances
- **Emails not sending**: For Gmail, use App Password (not account password); verify SMTP settings
- **Port 7150 in use**: Change via `ASPNETCORE_URLS` environment variable
- **Migration errors**: Check for conflicting migrations, review DbContext configuration

### Debugging
- Enable detailed errors in development: Set `ASPNETCORE_ENVIRONMENT=Development`
- Check logs via `/health` endpoint for configuration issues
- Use Swagger UI at `/swagger` to test API endpoints
- Enable sensitive data logging: Already enabled in development via Program.cs
