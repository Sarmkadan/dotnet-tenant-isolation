# Contributing to dotnet-tenant-isolation

Thank you for your interest in contributing to the dotnet-tenant-isolation framework! We welcome contributions from the community and appreciate your help in improving this project.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Making Changes](#making-changes)
- [Code Standards](#code-standards)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Reporting Issues](#reporting-issues)

## Getting Started

### Fork and Clone

1. **Fork the repository** on GitHub by clicking the "Fork" button in the top-right corner
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/dotnet-tenant-isolation.git
   cd dotnet-tenant-isolation
   ```
3. **Add upstream remote** to stay in sync with the main repository:
   ```bash
   git remote add upstream https://github.com/sarmkadan/dotnet-tenant-isolation.git
   ```

### Create a Feature Branch

Always create a new branch for your work:

```bash
git checkout -b feature/your-feature-name
# or for bug fixes
git checkout -b fix/your-bug-name
```

Branch naming conventions:
- `feature/` - for new features
- `fix/` - for bug fixes
- `docs/` - for documentation changes
- `refactor/` - for code refactoring
- `test/` - for test additions

## Development Setup

### Prerequisites

- **.NET 10.0 SDK** or later
  - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
  - Verify installation: `dotnet --version`
- **Visual Studio 2024**, **Visual Studio Code**, or **JetBrains Rider**
- **Git** version control

### Build the Project

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Build in Release configuration
dotnet build --configuration Release
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity detailed

# Run specific test file
dotnet test path/to/test/file.csproj

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

### Code Formatting

The project uses `dotnet-format` for consistent code style:

```bash
# Check formatting
dotnet format --verify-no-changes

# Auto-fix formatting
dotnet format
```

## Making Changes

### Code Style Guidelines

Follow Microsoft's [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

1. **Naming Conventions**
   - `PascalCase` for class names, method names, and properties
   - `camelCase` for local variables and parameters
   - `UPPER_SNAKE_CASE` for constants

2. **Async/Await**
   - Use `async`/`await` throughout the codebase
   - Method names ending with `Async` for asynchronous methods
   - Never use `Task.Wait()` or `Task.Result`

   ```csharp
   public async Task<Tenant> GetTenantAsync(Guid id)
   {
       return await _context.Tenants.FindAsync(id);
   }
   ```

3. **XML Documentation**
   - Add XML documentation comments to all public APIs
   - Document parameters, return values, and exceptions

   ```csharp
   /// <summary>
   /// Creates a new tenant with the specified details.
   /// </summary>
   /// <param name="name">The name of the tenant</param>
   /// <param name="slug">The URL-friendly slug for the tenant</param>
   /// <param name="adminEmail">The email of the tenant administrator</param>
   /// <returns>The created tenant entity</returns>
   /// <throws>TenantIsolationException if validation fails</throws>
   public async Task<Tenant> CreateTenantAsync(string name, string slug, string adminEmail)
   {
       // implementation
   }
   ```

4. **LINQ and Collections**
   - Use LINQ syntax (method syntax is acceptable)
   - Prefer `var` for local variable declarations when the type is obvious

   ```csharp
   var activeUsers = await _context.Users
       .Where(u => u.TenantId == tenantId && u.IsActive)
       .ToListAsync();
   ```

5. **Error Handling**
   - Use custom exceptions from the framework
   - Include meaningful error messages
   - Don't swallow exceptions without logging

   ```csharp
   if (tenant == null)
   {
       throw new TenantIsolationException($"Tenant with ID {tenantId} not found");
   }
   ```

6. **Keep Author Headers**
   - Do NOT remove or modify existing author headers in files
   - If adding new files, you may add appropriate headers following the project's conventions

### Testing

- **Write tests for all new features** - Aim for 90%+ code coverage
- **Use the existing test patterns** - Follow the project's testing conventions
- **Test edge cases** - Don't just test the happy path
- **Keep tests focused** - One concept per test method
- **Use descriptive names** - Test names should clearly describe what is being tested

```csharp
[TestClass]
public class TenantServiceTests
{
    [TestMethod]
    public async Task CreateTenant_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var service = new TenantService(_repository);
        
        // Act
        var tenant = await service.CreateTenantAsync("Test", "test", "admin@test.com");
        
        // Assert
        Assert.IsNotNull(tenant);
        Assert.AreEqual("Test", tenant.Name);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task CreateTenant_WithNullName_ShouldThrow()
    {
        // Arrange
        var service = new TenantService(_repository);
        
        // Act
        await service.CreateTenantAsync(null, "test", "admin@test.com");
    }
}
```

## Submitting Changes

### Before You Submit

1. **Sync with main branch**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Test your changes thoroughly**:
   ```bash
   dotnet build
   dotnet test
   dotnet format --verify-no-changes
   ```

3. **Commit with clear messages**:
   ```bash
   git commit -m "feat: add tenant status validation"
   git commit -m "fix: prevent null reference in isolation check"
   ```

### Create a Pull Request

1. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Open a Pull Request** on GitHub with:
   - Clear title describing the change
   - Description of what was changed and why
   - Reference to any related issues (e.g., "Closes #123")
   - Confirmation that tests pass and code is formatted

3. **PR Checklist**:
   - [ ] Code follows style guidelines
   - [ ] Tests added/updated and all pass
   - [ ] XML documentation added for public APIs
   - [ ] Documentation updated if needed
   - [ ] No breaking changes (or clearly noted)
   - [ ] Author headers are preserved

## Reporting Issues

### Security Issues

**DO NOT** open a public issue for security vulnerabilities. Instead:
- Use GitHub's [Private Vulnerability Reporting](https://github.com/sarmkadan/dotnet-tenant-isolation/security/advisories/new)
- Or email rutova2@gmail.com
- See [SECURITY.md](SECURITY.md) for details

### Bug Reports

When opening an issue, please include:

1. **Environment**:
   - .NET version: `dotnet --version`
   - Operating system and version
   - Package version

2. **Steps to Reproduce**:
   - Minimal code example
   - Expected behavior
   - Actual behavior

3. **Additional Context**:
   - Relevant code snippets
   - Configuration details
   - Logs or error messages

### Feature Requests

1. Check existing issues/discussions first to avoid duplicates
2. Describe the use case and why this feature is important
3. Explain the expected behavior
4. Suggest implementation approach if you have ideas

## Code Review Process

All submissions are subject to code review. The review process ensures:

- Code quality and consistency
- Test coverage
- Documentation completeness
- No security vulnerabilities
- Alignment with project goals

Be open to feedback and willing to make adjustments based on reviewer suggestions.

## License

By contributing to dotnet-tenant-isolation, you agree that your contributions will be licensed under the MIT License as stated in the [LICENSE](LICENSE) file.

## Questions?

Feel free to:
- Open a [GitHub Discussion](https://github.com/sarmkadan/dotnet-tenant-isolation/discussions)
- Check the [FAQ](docs/faq.md)
- Review [API Reference](docs/api-reference.md)

Thank you for contributing! Your work helps make this framework better for everyone.
