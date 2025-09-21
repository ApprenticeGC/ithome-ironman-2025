using GameConsole.Providers.Engine;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine.Tests;

/// <summary>
/// Tests for the ProviderPerformanceMonitor class.
/// </summary>
public class ProviderPerformanceMonitorTests
{
    private readonly FakeLogger<ProviderPerformanceMonitor> _logger;
    private readonly ProviderPerformanceMonitor _monitor;

    public ProviderPerformanceMonitorTests()
    {
        _logger = new FakeLogger<ProviderPerformanceMonitor>();
        _monitor = new ProviderPerformanceMonitor(_logger);
    }

    [Fact]
    public async Task RecordSuccessAsync_ShouldUpdateMetrics()
    {
        // Arrange
        const string providerId = "test-provider";
        var responseTime = TimeSpan.FromMilliseconds(150);

        // Act
        await _monitor.RecordSuccessAsync(providerId, responseTime);
        var metrics = await _monitor.GetMetricsAsync(providerId);

        // Assert
        Assert.Equal(1, metrics.RequestCount);
        Assert.Equal(0, metrics.FailureCount);
        Assert.Equal(100.0, metrics.SuccessRate);
        Assert.Equal(150.0, metrics.AverageResponseTime);
        Assert.True(metrics.HealthScore > 0);
        Assert.True(metrics.LastUpdated > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RecordFailureAsync_ShouldUpdateMetrics()
    {
        // Arrange
        const string providerId = "test-provider";
        var error = new InvalidOperationException("Test error");

        // Act
        await _monitor.RecordFailureAsync(providerId, error);
        var metrics = await _monitor.GetMetricsAsync(providerId);

        // Assert
        Assert.Equal(1, metrics.RequestCount);
        Assert.Equal(1, metrics.FailureCount);
        Assert.Equal(0.0, metrics.SuccessRate);
        Assert.Equal(error, metrics.LastError);
        Assert.True(metrics.HealthScore >= 0);
    }

    [Fact]
    public async Task IsHealthyAsync_ShouldReturnTrue_ForHealthyProvider()
    {
        // Arrange
        const string providerId = "healthy-provider";
        await _monitor.RecordSuccessAsync(providerId, TimeSpan.FromMilliseconds(100));
        await _monitor.RecordSuccessAsync(providerId, TimeSpan.FromMilliseconds(120));

        // Act
        var isHealthy = await _monitor.IsHealthyAsync(providerId);

        // Assert
        Assert.True(isHealthy);
    }

    [Fact]
    public async Task IsHealthyAsync_ShouldReturnFalse_ForUnhealthyProvider()
    {
        // Arrange
        const string providerId = "unhealthy-provider";
        for (int i = 0; i < 10; i++)
        {
            await _monitor.RecordFailureAsync(providerId, new Exception("Test error"));
        }

        // Act
        var isHealthy = await _monitor.IsHealthyAsync(providerId);

        // Assert
        Assert.False(isHealthy);
    }

    [Fact]
    public async Task GetAllMetricsAsync_ShouldReturnAllProviders()
    {
        // Arrange
        await _monitor.RecordSuccessAsync("provider1", TimeSpan.FromMilliseconds(100));
        await _monitor.RecordSuccessAsync("provider2", TimeSpan.FromMilliseconds(200));

        // Act
        var allMetrics = await _monitor.GetAllMetricsAsync();

        // Assert
        Assert.Equal(2, allMetrics.Count);
        Assert.Contains("provider1", allMetrics.Keys);
        Assert.Contains("provider2", allMetrics.Keys);
    }

    [Fact]
    public async Task ResetMetricsAsync_ShouldResetProviderMetrics()
    {
        // Arrange
        const string providerId = "reset-provider";
        await _monitor.RecordSuccessAsync(providerId, TimeSpan.FromMilliseconds(100));
        await _monitor.RecordFailureAsync(providerId, new Exception("Test"));

        // Act
        await _monitor.ResetMetricsAsync(providerId);
        var metrics = await _monitor.GetMetricsAsync(providerId);

        // Assert
        Assert.Equal(0, metrics.RequestCount);
        Assert.Equal(0, metrics.FailureCount);
        Assert.Equal(100.0, metrics.SuccessRate); // Default for no requests
        Assert.Null(metrics.LastError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordSuccessAsync_ShouldThrow_ForInvalidProviderId(string providerId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _monitor.RecordSuccessAsync(providerId, TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public async Task RecordFailureAsync_ShouldThrow_ForNullError()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _monitor.RecordFailureAsync("test", null!));
    }
}