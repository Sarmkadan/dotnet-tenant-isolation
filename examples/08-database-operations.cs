// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using TenantIsolation.Data;
using TenantIsolation.Models;
using TenantIsolation.Services;

namespace TenantIsolation.Examples;

/// <summary>
/// Example 8: Database Operations with Tenant Isolation
/// Demonstrates querying, inserting, and updating tenant-scoped data.
/// </summary>
public class DatabaseOperationsExample
{
    public static async Task RunAsync(WebApplication app, Guid tenantId)
    {
        using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
            var organizationRepository = scope.ServiceProvider
                .GetRequiredService<OrganizationRepository>();
            var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();

            Console.WriteLine("=== Database Operations Example ===\n");

            var tenant = await tenantService.GetTenantAsync(tenantId);
            Console.WriteLine($"Working with tenant: {tenant.Name}\n");

            // Set current tenant context
            dbContext.SetCurrentTenant(tenant.Id);

            // Create organizations
            Console.WriteLine("1. Creating Organizations\n");

            var org1 = new Organization
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Engineering Department",
                Slug = "engineering",
                Description = "All engineering staff",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var org2 = new Organization
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Sales Department",
                Slug = "sales",
                Description = "Sales and business development",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await organizationRepository.AddAsync(org1);
            await organizationRepository.AddAsync(org2);

            Console.WriteLine($"   ✓ Created {org1.Name}");
            Console.WriteLine($"   ✓ Created {org2.Name}\n");

            // Create users
            Console.WriteLine("2. Creating Users\n");

            var user1 = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                OrganizationId = org1.Id,
                Email = "alice@company.com",
                FirstName = "Alice",
                LastName = "Johnson",
                Role = "Engineer",
                PasswordHash = "hashed_password_1",
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                OrganizationId = org1.Id,
                Email = "bob@company.com",
                FirstName = "Bob",
                LastName = "Smith",
                Role = "Engineer",
                PasswordHash = "hashed_password_2",
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var user3 = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                OrganizationId = org2.Id,
                Email = "charlie@company.com",
                FirstName = "Charlie",
                LastName = "Brown",
                Role = "Sales Rep",
                PasswordHash = "hashed_password_3",
                IsEmailVerified = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await userRepository.AddAsync(user1);
            await userRepository.AddAsync(user2);
            await userRepository.AddAsync(user3);

            Console.WriteLine($"   ✓ Created user: {user1.Email}");
            Console.WriteLine($"   ✓ Created user: {user2.Email}");
            Console.WriteLine($"   ✓ Created user: {user3.Email}\n");

            // Query users - automatically scoped to current tenant
            Console.WriteLine("3. Querying Users (Auto-scoped by Tenant)\n");

            var allUsers = await userRepository.GetAllAsync();
            Console.WriteLine($"   Total users: {allUsers.Count}");
            foreach (var user in allUsers)
            {
                Console.WriteLine($"   - {user.Email} ({user.Role})");
            }
            Console.WriteLine();

            // Query users by organization
            Console.WriteLine("4. Querying Users by Organization\n");

            var engUsers = await userRepository.GetActiveUsersInOrganizationAsync(
                tenant.Id, org1.Id);
            Console.WriteLine($"   Engineering users: {engUsers.Count}");
            foreach (var user in engUsers)
            {
                Console.WriteLine($"   - {user.FirstName} {user.LastName}");
            }
            Console.WriteLine();

            // Query verified users
            Console.WriteLine("5. Querying Verified Users\n");

            var verifiedUsers = await userRepository.GetUnverifiedUsersAsync(tenant.Id);
            Console.WriteLine($"   Unverified users: {verifiedUsers.Count}");
            foreach (var user in verifiedUsers)
            {
                Console.WriteLine($"   - {user.Email}");
            }
            Console.WriteLine();

            // Update user
            Console.WriteLine("6. Updating User\n");

            user3.IsEmailVerified = true;
            await userRepository.UpdateAsync(user3);
            Console.WriteLine($"   ✓ Verified email for {user3.Email}\n");

            // Soft delete
            Console.WriteLine("7. Soft Deleting User\n");

            user1.IsDeleted = true;
            await userRepository.UpdateAsync(user1);
            Console.WriteLine($"   ✓ Soft deleted {user1.Email}\n");

            // Query active users
            Console.WriteLine("8. Active Users (Excluding Deleted)\n");

            var activeUsers = await userRepository.GetAllAsync();
            Console.WriteLine($"   Active users: {activeUsers.Count}");
            foreach (var user in activeUsers)
            {
                Console.WriteLine($"   - {user.Email}");
            }
            Console.WriteLine();

            // Get user statistics
            Console.WriteLine("9. User Statistics\n");

            var stats = await userRepository.GetUserStatisticsAsync(tenant.Id);
            Console.WriteLine($"   Total users: {stats.TotalUserCount}");
            Console.WriteLine($"   Active users: {stats.ActiveUserCount}");
            Console.WriteLine($"   Verified emails: {stats.VerifiedEmailCount}\n");

            // Bulk operations
            Console.WriteLine("10. Bulk Operations\n");

            // Deactivate all sales users
            var salesUsers = await userRepository.GetByRoleAsync(tenant.Id, "Sales Rep");
            foreach (var user in salesUsers)
            {
                user.IsActive = false;
            }
            await userRepository.BulkUpdateAsync(salesUsers);
            Console.WriteLine($"   ✓ Deactivated {salesUsers.Count} sales users\n");

            // Clear tenant context (not recommended in production)
            // dbContext.ClearCurrentTenant();
            // var allTenantsData = await userRepository.GetAllAsync(); // Now gets all tenants' data

            Console.WriteLine("Important Database Practices:");
            Console.WriteLine("┌───────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ALWAYS set tenant context before querying              │");
            Console.WriteLine("│ NEVER clear tenant context in production code          │");
            Console.WriteLine("│ Use soft deletes for audit trail compliance            │");
            Console.WriteLine("│ Create indexes on (TenantId, Key) columns              │");
            Console.WriteLine("│ Test queries return only current tenant's data         │");
            Console.WriteLine("│ Use repositories to encapsulate tenant filtering       │");
            Console.WriteLine("└───────────────────────────────────────────────────────┘\n");

            Console.WriteLine("=== Example Complete ===");
        }
    }
}
