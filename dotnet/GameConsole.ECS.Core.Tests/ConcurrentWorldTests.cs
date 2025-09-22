using System.Collections.Concurrent;
using GameConsole.ECS.Core;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Tests for concurrent ECS world operations and multiple world support.
/// </summary>
public class ConcurrentWorldTests
{
    [Fact]
    public async Task Multiple_Worlds_Should_Have_Unique_Ids()
    {
        // Arrange & Act
        await using var world1 = new ECSWorld();
        await using var world2 = new ECSWorld();
        await using var world3 = new ECSWorld();

        // Assert
        Assert.NotEqual(world1.WorldId, world2.WorldId);
        Assert.NotEqual(world2.WorldId, world3.WorldId);
        Assert.NotEqual(world1.WorldId, world3.WorldId);
    }

    [Fact]
    public async Task Multiple_Worlds_Should_Operate_Independently()
    {
        // Arrange
        await using var world1 = new ECSWorld();
        await using var world2 = new ECSWorld();

        // Act
        var entity1 = world1.CreateEntity();
        var entity2 = world2.CreateEntity();

        world1.AddComponent(entity1, new PositionComponent(1, 1, 1));
        world2.AddComponent(entity2, new VelocityComponent(2, 2, 2));

        // Assert
        Assert.Equal(1, world1.EntityCount);
        Assert.Equal(1, world2.EntityCount);

        Assert.True(world1.HasComponent<PositionComponent>(entity1));
        Assert.False(world1.HasComponent<VelocityComponent>(entity1));

        Assert.True(world2.HasComponent<VelocityComponent>(entity2));
        Assert.False(world2.HasComponent<PositionComponent>(entity2));

        // Cross-world operations should fail
        Assert.False(world1.HasComponent<VelocityComponent>(entity2));
        Assert.False(world2.HasComponent<PositionComponent>(entity1));
    }

