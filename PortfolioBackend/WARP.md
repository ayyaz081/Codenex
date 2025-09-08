# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is a **Portfolio Backend API** built with **ASP.NET Core 8.0** and **Entity Framework Core** with **SQLite**. It serves as the backend for a portfolio website with features for managing publications, products, solutions, user authentication, and content management.

## Development Commands

### Building and Running
```bash
# Build the project
dotnet build

# Run in development mode (with Swagger UI)
dotnet run

# Run with specific profile
dotnet run --launch-profile https

# Build for release
dotnet build --configuration Release
```

### Database Operations
```bash
# Add a new migration
dotnet ef migrations add [MigrationName]

# Update database to latest migration
dotnet ef database update

# Drop and recreate database (development only)
dotnet ef database drop --force
dotnet ef database update

# Generate SQL script for migration
dotnet ef migrations script
```

### Package Management
```bash
# Restore packages
dotnet restore

# Add new package
dotnet add package [PackageName]

# List packages
dotnet list package
```

### Development Tools
```bash
# Watch for changes and auto-restart (useful for development)
dotnet watch run

# Clean build artifacts
dotnet clean
```

## Architecture Overview

### Core Structure
- **Models**: Entity classes representing database tables (User, Product, Publication, Solution, etc.)
- **DTOs**: Data Transfer Objects for API communication
- **Controllers**: API endpoints organized by feature area
- **Services**: Business logic layer (currently only TokenService for JWT)
- **Data**: Entity Framework DbContext and database configuration
- **Migrations**: Database schema version control

### Key Architectural Patterns
1. **Repository Pattern**: Implemented through Entity Framework DbContext
2. **JWT Authentication**: Custom TokenService for user authentication
3. **ASP.NET Core Identity**: Built-in user management with custom User model
4. **DTO Pattern**: Separate data models for API input/output
5. **Controller-based API**: RESTful endpoints organized by domain

### Authentication & Authorization
- Uses **ASP.NET Core Identity** with custom `User` model
- **JWT tokens** with 24-hour expiration
- Role-based authorization (Admin, Manager, RegisteredUser, Guest)
- Token validation middleware configured in `Program.cs`

### Data Layer
- **SQLite** database for development (connection string in `appsettings.json`)
- **Entity Framework Core 9.0** for ORM
- **Code-First** approach with migrations
- Complex relationships between entities (Users, Publications, Comments, Ratings, etc.)

### Key Models & Relationships
- **User**: Extended Identity user with roles and profile information
- **Publication**: Research papers/articles with comments and ratings
- **Product**: Portfolio items with domain categorization  
- **Solution**: Problem-solving showcases
- **Repository**: Code repository links
- **Comments & Ratings**: User engagement features with like functionality

### API Organization
Controllers are organized by feature:
- `AuthController`: Registration, login, password reset
- `AdminController`: Administrative functions
- `PublicationsController`: Publication management
- `ProductsController`: Product portfolio management
- `PublicCommentsController`: Public comment system
- `GlobalSearchController`: Cross-entity search functionality

### Configuration
- Development runs on `https://localhost:7151` and `http://localhost:7150`
- CORS configured for development (allows any origin)
- Swagger UI available in development mode
- Static files served from `wwwroot/` with fallback to `Home.html`

### File Upload Handling
- Static files served from `wwwroot/content/`
- File uploads likely handled through controllers (check `ProductsController` and `PublicationsController`)
- Content-Disposition headers exposed for file downloads

## Development Notes

### Database Seeding
No automatic seeding is configured. You'll need to create initial admin users through the registration endpoint or seed data manually through migrations.

### CORS Configuration
The application uses permissive CORS settings for development. In production, this should be restricted to specific origins.

### JWT Configuration
JWT settings are in `appsettings.json` with fallback defaults. In production, ensure secure key management through environment variables or secure configuration providers.

### Email Integration
Email functionality is stubbed out (password reset, email confirmation) - email sending needs to be implemented for production use.

### Error Handling
Controllers include basic try-catch error handling with logging. Consider implementing global exception handling middleware for production.

### Validation
Model validation uses Data Annotations. Controllers validate ModelState before processing requests.

## Testing Notes

No test project is currently configured. Consider adding:
- Unit tests for services and business logic
- Integration tests for API endpoints
- Database integration tests with in-memory provider
