using GameConsole.AI.Models;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for AI performance-related classes.
/// </summary>
public class AIPerformanceTests
{
    [Fact]
    public void AIPerformanceMetrics_Should_Initialize_With_Defaults()
    {
        // Arrange & Act
        var metrics = new AIPerformanceMetrics();

        // Assert
        Assert.NotNull(metrics.AdditionalMetrics);
        Assert.True(metrics.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AIPerformanceMetrics_Should_Allow_Setting_Properties()
    {
        // Arrange
        var metrics = new AIPerformanceMetrics();

        // Act
        metrics.ExecutionTime = TimeSpan.FromMilliseconds(500);
        metrics.MemoryUsageBytes = 1024 * 1024 * 64; // 64 MB
        metrics.CpuUsagePercent = 75.5;
        metrics.GpuUsagePercent = 80.2;
        metrics.ThroughputOps = 1000.5;
        metrics.SuccessfulOperations = 100;
        metrics.FailedOperations = 2;
        metrics.AdditionalMetrics["custom_metric"] = 42.0;

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(500), metrics.ExecutionTime);
        Assert.Equal(1024 * 1024 * 64, metrics.MemoryUsageBytes);
        Assert.Equal(75.5, metrics.CpuUsagePercent);
        Assert.Equal(80.2, metrics.GpuUsagePercent);
        Assert.Equal(1000.5, metrics.ThroughputOps);
        Assert.Equal(100, metrics.SuccessfulOperations);
        Assert.Equal(2, metrics.FailedOperations);
        Assert.Equal(42.0, metrics.AdditionalMetrics["custom_metric"]);
    }

    [Fact]
    public void AIPerformanceEstimate_Should_Initialize_With_Defaults()
    {
        // Arrange & Act
        var estimate = new AIPerformanceEstimate();

        // Assert
        Assert.Equal(0.5, estimate.ConfidenceLevel);
        Assert.NotNull(estimate.AdditionalEstimates);
    }

    [Fact]
    public void AIPerformanceEstimate_Should_Allow_Setting_Properties()
    {
        // Arrange
        var estimate = new AIPerformanceEstimate();

        // Act
        estimate.EstimatedExecutionTime = TimeSpan.FromSeconds(2);
        estimate.EstimatedMemoryUsage = 1024 * 1024 * 128; // 128 MB
        estimate.EstimatedCpuUsage = 60.0;
        estimate.EstimatedGpuUsage = 70.0;
        estimate.ConfidenceLevel = 0.85;
        estimate.AdditionalEstimates["custom_estimate"] = 123.45;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(2), estimate.EstimatedExecutionTime);
        Assert.Equal(1024 * 1024 * 128, estimate.EstimatedMemoryUsage);
        Assert.Equal(60.0, estimate.EstimatedCpuUsage);
        Assert.Equal(70.0, estimate.EstimatedGpuUsage);
        Assert.Equal(0.85, estimate.ConfidenceLevel);
        Assert.Equal(123.45, estimate.AdditionalEstimates["custom_estimate"]);
    }
}