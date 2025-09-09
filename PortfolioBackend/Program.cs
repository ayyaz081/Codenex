using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using PortfolioBackend.Data;
using PortfolioBackend.Models;
using PortfolioBackend.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Enable response caching
builder.Services.AddResponseCaching();

// Configure DbContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
);

// Configure CORS for cloud deployment
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new string[0];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment() || allowedOrigins.Length == 0)
        {
            // Development or no specific origins configured - allow any origin
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithExposedHeaders("Content-Disposition", "Content-Length", "Content-Type");
        }
        else
        {
            // Production with specified allowed origins
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
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
    options.SignIn.RequireConfirmedEmail = false; // Set to true for production
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-very-secure-secret-key-that-is-at-least-256-bits-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CodenexSolutions";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CodenexSolutions";

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

// Configure Email Settings
builder.Services.Configure<EmailSettings>(options =>
{
    builder.Configuration.GetSection("EmailSettings").Bind(options);
    
    // Override password from environment variable if available (for production security)
    var envPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
    if (!string.IsNullOrEmpty(envPassword))
    {
        options.Password = envPassword;
    }
});

// Add custom services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add HSTS and security headers for production
if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production security headers
    app.UseHsts();
    
    // Custom security headers
    app.Use((context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' https:; " +
            "media-src 'self'; " +
            "object-src 'none'; " +
            "child-src 'none'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'; " +
            "upgrade-insecure-requests;";
        return next();
    });
}

// Configure HTTPS redirection based on environment and configuration
var useHttpsRedirection = builder.Configuration.GetValue<bool>("Security:UseHttpsRedirection", true);
var requireHttps = builder.Configuration.GetValue<bool>("Security:RequireHttps", !app.Environment.IsDevelopment());

if (useHttpsRedirection && (requireHttps || !app.Environment.IsDevelopment()))
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("DefaultCorsPolicy");

// Enable response caching middleware
app.UseResponseCaching();

// Configure default files (index.html, Home.html, default.html)
app.UseDefaultFiles();

// Serve static files from wwwroot
app.UseStaticFiles();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Fallback route to serve index.html for any unmatched routes (SPA support)
app.MapFallback(async context =>
{
    var indexPath = Path.Combine(app.Environment.WebRootPath, "index.html");
    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(await File.ReadAllTextAsync(indexPath));
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Page not found");
    }
});

app.Run();