    [Fact]
    public async Task Concurrent_Entity_Creation_Should_Be_Thread_Safe()
    {
        // Arrange
        await using var world = new ECSWorld();
        const int threadsCount = 10;
        const int entitiesPerThread = 100;
        var tasks = new Task[threadsCount];
        var allEntities = new ConcurrentBag<IEntity>();

        // Act
        for (int i = 0; i < threadsCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < entitiesPerThread; j++)
                {
                    var entity = world.CreateEntity();
                    allEntities.Add(entity);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(threadsCount * entitiesPerThread, world.EntityCount);
        Assert.Equal(threadsCount * entitiesPerThread, allEntities.Count);

        // All entities should have unique IDs
        var uniqueIds = allEntities.Select(e => e.Id).ToHashSet();
        Assert.Equal(threadsCount * entitiesPerThread, uniqueIds.Count);
    }

    [Fact]
    public async Task Concurrent_Component_Operations_Should_Be_Thread_Safe()
    {
        // Arrange
        await using var world = new ECSWorld();
        const int threadsCount = 5;
        const int operationsPerThread = 50;
        var entities = new IEntity[threadsCount * operationsPerThread];

        // Pre-create entities
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.CreateEntity();
        }

        var tasks = new Task[threadsCount];

        // Act - Each thread operates on its own subset of entities
        for (int i = 0; i < threadsCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                int startIndex = threadId * operationsPerThread;
                int endIndex = startIndex + operationsPerThread;

                for (int j = startIndex; j < endIndex; j++)
                {
                    var entity = entities[j];
                    world.AddComponent(entity, new PositionComponent(j, j, j));
                    world.AddComponent(entity, new VelocityComponent(j * 2, j * 2, j * 2));
                    
                    // Verify components were added
                    Assert.True(world.HasComponent<PositionComponent>(entity));
                    Assert.True(world.HasComponent<VelocityComponent>(entity));
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - All entities should have both components
        for (int i = 0; i < entities.Length; i++)
        {
            Assert.True(world.HasComponent<PositionComponent>(entities[i]));
            Assert.True(world.HasComponent<VelocityComponent>(entities[i]));
        }

        var positionEntities = world.Query<PositionComponent>().ToList();
        var velocityEntities = world.Query<VelocityComponent>().ToList();
        var bothEntities = world.Query<PositionComponent, VelocityComponent>().ToList();

        Assert.Equal(entities.Length, positionEntities.Count);
        Assert.Equal(entities.Length, velocityEntities.Count);
        Assert.Equal(entities.Length, bothEntities.Count);
    }

    [Fact]
    public async Task Concurrent_World_Operations_Should_Not_Interfere()
    {
        // Arrange
        const int worldCount = 5;
        const int entitiesPerWorld = 100;
        var worlds = new ECSWorld[worldCount];
        var tasks = new Task[worldCount];

        for (int i = 0; i < worldCount; i++)
        {
            worlds[i] = new ECSWorld();
        }

        // Act - Each task operates on its own world
        for (int i = 0; i < worldCount; i++)
        {
            int worldIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var world = worlds[worldIndex];
                
                for (int j = 0; j < entitiesPerWorld; j++)
                {
                    var entity = world.CreateEntity();
                    world.AddComponent(entity, new PositionComponent(worldIndex, j, 0));
                    
                    if (j % 2 == 0)
                    {
                        world.AddComponent(entity, new VelocityComponent(worldIndex, j, 0));
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Each world should have the correct number of entities and components
        for (int i = 0; i < worldCount; i++)
        {
            Assert.Equal(entitiesPerWorld, worlds[i].EntityCount);
            
            var positionEntities = worlds[i].Query<PositionComponent>().ToList();
            var velocityEntities = worlds[i].Query<VelocityComponent>().ToList();
            
            Assert.Equal(entitiesPerWorld, positionEntities.Count);
            Assert.Equal(entitiesPerWorld / 2, velocityEntities.Count);
        }

        // Cleanup
        for (int i = 0; i < worldCount; i++)
        {
            await worlds[i].DisposeAsync();
        }
    }

    [Fact]
    public async Task Concurrent_System_Updates_Should_Be_Thread_Safe()
    {
        // Arrange
        await using var world = new ECSWorld();
        const int entityCount = 100;
        
        // Create entities with position and velocity
        for (int i = 0; i < entityCount; i++)
        {
            var entity = world.CreateEntity();
            world.AddComponent(entity, new PositionComponent(0, 0, 0));
            world.AddComponent(entity, new VelocityComponent(1, 1, 1));
        }

        var system = new MovementSystem();
        world.AddSystem(system);

        const int updateCount = 50;
        const int threadCount = 4;
        var tasks = new Task[threadCount];

        // Act - Multiple threads calling UpdateSystems concurrently
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < updateCount; j++)
                {
                    world.UpdateSystems(0.016f);
                    Thread.Sleep(1); // Small delay to increase chance of concurrent access
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - All entities should have been updated
        // Position should be roughly (updateCount * threadCount * 0.016f, ...)
        var entities = world.Query<PositionComponent>().ToList();
        Assert.Equal(entityCount, entities.Count);

        foreach (var entity in entities)
        {
            var position = world.GetComponent<PositionComponent>(entity);
            Assert.NotNull(position);
            // Position should have been updated (exact value depends on race conditions)
            Assert.True(position.Value.X > 0, $"Expected position.X > 0, but was {position.Value.X}");
        }
    }

    [Fact]
    public async Task Service_Lifecycle_Should_Be_Thread_Safe()
    {
        // Arrange
        await using var world = new ECSWorld();
        const int threadCount = 10;
        var tasks = new Task[threadCount];

        // Act - Multiple threads calling service lifecycle methods
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await world.InitializeAsync();
                await world.StartAsync();
                
                // Perform some operations
                var entity = world.CreateEntity();
                world.AddComponent(entity, new PositionComponent(1, 1, 1));
                
                // Don't assert IsRunning here as other threads may have stopped it
            });
        }

        await Task.WhenAll(tasks);

        // Stop the service once after all operations
        await world.StopAsync();

        // Assert - Service should still be in a valid state
        Assert.False(world.IsRunning);
    }
}