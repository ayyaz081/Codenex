using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CodeNex.Data;
using CodeNex.Models;
using CodeNex.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Enable response caching
builder.Services.AddResponseCaching();

// Configure DbContext - get connection string from environment first
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING") ?? 
                      Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                      builder.Configuration.GetConnectionString("DefaultConnection") ??
                      throw new InvalidOperationException("Database connection string must be provided via DATABASE_CONNECTION_STRING environment variable or appsettings");

// Use SQL Server as the only supported database provider
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
    
    // Configure performance and logging
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    
    options.EnableServiceProviderCaching();
    options.EnableSensitiveDataLogging(false);
});

// Configure CORS - simplified, allow any origin for easier deployment
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development - allow any origin with credentials for easier development
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithExposedHeaders("Content-Disposition", "Content-Length", "Content-Type");
        }
        else
        {
            // Production - allow any origin for easier deployment
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .WithExposedHeaders("Content-Disposition", "Content-Length", "Content-Type");
        }
    });
});

// Add Swagger and health checks
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure ASP.NET Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Email confirmation
    var requireEmailConfirmation = Environment.GetEnvironmentVariable("REQUIRE_EMAIL_CONFIRMATION");
    options.SignIn.RequireConfirmedEmail = requireEmailConfirmation == "true" || 
                                          (builder.Environment.IsProduction() && requireEmailConfirmation != "false");
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication - read from environment first
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? 
             builder.Configuration["Jwt:Key"] ?? 
             "your-secure-jwt-key-at-least-256-bits-long-replace-this-in-production";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
                builder.Configuration["Jwt:Issuer"] ?? "PortfolioAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? 
                  builder.Configuration["Jwt:Audience"] ?? "PortfolioAPI";
var jwtExpiryHours = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") ?? 
                              builder.Configuration["Jwt:ExpiryHours"] ?? "24");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Log authentication failures
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("JWT token validated successfully for user: {UserId}", 
                context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Task.CompletedTask;
        }
    };
});

// Configure Email Settings - simplified, no environment variable overrides
builder.Services.Configure<EmailSettings>(options =>
{
    builder.Configuration.GetSection("EmailSettings").Bind(options);
});


// Add custom services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Security headers will be added in middleware, but no HSTS configuration

// Add comprehensive health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
    .AddCheck("jwt-configuration", () =>
    {
        try
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
            
            if (string.IsNullOrEmpty(jwtKey))
            {
                return HealthCheckResult.Unhealthy("JWT key is not configured");
            }
            
            if (jwtKey.Length < 32)
            {
                return HealthCheckResult.Unhealthy("JWT key is too short (minimum 32 characters required)");
            }
            
            if (jwtKey.Contains("development") && !builder.Environment.IsDevelopment())
            {
                return HealthCheckResult.Degraded("Using development JWT key in non-development environment");
            }
            
            return HealthCheckResult.Healthy("JWT configuration is valid");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"JWT configuration check failed: {ex.Message}");
        }
    })
    .AddCheck("email-configuration", () =>
    {
        try
        {
            var emailHost = Environment.GetEnvironmentVariable("EMAIL_HOST") ?? builder.Configuration["EmailSettings:Host"];
            return !string.IsNullOrEmpty(emailHost)
                ? HealthCheckResult.Healthy("Email configuration is valid")
                : HealthCheckResult.Degraded("Email host is not configured");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Email configuration check failed: {ex.Message}");
        }
    });

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for easier debugging
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    // Development-only configurations
}
else
{
    // Production security headers (but no HSTS)
    
    // Custom security headers
    app.Use((context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        var cspDirectives = Environment.GetEnvironmentVariable("CSP_DIRECTIVES") ??
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
            "style-src-elem 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
            "img-src 'self' data: https: blob:; " +
            "font-src 'self' data: https://cdnjs.cloudflare.com https://fonts.gstatic.com; " +
            "connect-src 'self' https: wss:; " +
            "media-src 'self' https:; " +
            "object-src 'none'; " +
            "child-src 'none'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'; " +
            "base-uri 'self'; " +
            "manifest-src 'self'; " +
            "upgrade-insecure-requests;";
        
        context.Response.Headers["Content-Security-Policy"] = cspDirectives;
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=()";
        // Less restrictive CORS policies for better compatibility
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        return next();
    });
}

