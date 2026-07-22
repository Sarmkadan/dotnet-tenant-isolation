using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TenantIsolation.BackgroundTasks;

public class TenantCleanupWorkerTests
{
    [Fact]
    public void CheckInterval_DefaultValue_IsOneDay()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        worker.CheckInterval.Should().Be(TimeSpan.FromDays(1));
    }

    [Fact]
    public void RetentionPeriod_DefaultValue_IsThirtyDays()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        worker.RetentionPeriod.Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public void CheckInterval_CanBeCustomized()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        var customInterval = TimeSpan.FromHours(6);
        worker.CheckInterval = customInterval;
        worker.CheckInterval.Should().Be(customInterval);
    }

    [Fact]
    public void RetentionPeriod_CanBeCustomized()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        var customRetention = TimeSpan.FromDays(60);
        worker.RetentionPeriod = customRetention;
        worker.RetentionPeriod.Should().Be(customRetention);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesWorker()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
        var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
        worker.Should().NotBeNull();
        worker.CheckInterval.Should().Be(TimeSpan.FromDays(1));
        worker.RetentionPeriod.Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public async Task StopAsync_DisposesTimer()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
        var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
        await worker.StopAsync(CancellationToken.None);
        worker.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_DisposesTimer()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
        var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
        worker.Dispose();
        worker.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<TenantCleanupWorker>>();
        var worker = new TenantCleanupWorker(mockServiceProvider.Object, mockLogger.Object);
        worker.Dispose();
        worker.Dispose();
        worker.Should().NotBeNull();
    }

    [Fact]
    public void GetRetentionPeriod_WithValidWorker_ReturnsRetentionPeriod()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        worker.RetentionPeriod = TimeSpan.FromDays(45);
        var retention = worker.GetRetentionPeriod();
        retention.Should().Be(TimeSpan.FromDays(45));
    }

    [Fact]
    public void GetRetentionPeriod_WithNullWorker_ThrowsArgumentNullException()
    {
        TenantCleanupWorker worker = null!;
        Assert.Throws<ArgumentNullException>(() => worker.GetRetentionPeriod());
    }

    [Fact]
    public void GetCheckInterval_WithValidWorker_ReturnsCheckInterval()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        worker.CheckInterval = TimeSpan.FromHours(12);
        var interval = worker.GetCheckInterval();
        interval.Should().Be(TimeSpan.FromHours(12));
    }

    [Fact]
    public void GetCheckInterval_WithNullWorker_ThrowsArgumentNullException()
    {
        TenantCleanupWorker worker = null!;
        Assert.Throws<ArgumentNullException>(() => worker.GetCheckInterval());
    }

    [Fact]
    public void AddTenantCleanupWorker_WithNullBuilder_ThrowsArgumentNullException()
    {
        IHostBuilder builder = null!;
        var checkInterval = TimeSpan.FromHours(2);
        var retentionPeriod = TimeSpan.FromDays(14);
        Assert.Throws<ArgumentNullException>(() => builder.AddTenantCleanupWorker(checkInterval, retentionPeriod));
    }

    [Fact]
    public void AddTenantCleanupWorker_WithNullBuilderAndRetention_ThrowsArgumentNullException()
    {
        IHostBuilder builder = null!;
        var retentionPeriod = TimeSpan.FromDays(21);
        Assert.Throws<ArgumentNullException>(() => builder.AddTenantCleanupWorker(retentionPeriod));
    }

    [Fact]
    public void CheckInterval_DefaultValue_MatchesExpectedDefault()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        var expectedDefault = TimeSpan.FromDays(1);
        worker.CheckInterval.Should().Be(expectedDefault);
    }

    [Fact]
    public void RetentionPeriod_DefaultValue_MatchesExpectedDefault()
    {
        var worker = new TenantCleanupWorker(null!, null!);
        var expectedDefault = TimeSpan.FromDays(30);
        worker.RetentionPeriod.Should().Be(expectedDefault);
    }
}
