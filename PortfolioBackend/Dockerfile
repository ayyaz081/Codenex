# Use the official ASP.NET Core runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install SSL certificate tools and utilities
RUN apt-get update && apt-get install -y \
    openssl \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create directories for SSL certificates and data
RUN mkdir -p /app/ssl /app/data /app/uploads

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PortfolioBackend.csproj", "."]
RUN dotnet restore "PortfolioBackend.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "PortfolioBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PortfolioBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy SSL certificate generation script
COPY scripts/generate-ssl-cert.sh /app/scripts/
RUN chmod +x /app/scripts/generate-ssl-cert.sh

# Create a startup script that handles SSL certificate generation
COPY scripts/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="https://+:443;http://+:80"

# Set proper ownership
RUN chown -R app:app /app/ssl /app/data /app/uploads

# Use non-root user
USER app

ENTRYPOINT ["/app/docker-entrypoint.sh"]
