# Migration Guide: v1.x to v2.0

This document covers the breaking changes and migration steps for upgrading from dotnet-tenant-isolation v1.x to v2.0.

## Breaking Changes

### 1. Default Port Changed from 5000 to 8080

The container now listens on port **8080** instead of 5000. This aligns with the .NET 8+ convention where non-root containers use port 8080 by default.

**Before (v1.x):**
```yaml
ports:
  - "5000:5000"
environment:
  ASPNETCORE_URLS: http://+:5000
```

**After (v2.0):**
```yaml
ports:
  - "8080:8080"
environment:
  ASPNETCORE_URLS: http://+:8080
```

If you have a reverse proxy or load balancer pointing at port 5000, update it to 8080.

### 2. Docker Image - Multi-Stage Build Updated

The Dockerfile now uses `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0` (explicit patch-version tags). The build stage restores at the solution level for better layer caching.

### 3. Non-Root User

The container runs as a non-root user (`appuser`). If you mount volumes, ensure the host directory is writable by UID 1000 or adjust ownership accordingly.

## Migration Steps

### Step 1: Update docker-compose.yml

Replace port mappings and environment variables:

```bash
sed -i 's/5000:5000/8080:8080/g' docker-compose.yml
sed -i 's/http:\/\/+:5000/http:\/\/+:8080/g' docker-compose.yml
```

### Step 2: Update Health Check URLs

Any external health check probes (Kubernetes liveness/readiness, load balancer health checks) must target port 8080:

```
http://your-host:8080/health
```

### Step 3: Update NuGet Package Reference

```xml
<PackageReference Include="Zaiets.dotnet.tenant.isolation" Version="2.0.0" />
```

### Step 4: Rebuild Container

```bash
docker compose build --no-cache
docker compose up -d
```

### Step 5: Verify

```bash
curl http://localhost:8080/health
```

## Configuration Changes

No changes to `appsettings.json` or `TenantIsolationOptions` are required. All existing configuration keys remain compatible.

## API Compatibility

All public APIs remain unchanged in v2.0. The version bump reflects the infrastructure-level breaking change (port) and Docker build improvements.
