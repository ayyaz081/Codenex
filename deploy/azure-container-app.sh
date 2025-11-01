#!/bin/bash
# ============================================
# Deploy CodeNex to Azure Container Apps
# ============================================

set -e  # Exit on error

# Configuration
RESOURCE_GROUP="codenex-rg"
LOCATION="eastus"
CONTAINER_APP_ENV="codenex-env"
CONTAINER_APP_NAME="codenex-app"
ACR_NAME="codenexacr"  # Must be globally unique
IMAGE_NAME="codenex"
IMAGE_TAG="latest"

echo "=========================================="
echo "Deploying CodeNex to Azure Container Apps"
echo "=========================================="

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "Error: Azure CLI is not installed. Install from https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Login to Azure (if not already logged in)
echo "Checking Azure login status..."
az account show > /dev/null 2>&1 || az login

# Create resource group
echo "Creating resource group: $RESOURCE_GROUP..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Azure Container Registry
echo "Creating Azure Container Registry: $ACR_NAME..."
az acr create \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --sku Basic \
    --admin-enabled true

# Build and push Docker image to ACR
echo "Building and pushing Docker image..."
az acr build \
    --registry $ACR_NAME \
    --image $IMAGE_NAME:$IMAGE_TAG \
    --file Dockerfile \
    .

# Get ACR credentials
ACR_SERVER=$(az acr show --name $ACR_NAME --query loginServer --output tsv)
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username --output tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query passwords[0].value --output tsv)

# Create Container Apps environment
echo "Creating Container Apps environment..."
az containerapp env create \
    --name $CONTAINER_APP_ENV \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION

# Create SQL Server (if needed)
echo "Note: You need to create Azure SQL Database separately and configure connection string"
echo "Visit: https://portal.azure.com to create Azure SQL Database"

# Deploy Container App
echo "Deploying Container App: $CONTAINER_APP_NAME..."
az containerapp create \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --environment $CONTAINER_APP_ENV \
    --image $ACR_SERVER/$IMAGE_NAME:$IMAGE_TAG \
    --registry-server $ACR_SERVER \
    --registry-username $ACR_USERNAME \
    --registry-password $ACR_PASSWORD \
    --target-port 7150 \
    --ingress external \
    --min-replicas 1 \
    --max-replicas 3 \
    --cpu 1.0 \
    --memory 2.0Gi \
    --env-vars \
        ASPNETCORE_ENVIRONMENT=Production \
        JWT_KEY=secretref:jwt-key \
        DATABASE_CONNECTION_STRING=secretref:db-connection

echo ""
echo "=========================================="
echo "Deployment initiated!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "1. Configure secrets (JWT_KEY, DATABASE_CONNECTION_STRING, etc.) in Azure Portal"
echo "2. Update environment variables in Container App settings"
echo "3. Configure Azure SQL Database and update connection string"
echo ""
echo "To set secrets, run:"
echo "  az containerapp secret set --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --secrets jwt-key=YOUR_JWT_KEY db-connection='YOUR_DB_CONNECTION_STRING'"
echo ""
echo "Application URL:"
az containerapp show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn --output tsv
