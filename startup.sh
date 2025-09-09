#!/bin/bash
# Startup script for Azure deployment

# Set environment variables
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://+:80

# Navigate to backend directory
cd PortfolioBackend

# Run the application
dotnet PortfolioBackend.dll
