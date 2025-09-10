# Portfolio Backend - Production Deployment Guide

This guide provides comprehensive instructions for deploying the Portfolio Backend to production environments.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker (optional, for containerized deployment)
- Azure CLI (for Azure deployment)
- Valid SSL certificates for HTTPS

### Environment Setup
1. Copy `.env.example` to `.env` and configure all required values
2. Set up your production database
3. Configure email service credentials
4. Set up monitoring and logging

## 🔧 Configuration

### Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` |
| `CONNECTION_STRING` | Database connection string | See database section |
| `JWT_KEY` | JWT secret key (min 256 bits) | `your-ultra-secure-jwt-key...` |
| `JWT_ISSUER` | JWT issuer | `https://yourapi.com` |
| `JWT_AUDIENCE` | JWT audience | `https://yourapi.com` |
| `CORS_ALLOWED_ORIGINS` | Allowed CORS origins | `https://yourfrontend.com,https://www.yourfrontend.com` |
| `EMAIL_HOST` | SMTP host | `smtp.gmail.com` |
| `EMAIL_PORT` | SMTP port | `587` |
| `EMAIL_FROM` | From email address | `noreply@yoursite.com` |
| `EMAIL_USERNAME` | SMTP username | `your-email@gmail.com` |
| `EMAIL_PASSWORD` | SMTP password/app password | `your-app-password` |
| `YOUTUBE_API_KEY` | YouTube Data API key | `your-youtube-api-key` |
| `YOUTUBE_CHANNEL_ID` | Your YouTube channel ID | `UCxxxxxxxxxxxxxxxxxxxxx` |
| `ADMIN_EMAIL` | **Default admin user email** | `admin@yoursite.com` |
| `ADMIN_PASSWORD` | **Default admin user password** | `YourSecurePassword123!` |
| `ADMIN_FIRST_NAME` | Default admin first name (optional) | `Admin` |
| `ADMIN_LAST_NAME` | Default admin last name (optional) | `User` |

### Database Configuration

#### SQLite (Development/Small Production)
```bash
CONNECTION_STRING="Data Source=/app/data/PortfolioDB.sqlite"
DATABASE_PROVIDER="sqlite"
```

#### Azure SQL Database (Recommended for Production)
```bash
CONNECTION_STRING="Server=yourserver.database.windows.net;Database=PortfolioDB;User Id=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
DATABASE_PROVIDER="sqlserver"
```

#### PostgreSQL
```bash
CONNECTION_STRING="Host=yourhost;Database=PortfolioDB;Username=yourusername;Password=yourpassword;SSL Mode=Require;"
DATABASE_PROVIDER="postgresql"
```

### Admin User Configuration

**IMPORTANT**: The application will automatically create an admin user on startup if one doesn't exist and the required environment variables are set.

#### Required Environment Variables for Admin Creation:
```bash
ADMIN_EMAIL="admin@yoursite.com"
ADMIN_PASSWORD="YourVerySecurePassword123!"
ADMIN_FIRST_NAME="Admin"  # Optional, defaults to "Admin"
ADMIN_LAST_NAME="User"   # Optional, defaults to "User"
```

#### Password Requirements:
- Minimum 8 characters
- Must contain uppercase letters
- Must contain lowercase letters  
- Must contain digits
- Non-alphanumeric characters recommended

#### Admin User Creation Process:
1. **On First Startup**: If no admin user exists and environment variables are set, admin user is created automatically
2. **Logging**: Creation success/failure is logged in application logs
3. **Health Check**: Use `/health/admin` endpoint to verify admin user exists
4. **Security**: Admin email is automatically verified (no email confirmation required)

## 🐳 Docker Deployment

### Build and Run Locally
```bash
# Build the Docker image
docker build -t portfolio-backend .

# Run with environment variables
docker run -d \
  --name portfolio-backend \
  -p 8080:8080 \
  -e JWT_KEY="your-jwt-key" \
  -e CORS_ALLOWED_ORIGINS="https://yourfrontend.com" \
  -e EMAIL_HOST="smtp.gmail.com" \
  -e EMAIL_USERNAME="your-email@gmail.com" \
  -e EMAIL_PASSWORD="your-app-password" \
  portfolio-backend
```

### Docker Compose
```bash
# Copy environment variables
cp .env.example .env
# Edit .env with your configuration

# Start services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f portfolio-backend
```

## ☁️ Cloud Deployment

### Azure App Service

#### 1. Create Resources
```bash
# Login to Azure
az login

# Create resource group
az group create --name portfolio-rg --location "East US"

# Deploy infrastructure
az deployment group create \
  --resource-group portfolio-rg \
  --template-file azure-deploy.json \
  --parameters appName=your-portfolio-backend
```

#### 2. Configure Application Settings
```bash
# Set environment variables
az webapp config appsettings set \
  --resource-group portfolio-rg \
  --name your-portfolio-backend-production \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    JWT_KEY="your-jwt-key" \
    CORS_ALLOWED_ORIGINS="https://yourfrontend.com" \
    EMAIL_HOST=smtp.gmail.com \
    EMAIL_USERNAME="your-email@gmail.com" \
    EMAIL_PASSWORD="your-app-password"
```

#### 3. Deploy Application
```bash
# Build and publish
dotnet publish PortfolioBackend/PortfolioBackend.csproj -c Release -o ./publish

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group portfolio-rg \
  --name your-portfolio-backend-production \
  --src ./publish.zip
```

### AWS Elastic Container Service (ECS)

