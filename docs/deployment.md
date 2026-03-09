# Deployment Guide

This guide covers deploying a multi-tenant ASP.NET Core application using the dotnet-tenant-isolation framework to production environments.

## Prerequisites

- Docker installed (for containerized deployments)
- Azure subscription (for Azure deployment)
- AWS account (for AWS deployment)
- SSL/TLS certificates (for HTTPS)
- Production database (SQL Server, PostgreSQL, or MySQL)

## Docker Deployment

### Build Docker Image

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10 AS builder
WORKDIR /src
COPY ["src/TenantIsolation/TenantIsolation.csproj", "src/TenantIsolation/"]
RUN dotnet restore "src/TenantIsolation/TenantIsolation.csproj"

COPY . .
RUN dotnet publish "src/TenantIsolation/TenantIsolation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10
WORKDIR /app
COPY --from=builder /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "TenantIsolation.dll"]
```

Build the image:

```bash
docker build -t tenant-isolation-api:latest .
```

### Docker Compose

Use `docker-compose.yml` for multi-service deployment:

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "5000:5000"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Server=mssql;Database=TenantDb;User Id=sa;Password=YourStrong@Pass123;"
      TenantIsolation__AutoMigrate: "true"
      TenantIsolation__EnableAuditLogging: "true"
    depends_on:
      - mssql
    networks:
      - tenant-network

  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Pass123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - mssql-data:/var/opt/mssql
    networks:
      - tenant-network

volumes:
  mssql-data:

networks:
  tenant-network:
    driver: bridge
```

Run services:

```bash
docker-compose up -d
```

## Azure Deployment

### 1. Create App Service

```bash
# Create resource group
az group create --name myResourceGroup --location eastus

# Create App Service plan
az appservice plan create --name myAppServicePlan \
  --resource-group myResourceGroup --sku B2 --is-linux

# Create Web App
az webapp create --resource-group myResourceGroup \
  --plan myAppServicePlan --name myTenantApp --runtime "DOTNET|10.0"
```

### 2. Configure Database

```bash
# Create SQL Server
az sql server create --name myserver --resource-group myResourceGroup \
  --admin-user azureuser --admin-password "MyPassword123!"

# Create database
az sql db create --resource-group myResourceGroup \
  --server myserver --name TenantDb --tier Basic
```

### 3. Configure App Service

```bash
# Set connection string
az webapp config connection-string set \
  --resource-group myResourceGroup --name myTenantApp \
  --settings DefaultConnection="Server=tcp:myserver.database.windows.net,1433;..." \
  --connection-string-type SQLAzure

# Set app settings
az webapp config appsettings set \
  --resource-group myResourceGroup --name myTenantApp \
  --settings ASPNETCORE_ENVIRONMENT=Production
```

### 4. Deploy Application

```bash
# Build release package
dotnet publish -c Release -o publish

# Deploy via ZIP
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --resource-group myResourceGroup --name myTenantApp --src ../app.zip
```

## AWS Deployment (ECS/Fargate)

### 1. Push to ECR

```bash
# Authenticate Docker
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin 123456789.dkr.ecr.us-east-1.amazonaws.com

# Tag and push image
docker tag tenant-isolation-api:latest 123456789.dkr.ecr.us-east-1.amazonaws.com/tenant-app:latest
docker push 123456789.dkr.ecr.us-east-1.amazonaws.com/tenant-app:latest
```

### 2. Create RDS Instance

```bash
aws rds create-db-instance \
  --db-instance-identifier tenant-isolation-db \
  --db-instance-class db.t3.micro \
  --engine mysql \
  --master-username admin \
  --master-user-password "YourPassword123!" \
  --allocated-storage 20
```

### 3. Create ECS Task Definition

```json
{
  "family": "tenant-isolation-app",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "containerDefinitions": [
    {
      "name": "app",
      "image": "123456789.dkr.ecr.us-east-1.amazonaws.com/tenant-app:latest",
      "portMappings": [
        {
          "containerPort": 5000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        },
        {
          "name": "ConnectionStrings__DefaultConnection",
          "value": "Server=...;Database=TenantDb;..."
        }
      ]
    }
  ]
}
```

### 4. Create ECS Service

```bash
aws ecs create-service \
  --cluster my-cluster \
  --service-name tenant-app \
  --task-definition tenant-isolation-app \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxx],securityGroups=[sg-xxx],assignPublicIp=ENABLED}"
```

