# =============================================================================
# Multi-stage Docker build for dotnet-tenant-isolation
# Build stage uses .NET SDK, final stage uses minimal ASP.NET runtime
# =============================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy solution and project files first for layer caching
COPY dotnet-tenant-isolation.sln ./
COPY src/TenantIsolation/TenantIsolation.csproj ./src/TenantIsolation/

# Restore dependencies (cached unless .csproj changes)
RUN dotnet restore

# Copy all source code
COPY . .

# Publish in Release mode
RUN dotnet publish src/TenantIsolation/TenantIsolation.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser -m appuser

WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TenantIsolation.dll"]
