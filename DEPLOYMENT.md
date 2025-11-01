# CodeNex Deployment Guide

Complete guide for deploying CodeNex to multiple platforms including Azure, AWS, Docker, Linux VMs, and VPC environments.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Quick Start with Docker](#quick-start-with-docker)
- [Platform-Specific Deployments](#platform-specific-deployments)
  - [Azure Web App](#1-azure-web-app)
  - [Azure Container Apps](#2-azure-container-apps)
  - [AWS ECS (Fargate)](#3-aws-ecs-fargate)
  - [AWS Elastic Beanstalk](#4-aws-elastic-beanstalk)
  - [Linux VM (Ubuntu/Debian)](#5-linux-vm-ubuntudebian)
  - [Docker Compose (Any Platform)](#6-docker-compose-any-platform)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [SSL/TLS Configuration](#ssltls-configuration)
- [Monitoring and Logging](#monitoring-and-logging)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required
- **Database**: SQL Server 2019+ (Azure SQL, AWS RDS, or self-hosted)
- **JWT Secret Key**: Minimum 32 characters (generate with `openssl rand -base64 32`)
- **Admin Credentials**: Email and password for first admin user

### Optional
- **Email Server**: SMTP credentials for notifications
- **GitHub Token**: For repository integration features
- **Stripe Account**: For payment processing

### Tools
- Docker (for containerized deployments)
- Azure CLI (for Azure deployments)
- AWS CLI (for AWS deployments)
- Git

---

## Quick Start with Docker

The fastest way to test CodeNex locally:

```bash
# 1. Clone the repository
git clone <your-repo-url>
cd Codenex

# 2. Create .env file from template
cp .env.example .env

# 3. Edit .env with your configuration
# Minimum required: DATABASE_CONNECTION_STRING, JWT_KEY, ADMIN_EMAIL, ADMIN_PASSWORD

# 4. Start with Docker Compose
docker-compose up -d

# 5. Access the application
# http://localhost:7150
```

---

## Platform-Specific Deployments

### 1. Azure Web App

**Best for**: Simple deployment without containers, automatic scaling

#### Option A: Using GitHub Actions (Recommended)

1. **Create Azure Web App**:
   ```bash
   az webapp create \
     --name codenex-app \
     --resource-group codenex-rg \
     --plan codenex-plan \
     --runtime "DOTNET|8.0"
   ```

2. **Configure App Settings** in Azure Portal:
   - Go to Configuration → Application Settings
   - Add required environment variables (see [Configuration](#configuration))

3. **Setup Continuous Deployment**:
   - Get publish profile: Azure Portal → Download publish profile
   - Add to GitHub Secrets as `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Push to master branch to trigger deployment

#### Option B: Manual Deployment

```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Deploy using Azure CLI
az webapp deploy \
  --resource-group codenex-rg \
  --name codenex-app \
  --src-path ./publish.zip \
  --type zip
```

#### Configure Database Connection
```bash
az webapp config connection-string set \
  --name codenex-app \
  --resource-group codenex-rg \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=tcp:your-server.database.windows.net,1433;Database=CodeNexDB;..."
```

---

### 2. Azure Container Apps

**Best for**: Modern cloud-native deployments, microservices, automatic HTTPS

#### Automated Script

```bash
cd deploy
chmod +x azure-container-app.sh
./azure-container-app.sh
```

#### Manual Steps

```bash
# 1. Create Azure Container Registry
az acr create \
  --resource-group codenex-rg \
  --name codenexacr \
  --sku Basic

# 2. Build and push image
az acr build \
  --registry codenexacr \
  --image codenex:latest \
  --file Dockerfile \
  .

# 3. Create Container Apps environment
az containerapp env create \
  --name codenex-env \
  --resource-group codenex-rg \
  --location eastus

# 4. Deploy container app
az containerapp create \
  --name codenex-app \
  --resource-group codenex-rg \
  --environment codenex-env \
  --image codenexacr.azurecr.io/codenex:latest \
  --target-port 7150 \
  --ingress external \
  --registry-server codenexacr.azurecr.io \
  --env-vars ASPNETCORE_ENVIRONMENT=Production
```

#### Configure Secrets

```bash
az containerapp secret set \
  --name codenex-app \
  --resource-group codenex-rg \
  --secrets \
    jwt-key="<your-jwt-key>" \
    db-connection="<your-connection-string>"
```

---

### 3. AWS ECS (Fargate)

**Best for**: Scalable container deployments on AWS, integration with other AWS services

#### Automated Script

```bash
cd deploy
chmod +x aws-ecs-deploy.sh
./aws-ecs-deploy.sh
```

#### Manual Steps

```bash
# 1. Create ECR repository
aws ecr create-repository --repository-name codenex --region us-east-1

# 2. Build and push image
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
docker build -t codenex .
docker tag codenex:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/codenex:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/codenex:latest

# 3. Create ECS cluster
aws ecs create-cluster --cluster-name codenex-cluster --region us-east-1

# 4. Register task definition (see deploy/aws-ecs-deploy.sh for full JSON)

# 5. Create RDS SQL Server instance
aws rds create-db-instance \
  --db-instance-identifier codenex-db \
  --db-instance-class db.t3.medium \
  --engine sqlserver-ex \
  --master-username admin \
  --master-user-password <your-password> \
  --allocated-storage 20

# 6. Store secrets in AWS Secrets Manager
aws secretsmanager create-secret \
  --name codenex/db-connection \
  --secret-string "Server=<rds-endpoint>;Database=CodeNexDB;..."

aws secretsmanager create-secret \
  --name codenex/jwt-key \
  --secret-string "<your-jwt-key>"

# 7. Create ECS service with load balancer
aws ecs create-service \
  --cluster codenex-cluster \
  --service-name codenex-service \
  --task-definition codenex-task \
  --desired-count 1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxx],securityGroups=[sg-xxx],assignPublicIp=ENABLED}"
```

---

### 4. AWS Elastic Beanstalk

**Best for**: Simplified AWS deployment, automatic platform management

```bash
# 1. Install EB CLI
pip install awsebcli

# 2. Initialize Elastic Beanstalk application
eb init -p docker codenex-app --region us-east-1

# 3. Create environment
eb create codenex-env \
  --instance-type t3.medium \
  --database.engine sqlserver-ex \
  --envvars ASPNETCORE_ENVIRONMENT=Production

# 4. Configure environment variables
eb setenv \
  JWT_KEY="<your-jwt-key>" \
  DATABASE_CONNECTION_STRING="<your-connection-string>" \
  ADMIN_EMAIL="admin@example.com" \
  ADMIN_PASSWORD="<your-password>"

# 5. Deploy
eb deploy

# 6. Open in browser
eb open
```

---

### 5. Linux VM (Ubuntu/Debian)

**Best for**: Self-hosted deployments, VPC environments, full control

#### Automated Script

```bash
# Copy script to your VM
scp deploy/linux-vm-deploy.sh user@your-vm-ip:/tmp/

# SSH to VM and run
ssh user@your-vm-ip
sudo bash /tmp/linux-vm-deploy.sh
```

#### Manual Steps

```bash
# 1. Install Docker
curl -fsSL https://get.docker.com | sh
sudo systemctl enable docker
sudo systemctl start docker

# 2. Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# 3. Clone application
cd /opt
sudo git clone <your-repo-url> codenex
cd codenex

# 4. Create .env file
sudo cp .env.example .env
sudo nano .env  # Edit with your configuration

# 5. Start application
sudo docker-compose up -d

# 6. Setup Nginx reverse proxy (optional)
sudo apt install nginx
sudo nano /etc/nginx/sites-available/codenex
```

**Nginx Configuration**:
```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:7150;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        client_max_body_size 100M;
    }
}
```

```bash
# Enable site and restart Nginx
sudo ln -s /etc/nginx/sites-available/codenex /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx

# Setup SSL with Let's Encrypt
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

---

### 6. Docker Compose (Any Platform)

**Best for**: Local development, testing, simple deployments

```bash
# 1. Create .env file
cp .env.example .env
nano .env  # Edit configuration

# 2. Start services
docker-compose up -d

# 3. View logs
docker-compose logs -f

# 4. Stop services
docker-compose down

# 5. Restart after changes
docker-compose down && docker-compose up -d --build
```

---

## Configuration

### Required Environment Variables

```bash
# Database
DATABASE_CONNECTION_STRING=Server=your-server;Database=CodeNexDB;User Id=user;Password=pass;TrustServerCertificate=True;

# JWT Authentication
JWT_KEY=<minimum-32-character-secret-key>
JWT_ISSUER=CodeNexAPI
JWT_AUDIENCE=CodeNexAPI
JWT_EXPIRY_HOURS=24

# Admin User
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=<secure-password>

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:7150
API_BASE_URL=https://yourdomain.com
```

### Optional Environment Variables

```bash
# Email Configuration
EmailSettings__Host=smtp.gmail.com
EmailSettings__Port=587
EmailSettings__FromEmail=noreply@yourdomain.com
EmailSettings__Username=your-email@gmail.com
EmailSettings__Password=<app-password>
EmailSettings__EnableSsl=true

# GitHub Integration
GITHUB_TOKEN=ghp_your_token

# Stripe Payments
Stripe__SecretKey=sk_test_...
Stripe__PublishableKey=pk_test_...

# Email Verification
REQUIRE_EMAIL_CONFIRMATION=true
```

---

## Database Setup

### Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name codenex-sql \
  --resource-group codenex-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password <password>

# Create database
az sql db create \
  --resource-group codenex-rg \
  --server codenex-sql \
  --name CodeNexDB \
  --service-objective S0

# Configure firewall
az sql server firewall-rule create \
  --resource-group codenex-rg \
  --server codenex-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### AWS RDS SQL Server

```bash
# Create DB instance
aws rds create-db-instance \
  --db-instance-identifier codenex-db \
  --db-instance-class db.t3.medium \
  --engine sqlserver-ex \
  --master-username admin \
  --master-user-password <password> \
  --allocated-storage 20 \
  --publicly-accessible
```

### Connection String Format

```
Server=tcp:your-server.database.windows.net,1433;Database=CodeNexDB;User Id=username;Password=password;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;
```

---

## SSL/TLS Configuration

### Let's Encrypt (Linux)

```bash
sudo certbot --nginx -d yourdomain.com
```

### Azure App Service
- Automatically provides SSL
- Custom domains: Portal → TLS/SSL settings → Custom domains

### AWS Certificate Manager

```bash
aws acm request-certificate \
  --domain-name yourdomain.com \
  --validation-method DNS
```

---

## Monitoring and Logging

### Health Endpoints

- `/health` - Comprehensive health check
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe

### View Logs

**Docker**:
```bash
docker-compose logs -f webapp
```

**Azure**:
```bash
az webapp log tail --name codenex-app --resource-group codenex-rg
```

**AWS ECS**:
```bash
aws logs tail /ecs/codenex-task --follow
```

---

## Troubleshooting

### Database Connection Failed
- Verify connection string format
- Check firewall rules
- Ensure SQL Server is accessible
- Test connection: `sqlcmd -S <server> -U <user> -P <password>`

### Port 7150 Not Accessible
- Check firewall: `sudo ufw allow 7150/tcp`
- Verify container is running: `docker ps`
- Check application logs

### JWT Authentication Issues
- Ensure JWT_KEY is at least 32 characters
- Verify JWT_ISSUER and JWT_AUDIENCE match
- Check token expiry settings

### Email Not Sending
- Verify SMTP credentials
- For Gmail, use App Password not regular password
- Check email service logs

### Container Won't Start
```bash
# View detailed logs
docker-compose logs webapp

# Check container health
docker inspect <container-id>

# Restart services
docker-compose restart
```

### Migration Errors
- Database automatically migrates on startup
- Manual migration: `dotnet ef database update`
- Check database permissions

---

## Security Best Practices

1. **Use strong JWT keys** (minimum 32 characters)
2. **Enable HTTPS** in production
3. **Use managed databases** (Azure SQL, AWS RDS)
4. **Store secrets securely** (Azure Key Vault, AWS Secrets Manager)
5. **Enable email confirmation** in production
6. **Regular backups** of database
7. **Update dependencies** regularly
8. **Use environment-specific configurations**

---

## Support

For issues or questions:
- Check application logs: `/health` endpoint
- Review this deployment guide
- Check GitHub issues
- Contact support team

---

## Quick Reference

| Platform | Deployment Time | Complexity | Best For |
|----------|----------------|------------|----------|
| Docker Compose | 5 min | Low | Development, Testing |
| Azure Web App | 10 min | Low | Simple production deployments |
| Azure Container Apps | 15 min | Medium | Modern cloud-native apps |
| AWS ECS | 20 min | Medium-High | AWS ecosystem integration |
| Linux VM | 15 min | Medium | Self-hosted, full control |

---

**Version**: 1.0  
**Last Updated**: 2025  
**Framework**: .NET 8.0  
**Database**: SQL Server 2019+
