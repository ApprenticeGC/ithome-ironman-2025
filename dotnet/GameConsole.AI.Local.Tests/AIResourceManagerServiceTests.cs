using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Local.Tests;

public class AIResourceManagerServiceTests
{
    private readonly AIResourceManagerService _resourceManager;

    public AIResourceManagerServiceTests()
    {
        _resourceManager = new AIResourceManagerService(new NullLogger<AIResourceManagerService>());
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Assert
        Assert.NotNull(_resourceManager.CurrentUtilization);
        Assert.NotNull(_resourceManager.Constraints);
        Assert.True(_resourceManager.Constraints.MaxMemoryBytes > 0);
    }

    [Fact]
    public async Task AllocateResourcesAsync_WithValidRequest_ShouldReturnAllocation()
    {
        // Arrange
        var requiredMemory = 100 * 1024 * 1024; // 100MB
        var estimatedDuration = 1000; // 1 second

        // Act
        var allocation = await _resourceManager.AllocateResourcesAsync(requiredMemory, estimatedDuration);

        // Assert
        Assert.NotNull(allocation);
        Assert.Equal(requiredMemory, allocation.AllocatedMemoryBytes);
        Assert.True(allocation.IsActive);
        Assert.NotEqual(Guid.Empty.ToString(), allocation.Id);

        // Cleanup
        await _resourceManager.ReleaseResourcesAsync(allocation);
    }

    [Fact]
    public async Task AllocateResourcesAsync_ExceedingMaxMemory_ShouldThrowInsufficientMemoryException()
    {
        // Arrange
        var excessiveMemory = _resourceManager.Constraints.MaxMemoryBytes + 1024;

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientMemoryException>(() =>
            _resourceManager.AllocateResourcesAsync(excessiveMemory, 1000));
    }

    [Fact]
    public async Task ReleaseResourcesAsync_WithValidAllocation_ShouldSucceed()
    {
        // Arrange
        var allocation = await _resourceManager.AllocateResourcesAsync(1024 * 1024, 1000);

        // Act
        await _resourceManager.ReleaseResourcesAsync(allocation);

        // Assert
        Assert.False(allocation.IsActive);
    }

    [Fact]
    public async Task ReleaseResourcesAsync_WithNullAllocation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _resourceManager.ReleaseResourcesAsync(null!));
    }

    [Fact]
    public async Task OptimizeAllocationAsync_ShouldCompleteWithoutError()
    {
        // Act & Assert
        await _resourceManager.OptimizeAllocationAsync();
        // Should complete without throwing
    }

    [Fact]
    public void CurrentUtilization_ShouldProvideValidMetrics()
    {
        // Act
        var metrics = _resourceManager.CurrentUtilization;

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.CpuUtilizationPercent >= 0);
        Assert.True(metrics.GpuUtilizationPercent >= 0);
        Assert.True(metrics.MemoryUsageBytes >= 0);
        Assert.True(metrics.RecordedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var allocation = await _resourceManager.AllocateResourcesAsync(1024 * 1024, 1000);

        // Act
        await _resourceManager.DisposeAsync();

        // Assert
        // Should complete without throwing
        Assert.False(allocation.IsActive);
    }

    [Fact]
    public async Task MultipleAllocationsAndReleases_ShouldMaintainCorrectState()
    {
        // Arrange
        var allocations = new List<ResourceAllocation>();
        
        // Act - Allocate multiple resources
        for (int i = 0; i < 5; i++)
        {
            var allocation = await _resourceManager.AllocateResourcesAsync(1024 * 1024, 1000);
            allocations.Add(allocation);
        }

        // Release some resources
        for (int i = 0; i < 3; i++)
        {
            await _resourceManager.ReleaseResourcesAsync(allocations[i]);
        }

        // Assert - Resource manager should handle this gracefully
        var metrics = _resourceManager.CurrentUtilization;
        Assert.NotNull(metrics);
        
        // Cleanup remaining allocations
        for (int i = 3; i < 5; i++)
        {
            await _resourceManager.ReleaseResourcesAsync(allocations[i]);
        }
    }
}