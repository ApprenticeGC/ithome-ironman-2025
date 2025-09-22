using GameConsole.ECS.Core;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Tests for ECS component queries and system execution.
/// </summary>
public class QueryAndSystemTests : IAsyncDisposable
{
    private readonly ECSWorld _world;

    public QueryAndSystemTests()
    {
        _world = new ECSWorld();
    }

    [Fact]
    public void Single_Component_Query_Should_Return_Matching_Entities()
    {
        // Arrange
        var entity1 = _world.CreateEntity();
        var entity2 = _world.CreateEntity();
        var entity3 = _world.CreateEntity();

        _world.AddComponent(entity1, new PositionComponent(1, 1, 1));
        _world.AddComponent(entity2, new VelocityComponent(2, 2, 2));
        _world.AddComponent(entity3, new PositionComponent(3, 3, 3));

        // Act
        var positionEntities = _world.Query<PositionComponent>().ToList();

        // Assert
        Assert.Equal(2, positionEntities.Count);
        Assert.Contains(entity1, positionEntities);
        Assert.Contains(entity3, positionEntities);
        Assert.DoesNotContain(entity2, positionEntities);
    }

    [Fact]
    public void Multi_Component_Query_Should_Return_Entities_With_All_Components()
    {
        // Arrange
        var entity1 = _world.CreateEntity();
        var entity2 = _world.CreateEntity();
        var entity3 = _world.CreateEntity();
        var entity4 = _world.CreateEntity();

        // Entity1: Position only
        _world.AddComponent(entity1, new PositionComponent(1, 1, 1));

        // Entity2: Velocity only
        _world.AddComponent(entity2, new VelocityComponent(2, 2, 2));

        // Entity3: Both Position and Velocity
        _world.AddComponent(entity3, new PositionComponent(3, 3, 3));
        _world.AddComponent(entity3, new VelocityComponent(3, 3, 3));

        // Entity4: All three components
        _world.AddComponent(entity4, new PositionComponent(4, 4, 4));
        _world.AddComponent(entity4, new VelocityComponent(4, 4, 4));
        _world.AddComponent(entity4, new HealthComponent(100, 100));

        // Act
        var positionVelocityEntities = _world.Query<PositionComponent, VelocityComponent>().ToList();

        // Assert
        Assert.Equal(2, positionVelocityEntities.Count);
        Assert.Contains(entity3, positionVelocityEntities);
        Assert.Contains(entity4, positionVelocityEntities);
        Assert.DoesNotContain(entity1, positionVelocityEntities);
        Assert.DoesNotContain(entity2, positionVelocityEntities);
    }

    [Fact]
    public void Query_On_Empty_World_Should_Return_Empty_Result()
    {
        // Act
        var entities = _world.Query<PositionComponent>().ToList();

        // Assert
        Assert.Empty(entities);
    }

    [Fact]
    public void Query_For_Nonexistent_Component_Should_Return_Empty_Result()
    {
        // Arrange
        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new PositionComponent(1, 1, 1));

        // Act
        var healthEntities = _world.Query<HealthComponent>().ToList();

