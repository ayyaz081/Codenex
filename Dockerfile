# Multi-stage Dockerfile for CodeNex .NET 8.0 Application
# Optimized for production deployment across platforms (Azure, AWS, Docker, VMs)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (cached layer)
COPY ["CodeNex.csproj", "./"]
RUN dotnet restore "CodeNex.csproj"

# Copy remaining source files
COPY . .

# Build the application
RUN dotnet build "CodeNex.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodeNex.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - smallest image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Install dependencies for healthchecks (curl)
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published app from publish stage
COPY --from=publish /app/publish .

# Create directories for uploads and ensure permissions
RUN mkdir -p /app/wwwroot/Uploads/products && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port 7150 (default application port)
EXPOSE 7150

# Environment variables with defaults
ENV ASPNETCORE_URLS=http://+:7150 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check configuration
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:7150/health/live || exit 1

# Set entrypoint
ENTRYPOINT ["dotnet", "CodeNex.dll"]
