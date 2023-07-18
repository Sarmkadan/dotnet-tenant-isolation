// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using TenantIsolation.Data;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 7: API Controller Implementation
/// Complete REST API controller demonstrating best practices for multi-tenant applications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly TenantResolutionService _tenantResolution;
    private readonly OrganizationRepository _organizationRepository;
    private readonly ConfigurationService _configService;
    private readonly TenantFeatureService _featureService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        TenantResolutionService tenantResolution,
        OrganizationRepository organizationRepository,
        ConfigurationService configService,
        TenantFeatureService featureService,
        ILogger<CompaniesController> logger)
    {
        _tenantResolution = tenantResolution;
        _organizationRepository = organizationRepository;
        _configService = configService;
        _featureService = featureService;
        _logger = logger;
    }

    /// <summary>
    /// List all companies for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> ListCompanies()
    {
        // Resolve current tenant from request
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant from request");

        _logger.LogInformation($"Listing companies for tenant: {tenant.Slug}");

        // Get all organizations for this tenant
        var organizations = await _organizationRepository
            .GetActiveOrganizationsAsync(tenant.Id);

        return Ok(organizations);
    }

    /// <summary>
    /// Get a specific company by ID (with permission check)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Organization>> GetCompany(Guid id)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant");

        var company = await _organizationRepository.GetByIdAsync(id);

        // Verify company belongs to current tenant
        if (company == null || company.TenantId != tenant.Id)
            return NotFound("Company not found");

        return Ok(company);
    }

    /// <summary>
    /// Create a new company
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Organization>> CreateCompany(CreateCompanyRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Company name is required");

        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant");

        // Check if feature is enabled
        var canCreateCompanies = await _featureService.IsFeatureEnabledAsync(
            tenant.Id, "multiple-organizations");

        if (!canCreateCompanies)
            return Forbid("This feature is not available for your tenant");

        // Create organization
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = request.Name,
            Slug = request.Slug?.ToLower().Replace(" ", "-") ?? request.Name.ToLower(),
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _organizationRepository.AddAsync(organization);

        _logger.LogInformation(
            $"Created company {organization.Name} for tenant {tenant.Slug}");

        return CreatedAtAction(nameof(GetCompany), new { id = organization.Id }, organization);
    }

    /// <summary>
    /// Update a company
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCompany(Guid id, UpdateCompanyRequest request)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant");

        var company = await _organizationRepository.GetByIdAsync(id);

        if (company == null || company.TenantId != tenant.Id)
            return NotFound();

        // Update properties
        if (!string.IsNullOrWhiteSpace(request.Name))
            company.Name = request.Name;

        if (request.Description != null)
            company.Description = request.Description;

        await _organizationRepository.UpdateAsync(company);

        _logger.LogInformation($"Updated company {company.Name}");

        return NoContent();
    }

    /// <summary>
    /// Delete a company
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant");

        var company = await _organizationRepository.GetByIdAsync(id);

        if (company == null || company.TenantId != tenant.Id)
            return NotFound();

        // Soft delete
        company.IsDeleted = true;
        await _organizationRepository.UpdateAsync(company);

        _logger.LogInformation($"Deleted company {company.Name}");

        return NoContent();
    }

    /// <summary>
    /// Get company-specific settings
    /// </summary>
    [HttpGet("{id:guid}/settings")]
    public async Task<ActionResult<object>> GetCompanySettings(Guid id)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant");

        var company = await _organizationRepository.GetByIdAsync(id);

        if (company == null || company.TenantId != tenant.Id)
            return NotFound();

        // Get configuration for this company
        var configKey = $"company:{company.Id}:settings";

        var settings = await _configService.GetAllConfigurationsAsync(tenant.Id);

        var companySettings = settings
            .Where(kvp => kvp.Key.StartsWith($"company:{company.Id}:"))
            .ToDictionary(kvp => kvp.Key.Replace($"company:{company.Id}:", ""), kvp => kvp.Value);

        return Ok(companySettings);
    }

    /// <summary>
    /// Update company settings
    /// </summary>
    [HttpPut("{id:guid}/settings")]
    public async Task<IActionResult> UpdateCompanySettings(Guid id, Dictionary<string, string> settings)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant");

        var company = await _organizationRepository.GetByIdAsync(id);

        if (company == null || company.TenantId != tenant.Id)
            return NotFound();

        // Save settings
        foreach (var kvp in settings)
        {
            var configKey = $"company:{company.Id}:{kvp.Key}";
            await _configService.SetConfigurationAsync(tenant.Id, configKey, kvp.Value);
        }

        _logger.LogInformation($"Updated settings for company {company.Name}");

        return NoContent();
    }

    /// <summary>
    /// Check if a feature is available
    /// </summary>
    [HttpGet("features/{featureKey}/available")]
    public async Task<ActionResult<bool>> IsFeatureAvailable(string featureKey)
    {
        var tenant = await _tenantResolution.ResolveTenantAsync();

        if (tenant == null)
            return BadRequest("Could not resolve tenant");

        var isAvailable = await _featureService.IsFeatureEnabledAsync(tenant.Id, featureKey);
        return Ok(isAvailable);
    }
}

// Request/Response DTOs

public class CreateCompanyRequest
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
}

public class UpdateCompanyRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class CompanyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
