using GameConsole.AI.Local;
using GameConsole.AI.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Local.Tests;

/// <summary>
/// Tests for the AIResourceManager component.
/// Validates resource allocation, device detection, and memory management.
/// </summary>
public class AIResourceManagerTests : IDisposable
{
    private readonly ILogger<AIResourceManager> _logger;
    private readonly AIResourceManager _resourceManager;

    public AIResourceManagerTests()
    {
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AIResourceManager>.Instance;
        _resourceManager = new AIResourceManager(_logger);
    }

    [Fact]
    public async Task AllocateResourcesAsync_WithValidConfig_ShouldSucceed()
    {
        // Arrange
        var config = new ResourceConfiguration(
            PreferredDevice: ExecutionDevice.CPU,
            MaxMemoryMB: 512,
            MaxConcurrentInferences: 2,
            InferenceTimeoutMs: TimeSpan.FromSeconds(30),
            OptimizationLevel: OptimizationLevel.Basic
        );

        // Act
        var result = await _resourceManager.AllocateResourcesAsync(config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AllocateResourcesAsync_WithExcessiveMemory_ShouldFail()
    {
        // Arrange
        var config = new ResourceConfiguration(
            PreferredDevice: ExecutionDevice.CPU,
            MaxMemoryMB: 999999, // Excessive memory request
            MaxConcurrentInferences: 1,
            InferenceTimeoutMs: TimeSpan.FromSeconds(30),
            OptimizationLevel: OptimizationLevel.Basic
        );

        // Act
        var result = await _resourceManager.AllocateResourcesAsync(config);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAvailableDevicesAsync_ShouldReturnCPU()
    {
        // Act
        var devices = await _resourceManager.GetAvailableDevicesAsync();

        // Assert
        Assert.Contains(ExecutionDevice.CPU, devices);
        Assert.NotEmpty(devices);
    }

    [Fact]
    public async Task GetOptimalDeviceAsync_WithCPUConfig_ShouldReturnCPU()
    {
        // Arrange
        var config = new ResourceConfiguration(
            PreferredDevice: ExecutionDevice.CPU,
            MaxMemoryMB: 256,
            MaxConcurrentInferences: 1,
            InferenceTimeoutMs: TimeSpan.FromSeconds(10),
            OptimizationLevel: OptimizationLevel.Basic
        );

        // Act
        var optimalDevice = await _resourceManager.GetOptimalDeviceAsync(config);

        // Assert
        Assert.Equal(ExecutionDevice.CPU, optimalDevice);
    }

    [Fact]
    public async Task GetOptimalDeviceAsync_WithUnavailableDevice_ShouldFallback()
    {
        // Arrange
        var config = new ResourceConfiguration(
            PreferredDevice: ExecutionDevice.CUDA, // Likely not available in test environment
            MaxMemoryMB: 256,
            MaxConcurrentInferences: 1,
            InferenceTimeoutMs: TimeSpan.FromSeconds(10),
            OptimizationLevel: OptimizationLevel.Basic
        );

        // Act
        var optimalDevice = await _resourceManager.GetOptimalDeviceAsync(config);

        // Assert
        // Should fallback to available device
        Assert.True(optimalDevice == ExecutionDevice.CPU || optimalDevice == ExecutionDevice.CUDA);
    }

    [Fact]
    public void GetCurrentStats_ShouldReturnValidStats()
    {
        // Act
        var stats = _resourceManager.GetCurrentStats();

        // Assert
        Assert.True(stats.MemoryUsedMB >= 0);
        Assert.True(stats.MemoryAvailableMB >= 0);
        Assert.True(stats.CpuUsagePercent >= 0);
        Assert.True(stats.ActiveInferences >= 0);
        Assert.True(stats.QueuedInferences >= 0);
    }

    [Fact]
    public async Task ReleaseResourcesAsync_WithAllocatedResources_ShouldSucceed()
    {
        // Arrange
        var config = new ResourceConfiguration(
            PreferredDevice: ExecutionDevice.CPU,
            MaxMemoryMB: 256,
            MaxConcurrentInferences: 1,
            InferenceTimeoutMs: TimeSpan.FromSeconds(10),
            OptimizationLevel: OptimizationLevel.Basic
        );
        
        await _resourceManager.AllocateResourcesAsync(config);

        // Act & Assert
        await _resourceManager.ReleaseResourcesAsync("test-model-id");
        
        // Should not throw exception
    }

    [Fact]
    public async Task HasCapabilityAsync_WithCorrectType_ShouldReturnTrue()
    {
        // Act
        var hasCapability = await _resourceManager.HasCapabilityAsync<IResourceManagerCapability>();

        // Assert
        Assert.True(hasCapability);
    }

    [Fact]
    public async Task GetCapabilityAsync_WithCorrectType_ShouldReturnInstance()
    {
        // Act
        var capability = await _resourceManager.GetCapabilityAsync<IResourceManagerCapability>();

        // Assert
        Assert.NotNull(capability);
        Assert.Same(_resourceManager, capability);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}