#### 1. Push to ECR
```bash
# Get login token
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin your-account.dkr.ecr.us-east-1.amazonaws.com

# Build and tag image
docker build -t portfolio-backend .
docker tag portfolio-backend:latest your-account.dkr.ecr.us-east-1.amazonaws.com/portfolio-backend:latest

# Push image
docker push your-account.dkr.ecr.us-east-1.amazonaws.com/portfolio-backend:latest
```

#### 2. Create ECS Service
- Use the provided task definition template
- Configure environment variables in the task definition
- Set up Application Load Balancer
- Configure auto-scaling

### Digital Ocean App Platform

#### 1. Create App Spec
```yaml
name: portfolio-backend
services:
- name: api
  source_dir: /
  dockerfile_path: Dockerfile
  instance_count: 1
  instance_size_slug: basic-xxs
  http_port: 8080
  env:
  - key: ASPNETCORE_ENVIRONMENT
    value: Production
  - key: JWT_KEY
    value: your-jwt-key
    type: SECRET
  - key: CORS_ALLOWED_ORIGINS
    value: https://yourfrontend.com
```

#### 2. Deploy
```bash
doctl apps create --spec app-spec.yaml
```

## 🗄️ Database Migration

### Run Migrations

#### Using Scripts
```bash
# Run the migration script
./PortfolioBackend/Scripts/init-database.ps1 -Environment Production -ConnectionString "your-connection-string"
```

#### Manual Migration
```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Production
export CONNECTION_STRING="your-connection-string"

# Navigate to backend directory
cd PortfolioBackend

# Run migrations
dotnet ef database update

# Verify database
dotnet run --no-build --check-database
```

## 🔐 SSL/TLS Certificate

### Let's Encrypt (Recommended)
```bash
# Using Certbot
sudo certbot certonly --webroot -w /var/www/html -d yourdomain.com

# Or use cloud provider's certificate manager
# Azure: App Service Certificates
# AWS: Certificate Manager
# Cloudflare: SSL certificates
```

### Configure HTTPS Redirection
The application automatically redirects HTTP to HTTPS in production. Configure your load balancer or reverse proxy accordingly.

## 📊 Monitoring and Logging

### Health Checks
- **Basic Health**: `https://yourdomain.com/health`
- **Readiness**: `https://yourdomain.com/health/ready`
- **Liveness**: `https://yourdomain.com/health/live`
- **Admin Status**: `https://yourdomain.com/health/admin`

### Application Insights (Azure)
```bash
# Configure in Azure portal
# Add connection string to app settings
APPLICATIONINSIGHTS_CONNECTION_STRING="your-connection-string"
```

### Structured Logging
Logs are automatically written to:
- Console (for container platforms)
- Files: `logs/portfolio-{date}.log`
- Error files: `logs/errors/portfolio-errors-{date}.log`

## 🔒 Security Checklist

### Before Deployment
- [ ] JWT secret key is properly configured (min 256 bits)
- [ ] CORS origins are restricted to your domains only
- [ ] HTTPS is enabled and HTTP redirects to HTTPS
- [ ] Email credentials are using app-specific passwords
- [ ] Database connection uses SSL/TLS
- [ ] All secrets are stored securely (not in code)
- [ ] Rate limiting is configured
- [ ] Security headers are enabled

### Post Deployment
- [ ] Health checks are responding
- [ ] SSL certificate is valid and properly configured
- [ ] CORS is working correctly
- [ ] Authentication endpoints are working
- [ ] Email functionality is working
- [ ] Database connectivity is confirmed
- [ ] Monitoring and logging are active

## 🚨 Troubleshooting

### Common Issues

#### 1. JWT Authentication Fails
```bash
# Check JWT configuration
curl -X GET https://yourdomain.com/health
# Look for "jwt-configuration" status
```

#### 2. CORS Errors
```bash
# Verify CORS configuration
echo $CORS_ALLOWED_ORIGINS
# Should match your frontend domain exactly
```

#### 3. Database Connection Issues
```bash
# Test database connectivity
curl -X GET https://yourdomain.com/health
# Check "database" status
```

#### 4. Email Not Sending
```bash
# Check email configuration
curl -X GET https://yourdomain.com/health
# Check "email-configuration" status
```

### Log Analysis
```bash
# View recent logs (Docker)
docker logs portfolio-backend --tail=100 --follow

# View logs (Azure App Service)
az webapp log tail --resource-group portfolio-rg --name your-app-name

# Search for errors
grep -i error logs/portfolio-*.log
```

## 🔄 CI/CD Pipeline

The repository includes a GitHub Actions workflow (`.github/workflows/deploy.yml`) that:
1. Builds and tests the application
2. Performs security scanning
3. Builds and pushes Docker images
4. Deploys to Azure App Service
5. Runs health checks and smoke tests

### Required GitHub Secrets
- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub password/token
- `AZURE_WEBAPP_PUBLISH_PROFILE`: Azure App Service publish profile
- Environment-specific secrets (JWT keys, email passwords, etc.)

## 📈 Performance Optimization

### Recommended Settings
```bash
# Response caching
RESPONSE_CACHING_MAX_AGE=3600

# Static files caching
STATIC_FILES_MAX_AGE=2592000

# Rate limiting
RATE_LIMIT_REQUESTS_PER_MINUTE=60
RATE_LIMIT_REQUESTS_PER_HOUR=1000
```

### Database Performance
- Use connection pooling
- Enable query caching
- Regular database maintenance
- Monitor query performance

## 📞 Support

For deployment issues or questions:
1. Check the health endpoints
2. Review application logs
3. Verify environment configuration
4. Check the troubleshooting section above

Remember to never commit secrets to version control and always use environment variables for sensitive configuration!
