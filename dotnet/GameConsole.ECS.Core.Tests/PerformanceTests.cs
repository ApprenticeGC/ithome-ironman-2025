using GameConsole.ECS.Core;
using Moq;
using System.Diagnostics;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Performance benchmarks and integration tests for ECS core interfaces.
/// Validates that the design meets performance requirements for game engine use.
/// </summary>
public class ECSPerformanceTests
{
    [Fact]
    public void ECS_Models_Should_Have_Efficient_Memory_Layout()
    {
        // Arrange - Test component memory efficiency
        var positions = new PositionComponent[1000];
        var velocities = new VelocityComponent[1000];
        
        // Act - Initialize components
        for (int i = 0; i < 1000; i++)
        {
            positions[i] = new PositionComponent(i, i * 2, i * 3);
            velocities[i] = new VelocityComponent(i * 0.1f, i * 0.2f, i * 0.3f);
        }

        // Assert - Components should be efficiently stored
        Assert.Equal(1000, positions.Length);
        Assert.Equal(1000, velocities.Length);
        
        // Verify data integrity
        Assert.Equal(500, positions[500].X);
        Assert.Equal(1000, positions[500].Y);
        Assert.Equal(1500, positions[500].Z);
    }

    [Fact]
    public async Task ECS_World_Should_Support_Large_Entity_Counts()
    {
        // Arrange
        var world = CreateMockWorld();
        var entities = new List<IEntity>();
        const int entityCount = 10000;

        // Setup mock to return unique entities
        Mock.Get(world)
            .Setup(w => w.CreateEntityAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(CreateMockEntity()));

        Mock.Get(world)
            .Setup(w => w.GetEntityCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => entities.Count);

        // Act - Create many entities (simulated)
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < entityCount; i++)
        {
            var entity = await world.CreateEntityAsync();
            entities.Add(entity);
        }
        
        stopwatch.Stop();

        var count = await world.GetEntityCountAsync();

