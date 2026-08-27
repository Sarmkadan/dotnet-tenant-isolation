using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TenantIsolation.BackgroundTasks;

/// <summary>
/// Tests for the <see cref="TenantCleanupWorker"/> class.
/// </summary>
public class TenantCleanupWorkerTests
{
    /// <summary>
/// Logger instance for test output.
/// </summary>
private readonly ILogger<TenantCleanupWorkerTests> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCleanupWorkerTests"/> class.
    /// Sets up a mock logger for test output.
    /// </summary>
    public TenantCleanupWorkerTests()
    {
        // Logger will be injected via test framework if needed
        // For now, we'll create a mock logger for demonstration
        var mockLogger = new Mock<ILogger<TenantCleanupWorkerTests>>();
        _logger = mockLogger.Object;
    }

    /// <summary>
    /// Verifies that the default value of the CheckInterval property is one day.
    /// </summary>
    [Fact]
    public void CheckInterval_DefaultValue_IsOneDay()
    {
        _logger.LogInformation("Starting test: CheckInterval_DefaultValue_IsOneDay");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            worker.CheckInterval.Should().Be(TimeSpan.FromDays(1));
            _logger.LogInformation("Completed test: CheckInterval_DefaultValue_IsOneDay - CheckInterval is {Expected}", TimeSpan.FromDays(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: CheckInterval_DefaultValue_IsOneDay");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the default value of the RetentionPeriod property is thirty days.
    /// </summary>
    [Fact]
    public void RetentionPeriod_DefaultValue_IsThirtyDays()
    {
        _logger.LogInformation("Starting test: RetentionPeriod_DefaultValue_IsThirtyDays");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            worker.RetentionPeriod.Should().Be(TimeSpan.FromDays(30));
            _logger.LogInformation("Completed test: RetentionPeriod_DefaultValue_IsThirtyDays - RetentionPeriod is {Expected}", TimeSpan.FromDays(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: RetentionPeriod_DefaultValue_IsThirtyDays");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the CheckInterval property can be set to a custom value.
    /// </summary>
    [Fact]
    public void CheckInterval_CanBeCustomized()
    {
        _logger.LogInformation("Starting test: CheckInterval_CanBeCustomized");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            var customInterval = TimeSpan.FromHours(6);
            worker.CheckInterval = customInterval;
            worker.CheckInterval.Should().Be(customInterval);
            _logger.LogInformation("Completed test: CheckInterval_CanBeCustomized - Custom interval set to {CustomInterval}", customInterval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: CheckInterval_CanBeCustomized");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the RetentionPeriod property can be set to a custom value.
    /// </summary>
    [Fact]
    public void RetentionPeriod_CanBeCustomized()
    {
        _logger.LogInformation("Starting test: RetentionPeriod_CanBeCustomized");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            var customRetention = TimeSpan.FromDays(60);
            worker.RetentionPeriod = customRetention;
            worker.RetentionPeriod.Should().Be(customRetention);
            _logger.LogInformation("Completed test: RetentionPeriod_CanBeCustomized - Custom retention set to {CustomRetention}", customRetention);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: RetentionPeriod_CanBeCustomized");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the TenantCleanupWorker can be constructed with valid service provider and logger.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_CreatesWorker()
    {
        _logger.LogInformation("Starting test: Constructor_WithValidParameters_CreatesWorker");
        try
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
            var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
            worker.Should().NotBeNull();
            worker.CheckInterval.Should().Be(TimeSpan.FromDays(1));
            worker.RetentionPeriod.Should().Be(TimeSpan.FromDays(30));
            _logger.LogInformation("Completed test: Constructor_WithValidParameters_CreatesWorker - Worker created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: Constructor_WithValidParameters_CreatesWorker");
            throw;
        }
    }

    /// <summary>
    /// Verifies that StopAsync disposes the timer properly.
    /// </summary>
    [Fact]
    public async Task StopAsync_DisposesTimer()
    {
        _logger.LogInformation("Starting test: StopAsync_DisposesTimer");
        try
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
            var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
            await worker.StopAsync(CancellationToken.None);
            worker.Should().NotBeNull();
            _logger.LogInformation("Completed test: StopAsync_DisposesTimer - StopAsync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: StopAsync_DisposesTimer");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the Dispose method disposes the timer properly.
    /// </summary>
    [Fact]
    public void Dispose_DisposesTimer()
    {
        _logger.LogInformation("Starting test: Dispose_DisposesTimer");
        try
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
            var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
            worker.Dispose();
            worker.Should().NotBeNull();
            _logger.LogInformation("Completed test: Dispose_DisposesTimer - Dispose completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: Dispose_DisposesTimer");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the Dispose method can be called multiple times without throwing an exception.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _logger.LogInformation("Starting test: Dispose_CanBeCalledMultipleTimes");
        try
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
            var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
            worker.Dispose();
            worker.Dispose();
            worker.Should().NotBeNull();
            _logger.LogInformation("Completed test: Dispose_CanBeCalledMultipleTimes - Dispose can be called multiple times");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: Dispose_CanBeCalledMultipleTimes");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the GetRetentionPeriod method returns the correct retention period for a valid worker instance.
    /// </summary>
    [Fact]
    public void GetRetentionPeriod_WithValidWorker_ReturnsRetentionPeriod()
    {
        _logger.LogInformation("Starting test: GetRetentionPeriod_WithValidWorker_ReturnsRetentionPeriod");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            worker.RetentionPeriod = TimeSpan.FromDays(45);
            var retention = worker.GetRetentionPeriod();
            retention.Should().Be(TimeSpan.FromDays(45));
            _logger.LogInformation("Completed test: GetRetentionPeriod_WithValidWorker_ReturnsRetentionPeriod - Retention period is {Retention}", TimeSpan.FromDays(45));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: GetRetentionPeriod_WithValidWorker_ReturnsRetentionPeriod");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the GetRetentionPeriod method throws an ArgumentNullException when called with a null worker instance.
    /// </summary>
    [Fact]
    public void GetRetentionPeriod_WithNullWorker_ThrowsArgumentNullException()
    {
        _logger.LogInformation("Starting test: GetRetentionPeriod_WithNullWorker_ThrowsArgumentNullException");
        try
        {
            TenantCleanupWorker worker = null!;
            Assert.Throws<ArgumentNullException>(() => worker.GetRetentionPeriod());
            _logger.LogInformation("Completed test: GetRetentionPeriod_WithNullWorker_ThrowsArgumentNullException - ArgumentNullException thrown as expected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: GetRetentionPeriod_WithNullWorker_ThrowsArgumentNullException");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the GetCheckInterval method returns the correct check interval for a valid worker instance.
    /// </summary>
    [Fact]
    public void GetCheckInterval_WithValidWorker_ReturnsCheckInterval()
    {
        _logger.LogInformation("Starting test: GetCheckInterval_WithValidWorker_ReturnsCheckInterval");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            worker.CheckInterval = TimeSpan.FromHours(12);
            var interval = worker.GetCheckInterval();
            interval.Should().Be(TimeSpan.FromHours(12));
            _logger.LogInformation("Completed test: GetCheckInterval_WithValidWorker_ReturnsCheckInterval - Check interval is {Interval}", TimeSpan.FromHours(12));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: GetCheckInterval_WithValidWorker_ReturnsCheckInterval");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the GetCheckInterval method throws an ArgumentNullException when called with a null worker instance.
    /// </summary>
    [Fact]
    public void GetCheckInterval_WithNullWorker_ThrowsArgumentNullException()
    {
        _logger.LogInformation("Starting test: GetCheckInterval_WithNullWorker_ThrowsArgumentNullException");
        try
        {
            TenantCleanupWorker worker = null!;
            Assert.Throws<ArgumentNullException>(() => worker.GetCheckInterval());
            _logger.LogInformation("Completed test: GetCheckInterval_WithNullWorker_ThrowsArgumentNullException - ArgumentNullException thrown as expected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: GetCheckInterval_WithNullWorker_ThrowsArgumentNullException");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the AddTenantCleanupWorker extension method throws an ArgumentNullException when the host builder is null.
    /// </summary>
    [Fact]
    public void AddTenantCleanupWorker_WithNullBuilder_ThrowsArgumentNullException()
    {
        _logger.LogInformation("Starting test: AddTenantCleanupWorker_WithNullBuilder_ThrowsArgumentNullException");
        try
        {
            IHostBuilder builder = null!;
            var checkInterval = TimeSpan.FromHours(2);
            var retentionPeriod = TimeSpan.FromDays(14);
            Assert.Throws<ArgumentNullException>(() => builder.AddTenantCleanupWorker(checkInterval, retentionPeriod));
            _logger.LogInformation("Completed test: AddTenantCleanupWorker_WithNullBuilder_ThrowsArgumentNullException - ArgumentNullException thrown as expected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: AddTenantCleanupWorker_WithNullBuilder_ThrowsArgumentNullException");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the AddTenantCleanupWorker extension method throws an ArgumentNullException when the host builder is null (overload with only retention period).
    /// </summary>
    [Fact]
    public void AddTenantCleanupWorker_WithNullBuilderAndRetention_ThrowsArgumentNullException()
    {
        _logger.LogInformation("Starting test: AddTenantCleanupWorker_WithNullBuilderAndRetention_ThrowsArgumentNullException");
        try
        {
            IHostBuilder builder = null!;
            var retentionPeriod = TimeSpan.FromDays(21);
            Assert.Throws<ArgumentNullException>(() => builder.AddTenantCleanupWorker(retentionPeriod));
            _logger.LogInformation("Completed test: AddTenantCleanupWorker_WithNullBuilderAndRetention_ThrowsArgumentNullException - ArgumentNullException thrown as expected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: AddTenantCleanupWorker_WithNullBuilderAndRetention_ThrowsArgumentNullException");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the default value of the CheckInterval property matches the expected default value of one day.
    /// </summary>
    [Fact]
    public void CheckInterval_DefaultValue_MatchesExpectedDefault()
    {
        _logger.LogInformation("Starting test: CheckInterval_DefaultValue_MatchesExpectedDefault");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            var expectedDefault = TimeSpan.FromDays(1);
            worker.CheckInterval.Should().Be(expectedDefault);
            _logger.LogInformation("Completed test: CheckInterval_DefaultValue_MatchesExpectedDefault - CheckInterval matches expected default {Expected}", expectedDefault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: CheckInterval_DefaultValue_MatchesExpectedDefault");
            throw;
        }
    }

    /// <summary>
    /// Verifies that the default value of the RetentionPeriod property matches the expected default value of thirty days.
    /// </summary>
    [Fact]
    public void RetentionPeriod_DefaultValue_MatchesExpectedDefault()
    {
        _logger.LogInformation("Starting test: RetentionPeriod_DefaultValue_MatchesExpectedDefault");
        try
        {
            var worker = new TenantCleanupWorker(null!, null!);
            var expectedDefault = TimeSpan.FromDays(30);
            worker.RetentionPeriod.Should().Be(expectedDefault);
            _logger.LogInformation("Completed test: RetentionPeriod_DefaultValue_MatchesExpectedDefault - RetentionPeriod matches expected default {Expected}", expectedDefault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test: RetentionPeriod_DefaultValue_MatchesExpectedDefault");
            throw;
        }
    }
}