// Disable HTTPS redirection - let deployment/reverse proxy handle HTTPS
// No HTTPS redirection in application

// Enable CORS
app.UseCors("DefaultCorsPolicy");

// Enable response caching middleware
app.UseResponseCaching();

// Configure default files (index.html, default.html)
app.UseDefaultFiles();

// Serve static files from wwwroot
app.UseStaticFiles();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Simple health check endpoint for load balancers
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        await context.Response.WriteAsync(report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy");
    }
});

// Live health check endpoint
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        await context.Response.WriteAsync("Live");
    }
});

// Admin status endpoint
app.MapGet("/health/admin", async (UserManager<User> userManager) =>
{
    try
    {
        var adminExists = await userManager.Users.AnyAsync(u => u.Role == "Admin");
        return Results.Ok(new 
        { 
            hasAdmin = adminExists,
            message = adminExists 
                ? "Admin user exists" 
                : "No admin user found. Set ADMIN_EMAIL and ADMIN_PASSWORD environment variables to create one."
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error checking admin status: {ex.Message}");
    }
});

// Fallback route to serve index.html for any unmatched routes (SPA support)
app.MapFallback(async context =>
{
    var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html";
        var htmlContent = await File.ReadAllTextAsync(indexPath);
        
        // Inject API_BASE_URL from environment variables or configuration
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? 
                         builder.Configuration["API_BASE_URL"];
        if (!string.IsNullOrEmpty(apiBaseUrl))
        {
            var scriptInjection = $"<script>window.API_BASE_URL = '{apiBaseUrl}';</script>";
            // Inject before closing head tag
            htmlContent = htmlContent.Replace("</head>", scriptInjection + "</head>");
        }
        
        await context.Response.WriteAsync(htmlContent);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Page not found");
    }
});

// Ensure database directory and database are created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Ensuring database directory and database are created...");
        
        // SQL Server database - no need to create directories
        
        // Apply SQL Server migrations
        try
        {
            logger.LogInformation("Applying SQL Server database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrations completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations.");
        }
        
        // Create default admin user if it doesn't exist
        await CreateDefaultAdminUserAsync(scope.ServiceProvider, logger);

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring the database was created.");
        // Don't throw - let the app start and show meaningful error messages
    }
}

// Method to create default admin user from environment variables
static async Task CreateDefaultAdminUserAsync(IServiceProvider serviceProvider, ILogger logger)
{
    try
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        
        // Check if admin user already exists
        var existingAdmin = await userManager.Users
            .FirstOrDefaultAsync(u => u.Role == "Admin");
            
        if (existingAdmin != null)
        {
            logger.LogInformation("Admin user already exists: {Email}", existingAdmin.Email);
            return;
        }
        
// Read admin user from environment if provided; otherwise fall back to defaults
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@portfolio.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin123!@#";
        var adminFirstName = "Admin";
        var adminLastName = "User";
        
        logger.LogInformation("Creating default admin user with email: {Email}", adminEmail);
        
        // Validate email format
        if (!IsValidEmail(adminEmail))
        {
            logger.LogError("Invalid admin email format: {Email}", adminEmail);
            return;
        }
        
        // Validate password strength
        if (adminPassword.Length < 8)
        {
            logger.LogError("Admin password must be at least 8 characters long.");
            return;
        }
        
        // Create admin user
        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = adminFirstName,
            LastName = adminLastName,
            Role = "Admin",
            EmailConfirmed = true, // Auto-confirm admin email
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        
        if (result.Succeeded)
        {
            logger.LogInformation("Default admin user created successfully: {Email}", adminEmail);
            logger.LogInformation("Admin user ID: {UserId}", adminUser.Id);
        }
        else
        {
            logger.LogError("Failed to create default admin user: {Errors}", 
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred while creating default admin user.");
    }
}

// Simple email validation helper
static bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}

app.Run();