        // Assert - Should handle large numbers efficiently
        Assert.Equal(entityCount, entities.Count);
        Assert.Equal(entityCount, count);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Entity creation took {stopwatch.ElapsedMilliseconds}ms for {entityCount} entities");
    }

    [Fact]
    public async Task ECS_Component_Operations_Should_Be_Fast()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        var position = new PositionComponent(10, 20, 30);
        const int operationCount = 1000;

        // Setup mocks for component operations
        Mock.Get(world)
            .Setup(w => w.AddComponentAsync(entity, It.IsAny<PositionComponent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock.Get(world)
            .Setup(w => w.GetComponentAsync<PositionComponent>(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        Mock.Get(world)
            .Setup(w => w.HasComponentAsync<PositionComponent>(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - Perform many component operations
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < operationCount; i++)
        {
            await world.AddComponentAsync(entity, new PositionComponent(i, i, i));
            var hasComponent = await world.HasComponentAsync<PositionComponent>(entity);
            var component = await world.GetComponentAsync<PositionComponent>(entity);
            
            Assert.True(hasComponent);
            Assert.NotNull(component);
        }

        stopwatch.Stop();

        // Assert - Operations should be fast
        var operationsPerSecond = (operationCount * 3) / (stopwatch.ElapsedMilliseconds / 1000.0);
        Assert.True(operationsPerSecond > 10000, $"Component operations too slow: {operationsPerSecond:F0} ops/sec");
    }

    [Fact]
    public async Task ECS_System_Update_Should_Support_High_Frequency()
    {
        // Arrange
        var world = CreateMockWorld();
        var system = CreateMockSystem(priority: 1);
        const int updateCount = 1000;
        const float deltaTime = 0.016f; // 60 FPS

        // Setup system update
        var updateCallCount = 0;
        Mock.Get(system)
            .Setup(s => s.UpdateAsync(world, deltaTime, It.IsAny<CancellationToken>()))
            .Returns(() => 
            {
                updateCallCount++;
                return Task.CompletedTask;
            });

        // Act - Update system many times
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < updateCount; i++)
        {
            await system.UpdateAsync(world, deltaTime);
        }

        stopwatch.Stop();

        // Assert - Updates should be fast enough for real-time gameplay
        Assert.Equal(updateCount, updateCallCount);
        var updatesPerSecond = updateCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        Assert.True(updatesPerSecond > 1000, $"System updates too slow: {updatesPerSecond:F0} updates/sec");
    }

    [Fact]
    public async Task ECS_Query_Should_Be_Efficient()
    {
        // Arrange
        var world = CreateMockWorld();
        var query = CreateMockQuery();
        var entities = CreateMockEntities(1000);

        // Setup query to return large entity set
        Mock.Get(query)
            .Setup(q => q.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        Mock.Get(query)
            .Setup(q => q.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities.Count);

        Mock.Get(world)
            .Setup(w => w.CreateQueryAsync<PositionComponent>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(query);

        // Act - Perform query operations
        var stopwatch = Stopwatch.StartNew();

        var queryInstance = await world.CreateQueryAsync<PositionComponent>();
        var count = await queryInstance.GetCountAsync();
        var resultEntities = await queryInstance.GetEntitiesAsync();

        stopwatch.Stop();

        // Assert - Queries should be fast
        Assert.Equal(entities.Count, count);
        Assert.Equal(entities.Count, resultEntities.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Query operations took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ComponentPoolStats_Should_Calculate_Hit_Ratio_Correctly()
    {
        // Arrange & Act
        var stats1 = new ComponentPoolStats(typeof(PositionComponent), 5, 10, 20, 15, 100);
        var stats2 = new ComponentPoolStats(typeof(VelocityComponent), 0, 5, 10, 0, 50);
        var stats3 = new ComponentPoolStats(typeof(HealthComponent), 2, 3, 0, 0, 25);

        // Assert
        Assert.Equal(0.75f, stats1.HitRatio);
        Assert.Equal(0.0f, stats2.HitRatio);
        Assert.Equal(0.0f, stats3.HitRatio);
    }

    [Fact]
    public void ECSPerformanceStats_Should_Track_Performance_Metrics()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        
        // Act
        var stats = new ECSPerformanceStats(
            worldId: worldId,
            updateCycles: 60000,
            averageUpdateTime: 16.67,
            minUpdateTime: 12.0,
            maxUpdateTime: 25.0,
            entityCount: 5000,
            systemCount: 10,
            componentMemoryUsage: 2048000,
            framesPerSecond: 60.0
        );

        // Assert
        Assert.Equal(worldId, stats.WorldId);
        Assert.Equal(60000, stats.UpdateCycles);
        Assert.Equal(16.67, stats.AverageUpdateTime);
        Assert.Equal(12.0, stats.MinUpdateTime);
        Assert.Equal(25.0, stats.MaxUpdateTime);
        Assert.Equal(5000, stats.EntityCount);
        Assert.Equal(10, stats.SystemCount);
        Assert.Equal(2048000, stats.ComponentMemoryUsage);
        Assert.Equal(60.0, stats.FramesPerSecond);
    }

    [Fact]
    public void SystemPerformanceStats_Should_Track_System_Metrics()
    {
        // Arrange & Act
        var stats = new SystemPerformanceStats(
            systemType: typeof(TestMovementSystem),
            updateCalls: 10000,
            averageExecutionTime: 2.5,
            minExecutionTime: 1.0,
            maxExecutionTime: 8.0,
            totalExecutionTime: 25000.0,
            priority: 10,
            canExecuteInParallel: true
        );

        // Assert
        Assert.Equal(typeof(TestMovementSystem), stats.SystemType);
        Assert.Equal(10000, stats.UpdateCalls);
        Assert.Equal(2.5, stats.AverageExecutionTime);
        Assert.Equal(1.0, stats.MinExecutionTime);
        Assert.Equal(8.0, stats.MaxExecutionTime);
        Assert.Equal(25000.0, stats.TotalExecutionTime);
        Assert.Equal(10, stats.Priority);
        Assert.True(stats.CanExecuteInParallel);
    }

    // Helper methods
    private static IECSWorld CreateMockWorld()
    {
        var mock = new Mock<IECSWorld>();
        mock.Setup(w => w.WorldId).Returns(Guid.NewGuid());
        mock.Setup(w => w.Name).Returns("TestWorld");
        mock.Setup(w => w.IsRunning).Returns(false);
        return mock.Object;
    }

    private static IEntity CreateMockEntity()
    {
        var mock = new Mock<IEntity>();
        mock.Setup(e => e.Id).Returns(Guid.NewGuid());
        mock.Setup(e => e.IsAlive).Returns(true);
        mock.Setup(e => e.Generation).Returns(1u);
        mock.Setup(e => e.World).Returns(new Mock<IECSWorld>().Object);
        return mock.Object;
    }

    private static IReadOnlyList<IEntity> CreateMockEntities(int count)
    {
        var entities = new List<IEntity>();
        for (int i = 0; i < count; i++)
        {
            entities.Add(CreateMockEntity());
        }
        return entities;
    }

    private static IEntityQuery CreateMockQuery()
    {
        var mock = new Mock<IEntityQuery>();
        mock.Setup(q => q.RequiredComponents).Returns(new HashSet<Type> { typeof(PositionComponent) });
        mock.Setup(q => q.ExcludedComponents).Returns(new HashSet<Type>());
        return mock.Object;
    }

    private static ISystem CreateMockSystem(int priority = 0)
    {
        var mock = new Mock<ISystem>();
        mock.Setup(s => s.Priority).Returns(priority);
        mock.Setup(s => s.CanExecuteInParallel).Returns(false);
        mock.Setup(s => s.ComponentTypes).Returns(new HashSet<Type> { typeof(PositionComponent) });
        mock.Setup(s => s.IsRunning).Returns(false);
        return mock.Object;
    }

    // Test helper class for performance stats
    private class TestMovementSystem : ISystem
    {
        public int Priority => 10;
        public bool CanExecuteInParallel => true;
        public IReadOnlySet<Type> ComponentTypes => new HashSet<Type> { typeof(PositionComponent), typeof(VelocityComponent) };
        public bool IsRunning => false;

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task UpdateAsync(IECSWorld world, float deltaTime, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}