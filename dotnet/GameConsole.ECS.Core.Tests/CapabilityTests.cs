using GameConsole.ECS.Core;
using Moq;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Tests for ECS capability interfaces.
/// </summary>
public class ECSCapabilitiesTests
{
    [Fact]
    public void ComponentPoolingCapability_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange
        var capability = new Mock<IComponentPoolingCapability>().Object;

        // Act & Assert
        Assert.IsAssignableFrom<GameConsole.Core.Abstractions.ICapabilityProvider>(capability);
    }

    [Fact]
    public async Task ComponentPoolingCapability_Should_Configure_Pooling()
    {
        // Arrange
        var capability = new Mock<IComponentPoolingCapability>();
        capability.Setup(c => c.ConfigurePoolingAsync<PositionComponent>(16, 0, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await capability.Object.ConfigurePoolingAsync<PositionComponent>(16, 0);

        // Verify
        capability.Verify(c => c.ConfigurePoolingAsync<PositionComponent>(16, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ComponentPoolingCapability_Should_Return_Pool_Stats()
    {
        // Arrange
        var stats = new ComponentPoolStats(typeof(PositionComponent), 5, 10, 20, 15, 100);
        var capability = new Mock<IComponentPoolingCapability>();
        capability.Setup(c => c.GetPoolStatsAsync<PositionComponent>(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(stats);

        // Act
        var result = await capability.Object.GetPoolStatsAsync<PositionComponent>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(PositionComponent), result.ComponentType);
        Assert.Equal(5, result.PooledCount);
        Assert.Equal(10, result.ActiveCount);
        Assert.Equal(20, result.TotalCreated);
        Assert.Equal(15, result.TotalReturned);
        Assert.Equal(100, result.MaxPoolSize);
        Assert.Equal(0.75f, result.HitRatio);
    }

    [Fact]
    public void ProfilingCapability_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange
        var capability = new Mock<IECSProfilingCapability>().Object;

        // Act & Assert
        Assert.IsAssignableFrom<GameConsole.Core.Abstractions.ICapabilityProvider>(capability);
    }

    [Fact]
    public async Task ProfilingCapability_Should_Enable_Profiling()
    {
        // Arrange
        var capability = new Mock<IECSProfilingCapability>();
        capability.Setup(c => c.SetProfilingEnabledAsync(true, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await capability.Object.SetProfilingEnabledAsync(true);

        // Verify
        capability.Verify(c => c.SetProfilingEnabledAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProfilingCapability_Should_Return_Performance_Stats()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var stats = new ECSPerformanceStats(worldId, 1000, 16.5, 10.0, 25.0, 100, 5, 1024000, 60.0);
        var capability = new Mock<IECSProfilingCapability>();
        capability.Setup(c => c.GetPerformanceStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(stats);

        // Act
        var result = await capability.Object.GetPerformanceStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(worldId, result.WorldId);
        Assert.Equal(1000, result.UpdateCycles);
        Assert.Equal(16.5, result.AverageUpdateTime);
        Assert.Equal(10.0, result.MinUpdateTime);
        Assert.Equal(25.0, result.MaxUpdateTime);
        Assert.Equal(100, result.EntityCount);
        Assert.Equal(5, result.SystemCount);
        Assert.Equal(1024000, result.ComponentMemoryUsage);
        Assert.Equal(60.0, result.FramesPerSecond);
    }

    [Fact]
    public async Task ProfilingCapability_Should_Return_System_Stats()
    {
        // Arrange
        var systemType = typeof(TestSystem);
        var stats = new SystemPerformanceStats(systemType, 500, 2.5, 1.0, 5.0, 1250.0, 10, true);
        var capability = new Mock<IECSProfilingCapability>();
        var system = new Mock<ISystem>().Object;
        capability.Setup(c => c.GetSystemStatsAsync(system, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(stats);

        // Act
        var result = await capability.Object.GetSystemStatsAsync(system);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(systemType, result.SystemType);
        Assert.Equal(500, result.UpdateCalls);
        Assert.Equal(2.5, result.AverageExecutionTime);
        Assert.Equal(1.0, result.MinExecutionTime);
        Assert.Equal(5.0, result.MaxExecutionTime);
        Assert.Equal(1250.0, result.TotalExecutionTime);
        Assert.Equal(10, result.Priority);
        Assert.True(result.CanExecuteInParallel);
    }

    [Fact]
    public void SerializationCapability_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange
        var capability = new Mock<IECSSerializationCapability>().Object;

        // Act & Assert
        Assert.IsAssignableFrom<GameConsole.Core.Abstractions.ICapabilityProvider>(capability);
    }

    [Fact]
    public async Task SerializationCapability_Should_Serialize_To_Stream()
    {
        // Arrange
        var capability = new Mock<IECSSerializationCapability>();
        var stream = new MemoryStream();
        capability.Setup(c => c.SerializeAsync(stream, SerializationFormat.Binary, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await capability.Object.SerializeAsync(stream, SerializationFormat.Binary);

        // Verify
        capability.Verify(c => c.SerializeAsync(stream, SerializationFormat.Binary, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SerializationCapability_Should_Deserialize_From_Stream()
    {
        // Arrange
        var capability = new Mock<IECSSerializationCapability>();
        var stream = new MemoryStream();
        capability.Setup(c => c.DeserializeAsync(stream, SerializationFormat.Json, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await capability.Object.DeserializeAsync(stream, SerializationFormat.Json);

        // Verify
        capability.Verify(c => c.DeserializeAsync(stream, SerializationFormat.Json, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SerializationCapability_Should_Save_World()
    {
        // Arrange
        var capability = new Mock<IECSSerializationCapability>();
        var filePath = "/tmp/test_world.bin";
        capability.Setup(c => c.SaveWorldAsync(filePath, SerializationFormat.Binary, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await capability.Object.SaveWorldAsync(filePath, SerializationFormat.Binary);

        // Verify
        capability.Verify(c => c.SaveWorldAsync(filePath, SerializationFormat.Binary, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SerializationCapability_Should_Load_World()
    {
        // Arrange
        var capability = new Mock<IECSSerializationCapability>();
        var filePath = "/tmp/test_world.bin";
        capability.Setup(c => c.LoadWorldAsync(filePath, SerializationFormat.Binary, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await capability.Object.LoadWorldAsync(filePath, SerializationFormat.Binary);

        // Verify
        capability.Verify(c => c.LoadWorldAsync(filePath, SerializationFormat.Binary, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Test helper class
    private class TestSystem : ISystem
    {
        public int Priority => 10;
        public bool CanExecuteInParallel => true;
        public IReadOnlySet<Type> ComponentTypes => new HashSet<Type>();
        public bool IsRunning => false;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task UpdateAsync(IECSWorld world, float deltaTime, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}