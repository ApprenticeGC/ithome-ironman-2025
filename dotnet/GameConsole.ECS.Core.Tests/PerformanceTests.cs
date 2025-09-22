using System.Diagnostics;
using GameConsole.ECS.Core;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Performance tests to validate ECS operations meet game engine requirements.
/// </summary>
public class PerformanceTests : IAsyncDisposable
{
    private readonly ECSWorld _world;

    public PerformanceTests()
    {
        _world = new ECSWorld();
    }

    [Fact]
    public void Entity_Creation_Should_Be_Fast()
    {
        // Arrange
        const int entityCount = 10000;
        
        // Act - Measure entity creation time
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < entityCount; i++)
        {
            _world.CreateEntity();
        }
        stopwatch.Stop();

        // Assert - Should create 10k entities in under 100ms
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / entityCount;
        Assert.True(averageTime < 0.01, $"Average entity creation time {averageTime:F4}ms exceeds 0.01ms requirement");
        Assert.Equal(entityCount, _world.EntityCount);
    }

    [Fact]
    public void Component_Addition_Should_Be_Fast()
    {
        // Arrange
        const int operationCount = 10000;
        var entities = new List<IEntity>(operationCount);
        
        for (int i = 0; i < operationCount; i++)
        {
            entities.Add(_world.CreateEntity());
        }

        // Act - Measure component addition time
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < operationCount; i++)
        {
            _world.AddComponent(entities[i], new PositionComponent(i, i, i));
        }
        stopwatch.Stop();

        // Assert - Should add 10k components in under 50ms
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / operationCount;
        Assert.True(averageTime < 0.005, $"Average component addition time {averageTime:F4}ms exceeds 0.005ms requirement");
    }

    [Fact]
    public void Component_Query_Should_Be_Fast()
    {
        // Arrange
        const int entityCount = 10000;
        
        // Create entities with different component combinations
        for (int i = 0; i < entityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new PositionComponent(i, i, i));
            
            if (i % 2 == 0)
            {
                _world.AddComponent(entity, new VelocityComponent(i, i, i));
            }
            
            if (i % 3 == 0)
            {
                _world.AddComponent(entity, new HealthComponent(100, 100));
            }
        }

        // Warm up - first query
        _ = _world.Query<PositionComponent>().ToList();
        _ = _world.Query<PositionComponent, VelocityComponent>().ToList();

        // Act - Measure query performance
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _world.Query<PositionComponent>().ToList();
        }
        stopwatch.Stop();

        // Assert - Should execute 1k queries in under 2ms
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 1000;
        Assert.True(averageTime < 2.0, $"Average query time {averageTime:F4}ms exceeds 2.0ms requirement");
    }

    [Fact]
    public void Multi_Component_Query_Should_Be_Fast()
    {
        // Arrange
        const int entityCount = 10000;
        
        for (int i = 0; i < entityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new PositionComponent(i, i, i));
            
            if (i % 2 == 0)
            {
                _world.AddComponent(entity, new VelocityComponent(i, i, i));
            }
        }

        // Warm up
        _ = _world.Query<PositionComponent, VelocityComponent>().ToList();

        // Act - Measure multi-component query performance
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _world.Query<PositionComponent, VelocityComponent>().ToList();
        }
        stopwatch.Stop();

        // Assert - Should execute 1k multi-component queries in under 5ms
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 1000;
        Assert.True(averageTime < 5.0, $"Average multi-component query time {averageTime:F4}ms exceeds 5.0ms requirement");
    }

    [Fact]
    public void Component_Access_Should_Be_Fast()
    {
        // Arrange
        const int entityCount = 1000;
        var entities = new List<IEntity>(entityCount);
        
        for (int i = 0; i < entityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new PositionComponent(i, i, i));
            entities.Add(entity);
        }

        // Act - Measure component access time
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var entity = entities[i % entityCount];
            _ = _world.GetComponent<PositionComponent>(entity);
        }
        stopwatch.Stop();

        // Assert - Should perform 10k component accesses in under 50ms
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 10000;
        Assert.True(averageTime < 0.005, $"Average component access time {averageTime:F4}ms exceeds 0.005ms requirement");
    }

    [Fact]
    public void System_Update_Should_Be_Fast()
    {
        // Arrange
        const int entityCount = 1000;
        
        for (int i = 0; i < entityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new PositionComponent(i, i, i));
            _world.AddComponent(entity, new VelocityComponent(1, 1, 1));
        }

        var system = new MovementSystem();
        _world.AddSystem(system);

        // Warm up
        _world.UpdateSystems(0.016f);

        // Act - Measure system update time
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _world.UpdateSystems(0.016f);
        }
        stopwatch.Stop();

        // Assert - Should update 1k entities 100 times in under 500ms
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 100;
        Assert.True(averageTime < 5.0, $"Average system update time {averageTime:F4}ms exceeds 5ms requirement");
    }

    [Fact]
    public void Memory_Usage_Should_Remain_Stable_During_Component_Operations()
    {
        // Arrange
        const int cycles = 10;
        const int entitiesPerCycle = 1000;

        // Warm up and establish baseline
        for (int i = 0; i < entitiesPerCycle; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new PositionComponent(i, i, i));
        }
        
        GC.Collect(2, GCCollectionMode.Forced, true);
        var initialMemory = GC.GetTotalMemory(false);

        // Act - Multiple cycles of entity creation and component addition
        for (int cycle = 0; cycle < cycles; cycle++)
        {
            var cycleEntities = new List<IEntity>(entitiesPerCycle);
            
            // Create entities and add components
            for (int i = 0; i < entitiesPerCycle; i++)
            {
                var entity = _world.CreateEntity();
                _world.AddComponent(entity, new PositionComponent(i, i, i));
                _world.AddComponent(entity, new VelocityComponent(i, i, i));
                cycleEntities.Add(entity);
            }

            // Clean up cycle entities
            foreach (var entity in cycleEntities)
            {
                _world.DestroyEntity(entity);
            }
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        var finalMemory = GC.GetTotalMemory(false);

        // Assert - Memory growth should be minimal
        var memoryGrowth = finalMemory - initialMemory;
        Assert.True(memoryGrowth < 1024 * 1024, $"Memory grew by {memoryGrowth} bytes, which exceeds 1MB limit");
    }

    [Fact]
    public void Large_Scale_ECS_Operations_Should_Complete_Within_Frame_Budget()
    {
        // Arrange - Simulate a complex game scenario
        const int entityCount = 5000;
        var entities = new List<IEntity>(entityCount);

        // Create entities with various components
        for (int i = 0; i < entityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new PositionComponent(i, i, i));
            
            if (i % 2 == 0)
                _world.AddComponent(entity, new VelocityComponent(1, 1, 1));
            
            if (i % 3 == 0)
                _world.AddComponent(entity, new HealthComponent(100, 100));
                
            entities.Add(entity);
        }

        var system = new MovementSystem();
        _world.AddSystem(system);

        // Act - Simulate multiple game frames
        var frameCount = 60; // 1 second at 60 FPS
        var stopwatch = Stopwatch.StartNew();
        
        for (int frame = 0; frame < frameCount; frame++)
        {
            // System updates
            _world.UpdateSystems(1.0f / 60.0f);
            
            // Some component queries (typical in a game frame)
            _ = _world.Query<PositionComponent>().Take(100).ToList();
            _ = _world.Query<PositionComponent, VelocityComponent>().Take(100).ToList();
            
            // Some component updates (typical in a game frame)
            for (int i = 0; i < 10; i++)
            {
                var entity = entities[frame * 10 + i];
                if (_world.HasComponent<HealthComponent>(entity))
                {
                    var health = _world.GetComponent<HealthComponent>(entity);
                    if (health.HasValue)
                    {
                        var newHealth = new HealthComponent(health.Value.Health - 1, health.Value.MaxHealth);
                        _world.UpdateComponent(entity, newHealth);
                    }
                }
            }
        }
        
        stopwatch.Stop();

        // Assert - Should complete 60 complex frames in under 1 second (16.67ms per frame average)
        var averageFrameTime = stopwatch.Elapsed.TotalMilliseconds / frameCount;
        Assert.True(averageFrameTime < 16.67, $"Average frame time {averageFrameTime:F2}ms exceeds 16.67ms budget for 60 FPS");
    }

    public async ValueTask DisposeAsync()
    {
        await _world.DisposeAsync();
    }
}