using GameConsole.AI.Models;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for AI security and context-related classes.
/// </summary>
public class AISecurityTests
{
    [Fact]
    public void AISecuritySettings_Should_Initialize_With_Secure_Defaults()
    {
        // Arrange & Act
        var settings = new AISecuritySettings();

        // Assert
        Assert.True(settings.EnableSandboxing);
        Assert.Equal(TimeSpan.FromMinutes(5), settings.MaxExecutionTime);
        Assert.Equal(1024 * 1024 * 1024, settings.MaxMemoryAllocation); // 1 GB
        Assert.False(settings.EnableNetworkAccess);
        Assert.False(settings.EnableFileSystemAccess);
        Assert.NotNull(settings.AllowedOperations);
        Assert.NotNull(settings.BlockedOperations);
        Assert.NotNull(settings.AllowedFilePaths);
        Assert.NotNull(settings.AdditionalSettings);
    }

    [Fact]
    public void AISecuritySettings_Should_Allow_Customization()
    {
        // Arrange
        var settings = new AISecuritySettings();

        // Act
        settings.EnableSandboxing = false;
        settings.MaxExecutionTime = TimeSpan.FromMinutes(10);
        settings.MaxMemoryAllocation = 1024 * 1024 * 512; // 512 MB
        settings.EnableNetworkAccess = true;
        settings.EnableFileSystemAccess = true;
        settings.AllowedOperations.Add("read_data");
        settings.BlockedOperations.Add("write_system");
        settings.AllowedFilePaths.Add("/safe/path");
        settings.AdditionalSettings["custom_setting"] = "value";

        // Assert
        Assert.False(settings.EnableSandboxing);
        Assert.Equal(TimeSpan.FromMinutes(10), settings.MaxExecutionTime);
        Assert.Equal(1024 * 1024 * 512, settings.MaxMemoryAllocation);
        Assert.True(settings.EnableNetworkAccess);
        Assert.True(settings.EnableFileSystemAccess);
        Assert.Contains("read_data", settings.AllowedOperations);
        Assert.Contains("write_system", settings.BlockedOperations);
        Assert.Contains("/safe/path", settings.AllowedFilePaths);
        Assert.Equal("value", settings.AdditionalSettings["custom_setting"]);
    }

    [Fact]
    public void AIContextSettings_Should_Initialize_With_Defaults()
    {
        // Arrange & Act
        var settings = new AIContextSettings();

        // Assert
        Assert.NotNull(settings.ResourceRequirements);
        Assert.NotNull(settings.SecuritySettings);
        Assert.True(settings.EnablePerformanceMonitoring);
        Assert.Equal(TimeSpan.FromSeconds(1), settings.PerformanceMonitoringInterval);
        Assert.True(settings.EnableLogging);
        Assert.Equal("Information", settings.LogLevel);
        Assert.NotNull(settings.AdditionalSettings);
    }

    [Fact]
    public void AIContextSettings_Should_Allow_Customization()
    {
        // Arrange
        var settings = new AIContextSettings();
        var customResourceRequirements = new AIResourceRequirements { MinimumCpuCores = 4 };
        var customSecuritySettings = new AISecuritySettings { EnableSandboxing = false };

        // Act
        settings.ResourceRequirements = customResourceRequirements;
        settings.SecuritySettings = customSecuritySettings;
        settings.EnablePerformanceMonitoring = false;
        settings.PerformanceMonitoringInterval = TimeSpan.FromSeconds(5);
        settings.EnableLogging = false;
        settings.LogLevel = "Debug";
        settings.AdditionalSettings["test"] = 42;

        // Assert
        Assert.Equal(customResourceRequirements, settings.ResourceRequirements);
        Assert.Equal(customSecuritySettings, settings.SecuritySettings);
        Assert.False(settings.EnablePerformanceMonitoring);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.PerformanceMonitoringInterval);
        Assert.False(settings.EnableLogging);
        Assert.Equal("Debug", settings.LogLevel);
        Assert.Equal(42, settings.AdditionalSettings["test"]);
    }
}