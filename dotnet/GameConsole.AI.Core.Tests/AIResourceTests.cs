using GameConsole.AI.Models;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for AI resource-related classes.
/// </summary>
public class AIResourceTests
{
    [Fact]
    public void AIResourceRequirements_Should_Initialize_With_Defaults()
    {
        // Arrange & Act
        var requirements = new AIResourceRequirements();

        // Assert
        Assert.Equal(1, requirements.MinimumCpuCores);
        Assert.Equal(1, requirements.RecommendedCpuCores);
        Assert.Single(requirements.PreferredProcessingUnits);
        Assert.Equal(AIProcessingUnit.CPU, requirements.PreferredProcessingUnits[0]);
        Assert.NotNull(requirements.AdditionalRequirements);
    }

    [Fact]
    public void AIResourceRequirements_Should_Allow_Setting_Properties()
    {
        // Arrange
        var requirements = new AIResourceRequirements();

        // Act
        requirements.MinimumMemoryBytes = 1024 * 1024 * 512; // 512 MB
        requirements.RecommendedMemoryBytes = 1024 * 1024 * 1024; // 1 GB
        requirements.MinimumCpuCores = 2;
        requirements.RecommendedCpuCores = 4;
        requirements.MinimumGpuMemoryBytes = 1024 * 1024 * 256; // 256 MB
        requirements.RequiresDedicatedGpu = true;
        requirements.EstimatedExecutionTime = TimeSpan.FromSeconds(30);
        requirements.PreferredProcessingUnits.Clear();
        requirements.PreferredProcessingUnits.Add(AIProcessingUnit.GPU);

        // Assert
        Assert.Equal(1024 * 1024 * 512, requirements.MinimumMemoryBytes);
        Assert.Equal(1024 * 1024 * 1024, requirements.RecommendedMemoryBytes);
        Assert.Equal(2, requirements.MinimumCpuCores);
        Assert.Equal(4, requirements.RecommendedCpuCores);
        Assert.Equal(1024 * 1024 * 256, requirements.MinimumGpuMemoryBytes);
        Assert.True(requirements.RequiresDedicatedGpu);
        Assert.Equal(TimeSpan.FromSeconds(30), requirements.EstimatedExecutionTime);
        Assert.Single(requirements.PreferredProcessingUnits);
        Assert.Equal(AIProcessingUnit.GPU, requirements.PreferredProcessingUnits[0]);
    }

    [Fact]
    public void AIResourceAllocation_Should_Initialize_With_Id()
    {
        // Arrange
        var allocationId = "allocation-123";

        // Act
        var allocation = new AIResourceAllocation(allocationId);

        // Assert
        Assert.Equal(allocationId, allocation.AllocationId);
        Assert.NotNull(allocation.AllocatedProcessingUnits);
        Assert.NotNull(allocation.Properties);
        Assert.True(allocation.AllocationTime <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AIResourceAllocation_Should_Throw_ArgumentNullException_For_Null_Id()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIResourceAllocation(null!));
    }
}