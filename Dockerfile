# Portfolio Backend Dockerfile
# Multi-stage build for optimized production deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["PortfolioBackend/PortfolioBackend.csproj", "PortfolioBackend/"]

# Restore dependencies
RUN dotnet restore "PortfolioBackend/PortfolioBackend.csproj"

# Copy all source files
COPY . .

# Build and publish the application
WORKDIR "/src/PortfolioBackend"
RUN dotnet build "PortfolioBackend.csproj" -c Release -o /app/build
RUN dotnet publish "PortfolioBackend.csproj" -c Release -o /app/publish --no-restore --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install SQLite (in case using SQLite in production)
RUN apt-get update && \
    apt-get install -y sqlite3 libsqlite3-dev && \
    rm -rf /var/lib/apt/lists/*

# Create app directory and data directory
WORKDIR /app
RUN mkdir -p /app/data && \
    chmod 755 /app/data

# Create a non-root user for security
RUN groupadd -r appgroup && \
    useradd -r -g appgroup -d /app -s /bin/bash appuser && \
    chown -R appuser:appgroup /app

# Copy published application
COPY --from=build /app/publish .

# Create directory for static files and database
RUN mkdir -p wwwroot/App_Data && \
    chown -R appuser:appgroup wwwroot

# Switch to non-root user
USER appuser

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV CONNECTION_STRING="Data Source=/app/data/PortfolioDB.sqlite"

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "PortfolioBackend.dll"]