        // Assert
        Assert.Empty(healthEntities);
    }

    [Fact]
    public void System_Addition_Should_Work_Correctly()
    {
        // Arrange
        var system = new MovementSystem();

        // Act
        _world.AddSystem(system);

        // Assert - Can't directly verify system was added, but UpdateSystems shouldn't throw
        _world.UpdateSystems(0.016f);
    }

    [Fact]
    public void System_Removal_Should_Work_Correctly()
    {
        // Arrange
        var system = new MovementSystem();
        _world.AddSystem(system);

        // Act
        var result = _world.RemoveSystem(system);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Removing_Nonexistent_System_Should_Return_False()
    {
        // Arrange
        var system = new MovementSystem();

        // Act
        var result = _world.RemoveSystem(system);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void System_Update_Should_Process_Entities()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var initialPosition = new PositionComponent(0, 0, 0);
        var velocity = new VelocityComponent(1, 2, 3);

        _world.AddComponent(entity, initialPosition);
        _world.AddComponent(entity, velocity);

        var system = new MovementSystem();
        _world.AddSystem(system);

        // Act
        _world.UpdateSystems(1.0f); // 1 second delta

        // Assert
        var updatedPosition = _world.GetComponent<PositionComponent>(entity);
        Assert.NotNull(updatedPosition);
        Assert.Equal(1.0f, updatedPosition.Value.X);
        Assert.Equal(2.0f, updatedPosition.Value.Y);
        Assert.Equal(3.0f, updatedPosition.Value.Z);
    }

    [Fact]
    public void Disabled_System_Should_Not_Execute()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var initialPosition = new PositionComponent(0, 0, 0);
        var velocity = new VelocityComponent(1, 2, 3);

        _world.AddComponent(entity, initialPosition);
        _world.AddComponent(entity, velocity);

        var system = new MovementSystem { IsEnabled = false };
        _world.AddSystem(system);

        // Act
        _world.UpdateSystems(1.0f);

        // Assert
        var position = _world.GetComponent<PositionComponent>(entity);
        Assert.NotNull(position);
        Assert.Equal(0.0f, position.Value.X); // Should remain unchanged
        Assert.Equal(0.0f, position.Value.Y);
        Assert.Equal(0.0f, position.Value.Z);
    }

    [Fact]
    public void Systems_Should_Execute_In_Priority_Order()
    {
        // Arrange
        var executionOrder = new List<string>();

        var highPrioritySystem = new TestSystem("High", UpdatePriority.High, executionOrder);
        var normalPrioritySystem = new TestSystem("Normal", UpdatePriority.Normal, executionOrder);
        var lowPrioritySystem = new TestSystem("Low", UpdatePriority.Low, executionOrder);

        // Add systems in reverse priority order
        _world.AddSystem(lowPrioritySystem);
        _world.AddSystem(highPrioritySystem);
        _world.AddSystem(normalPrioritySystem);

        // Act
        _world.UpdateSystems(0.016f);

        // Assert
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("High", executionOrder[0]);
        Assert.Equal("Normal", executionOrder[1]);
        Assert.Equal("Low", executionOrder[2]);
    }

    [Fact]
    public void Adding_Same_System_Multiple_Times_Should_Only_Add_Once()
    {
        // Arrange
        var executionOrder = new List<string>();
        var system = new TestSystem("Test", UpdatePriority.Normal, executionOrder);

        // Act
        _world.AddSystem(system);
        _world.AddSystem(system); // Add same system again

        // Act
        _world.UpdateSystems(0.016f);

        // Assert - System should only execute once
        Assert.Single(executionOrder);
        Assert.Equal("Test", executionOrder[0]);
    }

    [Fact]
    public void Adding_Null_System_Should_Not_Throw()
    {
        // Act & Assert - Should not throw
        _world.AddSystem(null!);
        _world.UpdateSystems(0.016f);
    }

    [Fact]
    public void Removing_Null_System_Should_Return_False()
    {
        // Act
        var result = _world.RemoveSystem(null!);

        // Assert
        Assert.False(result);
    }

    public async ValueTask DisposeAsync()
    {
        await _world.DisposeAsync();
    }

    // Helper test system for testing execution order
    private class TestSystem : ISystem
    {
        private readonly string _name;
        private readonly List<string> _executionOrder;

        public TestSystem(string name, UpdatePriority priority, List<string> executionOrder)
        {
            _name = name;
            Priority = priority;
            _executionOrder = executionOrder;
        }

        public UpdatePriority Priority { get; }
        public bool IsEnabled { get; set; } = true;

        public void Update(IECSWorld world, float deltaTime)
        {
            _executionOrder.Add(_name);
        }
    }
}