## Production Configuration

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server.database.windows.net;Database=TenantDb;User Id=sa;Password=***;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
  },
  "TenantIsolation": {
    "AutoMigrate": true,
    "EnableAuditLogging": true,
    "CacheDurationMinutes": 120,
    "TenantIdentificationStrategy": "Header",
    "DefaultIsolationStrategy": "RowLevelSecurity",
    "MaxTenantsPerInstance": 5000,
    "EnableHealthChecks": true,
    "RateLimitPerMinute": 2000
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/app/tenant-isolation.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

## SSL/TLS Configuration

### Enable HTTPS

In `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Use HTTPS with certificate
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

var app = builder.Build();
app.Run();
```

### Using Let's Encrypt (Certbot)

```bash
# Install Certbot
apt-get install certbot python3-certbot-nginx

# Request certificate
certbot certonly --standalone -d example.com

# Certificate location: /etc/letsencrypt/live/example.com/
```

### Configure in Docker

```dockerfile
RUN apt-get update && apt-get install -y certbot

COPY entrypoint.sh /app/
RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
```

## Performance Tuning

### Connection Pooling

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TenantDb;Min Pool Size=5;Max Pool Size=100;Connection Lifetime=300;"
  }
}
```

### Database Indexes

Create indexes for optimal query performance:

```sql
-- Tenant queries
CREATE INDEX idx_tenants_slug ON dbo.Tenants(Slug);
CREATE INDEX idx_tenants_status ON dbo.Tenants(Status);

-- Configuration queries
CREATE INDEX idx_config_tenant_key ON dbo.TenantConfigurations(TenantId, ConfigKey);

-- Feature queries
CREATE INDEX idx_features_tenant_key ON dbo.TenantFeatures(TenantId, FeatureKey);

-- User queries
CREATE INDEX idx_users_tenant_email ON dbo.Users(TenantId, Email);
```

### Caching Strategy

```csharp
// Increase cache duration in production
options.CacheDurationMinutes = 120;  // 2 hours instead of 1

// Use distributed cache (Redis)
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

## Monitoring and Logging

### Application Insights (Azure)

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Structured Logging with Serilog

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/tenant-app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TenantDbContext>()
    .AddCheck("Tenants", new TenantHealthCheck());

app.MapHealthChecks("/health");
```

## Scaling Strategies

### Horizontal Scaling

Deploy multiple instances behind load balancer:

```nginx
upstream backend {
    server api1:5000;
    server api2:5000;
    server api3:5000;
}

server {
    listen 80;
    
    location / {
        proxy_pass http://backend;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

### Database Sharding

For very large deployments, shard by tenant:

```csharp
public class TenantDatabaseRouter
{
    private readonly Dictionary<Guid, string> _shards = new();

    public string GetConnectionString(Guid tenantId)
    {
        var shardId = GetShardId(tenantId);
        return _shards[$"Shard{shardId}"];
    }

    private int GetShardId(Guid tenantId)
    {
        return Math.Abs(tenantId.GetHashCode()) % 10;  // 10 shards
    }
}
```

## Backup and Recovery

### Database Backups

```bash
# SQL Server automated backup
BACKUP DATABASE TenantDb 
TO DISK = '/var/opt/mssql/backup/TenantDb.bak'
WITH INIT, COMPRESSION;

# PostgreSQL dump
pg_dump -U postgres -d tenant_db > backup.sql

# Restore
psql -U postgres -d tenant_db < backup.sql
```

### Automated Daily Backups (Azure)

```bash
# Enable backup
az backup protection enable-for-vm \
  --resource-group myResourceGroup \
  --vault-name myRecoveryServicesVault \
  --vm myVM \
  --policy-name DefaultPolicy
```

## Disaster Recovery Plan

1. **RTO (Recovery Time Objective)**: 1 hour
2. **RPO (Recovery Point Objective)**: 15 minutes
3. **Backup Location**: Geographically separate region
4. **Failover Procedure**: Automated via Azure Traffic Manager

```bash
# Failover to secondary region
az traffic-manager endpoint update \
  --name prod-endpoint \
  --profile-name traffic-manager \
  --endpoint-status Enabled
```

## Security Considerations

### Secrets Management

Use Azure Key Vault:

```bash
# Store connection string
az keyvault secret set --vault-name myKeyVault \
  --name ConnectionString \
  --value "Server=...;Password=..."

# Reference in app
var connectionString = await new DefaultAzureCredential()
    .GetTokenAsync(new TokenRequestContext(...));
```

### Network Security

```csharp
// Enforce HTTPS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", builder =>
        builder.WithOrigins("https://yourdomain.com")
               .AllowAnyMethod()
               .AllowAnyHeader());
});
```

### Rate Limiting

The framework includes built-in rate limiting:

```json
{
  "TenantIsolation": {
    "RateLimitPerMinute": 1000
  }
}
```

## Post-Deployment Checklist

- [ ] Database migrations applied successfully
- [ ] All environment variables configured
- [ ] SSL/TLS certificates installed and valid
- [ ] Logging is working and accessible
- [ ] Health check endpoint responding
- [ ] Backup processes running
- [ ] Monitoring and alerts configured
- [ ] Load balancer health checks passing
- [ ] SSL Labs security grade A or higher
- [ ] Database performance baseline established

## Troubleshooting

### "Connection timeout"
Check database firewall rules and network connectivity.

### "High memory usage"
Increase cache TTL or reduce cache size in configuration.

### "Database locks"
Monitor long-running queries and implement connection pooling.

### "Slow tenant resolution"
Check if resolution strategy involves unnecessary database queries; consider using header-based resolution.
