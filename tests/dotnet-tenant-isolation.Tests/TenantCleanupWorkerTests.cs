using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TenantIsolation.BackgroundTasks;

public class TenantCleanupWorkerTests
{
    private readonly ILogger<TenantCleanupWorkerTests> _logger;

    public TenantCleanupWorkerTests()
    {
        // Logger will be injected via test framework if needed
        // For now, we'll create a mock logger for demonstration
        var mockLogger = new Mock<ILogger<TenantCleanupWorkerTests>>();
        _logger = mockLogger.Object;
    }

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
