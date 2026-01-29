# Security Policy

## Reporting Security Vulnerabilities

The dotnet-tenant-isolation team takes security seriously. If you discover a security vulnerability, please help us keep our users safe by following responsible disclosure practices.

### DO NOT Open Public Issues

**Please do not open a public GitHub issue for security vulnerabilities.** Public issues make it easier for malicious actors to exploit the vulnerability before users have time to patch.

### Report to Us Privately

Report security vulnerabilities using one of these methods:

#### GitHub Private Vulnerability Reporting (Recommended)

Use GitHub's built-in security advisory system:

1. Navigate to [Security Advisories](https://github.com/sarmkadan/dotnet-tenant-isolation/security/advisories/new)
2. Click "Report a vulnerability"
3. Provide detailed information about the vulnerability
4. Submit the report

#### Email

Send a detailed report to: **rutova2@gmail.com**

Include in your report:
- Description of the vulnerability
- Affected versions
- Steps to reproduce
- Potential impact
- Suggested fix (if you have one)

## Response Timeline

We are committed to:

- **48 hours**: Initial acknowledgment of your report
- **1 week**: Assessment and initial analysis
- **Patch development**: Timeline depends on complexity
- **Disclosure**: After users have time to patch

## Supported Versions

Security updates are provided for:

| Version | Status | Support |
|---------|--------|---------|
| 1.x | Current | Actively maintained |
| < 1.0 | Unsupported | No security updates |

We recommend upgrading to the latest version to receive security patches.

## Security Best Practices for Users

When using dotnet-tenant-isolation:

### Authentication & Authorization

1. **Always validate tenant context** - Don't assume tenant resolution succeeded
   ```csharp
   var tenant = await _tenantResolution.ResolveTenantAsync();
   if (tenant == null)
   {
       return BadRequest("Tenant could not be resolved");
   }
   ```

2. **Use strict data isolation policies** by default
   ```csharp
   var policy = DataIsolationPolicyType.Strict;
   ```

3. **Validate cross-tenant access** explicitly
   ```csharp
   if (!await _isolationService.CanAccessCrossTenantAsync(currentTenantId, targetTenantId))
   {
       return Forbid();
   }
   ```

### Configuration Security

1. **Encrypt sensitive configuration** values
   ```csharp
   await _configService.SetConfigurationAsync(
       tenantId, "api:secret", encryptedValue, isEncrypted: true);
   ```

2. **Store secrets securely** - Use Azure Key Vault, AWS Secrets Manager, or similar
   ```csharp
   var secret = await _keyVaultClient.GetSecretAsync("tenant-api-key");
   ```

3. **Rotate secrets regularly** - Especially for shared credentials

### Database Security

1. **Use least privilege** - Database accounts should have minimal required permissions

2. **Enable connection encryption** - Use SSL/TLS for all database connections
   ```csharp
   "Server=myserver.com;Encrypt=true;TrustServerCertificate=false;"
   ```

3. **Implement row-level security** - In addition to application-level isolation

### Data Isolation

1. **Test your isolation policies** - Don't assume they work correctly
   ```csharp
   var testResult = await _isolationService.CheckPolicyViolationsAsync(
       tenantId, "User", userData);
   ```

2. **Monitor for isolation violations** - Log and alert on suspicious access patterns

3. **Regular audits** - Review data access logs for anomalies

### Dependency Management

1. **Keep dependencies updated** - Regularly update NuGet packages
   ```bash
   dotnet list package --outdated
   dotnet package update
   ```

2. **Monitor for vulnerabilities** - Use tools to scan dependencies
   ```bash
   dotnet list package --vulnerable
   ```

3. **Review security advisories** - Check GitHub security advisories regularly

## Known Security Limitations

- **Single-server deployments**: Ensure redundancy for production
- **In-memory caching**: Sensitive data should not be cached indefinitely
- **HTTP headers for tenant resolution**: Use HTTPS in production; consider using authenticated claims instead

## Vulnerability Disclosure History

As vulnerabilities are discovered and fixed, they will be documented in our [CHANGELOG](CHANGELOG.md).

## Security Contact

For immediate security concerns, contact: **rutova2@gmail.com**

Thank you for helping us keep dotnet-tenant-isolation secure!
