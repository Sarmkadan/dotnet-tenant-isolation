# =============================================================================
# Multi-stage Docker build for dotnet-tenant-isolation
# Build stage uses .NET SDK, final stage uses minimal ASP.NET runtime
# =============================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10 AS builder

WORKDIR /src

# Copy project file
COPY src/TenantIsolation/TenantIsolation.csproj ./src/TenantIsolation/

# Restore dependencies
RUN dotnet restore "./src/TenantIsolation/TenantIsolation.csproj"

# Copy all source code
COPY . .

# Build in Release mode
RUN dotnet build "./src/TenantIsolation/TenantIsolation.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

# Publish to output directory
RUN dotnet publish "./src/TenantIsolation/TenantIsolation.csproj" \
    -c Release \
    -o /app/publish \
    --no-build

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10

# Create non-root user for security
RUN useradd -m -u 1000 appuser

WORKDIR /app

# Copy published application from builder
COPY --from=builder /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "TenantIsolation.dll"]
