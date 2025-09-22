using GameConsole.ECS.Core;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Tests for basic ECS functionality including entity and component management.
/// </summary>
public class BasicECSTests : IAsyncDisposable
{
    private readonly ECSWorld _world;

    public BasicECSTests()
    {
        _world = new ECSWorld();
    }

    [Fact]
    public void Entity_Creation_Should_Generate_Unique_Ids()
    {
        // Act
        var entity1 = _world.CreateEntity();
        var entity2 = _world.CreateEntity();
        var entity3 = _world.CreateEntity();

        // Assert
        Assert.True(entity1.IsValid);
        Assert.True(entity2.IsValid);
        Assert.True(entity3.IsValid);
        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.NotEqual(entity2.Id, entity3.Id);
        Assert.NotEqual(entity1.Id, entity3.Id);
    }

    [Fact]
    public void World_Should_Track_Entity_Count()
    {
        // Arrange
        Assert.Equal(0, _world.EntityCount);

        // Act
        var entity1 = _world.CreateEntity();
        var entity2 = _world.CreateEntity();

        // Assert
        Assert.Equal(2, _world.EntityCount);

        // Act - Destroy one entity
        _world.DestroyEntity(entity1);

        // Assert
        Assert.Equal(1, _world.EntityCount);
    }

    [Fact]
    public void Component_Addition_Should_Work_Correctly()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var position = new PositionComponent(1.0f, 2.0f, 3.0f);

        // Act
        var result = _world.AddComponent(entity, position);

        // Assert
        Assert.True(result);
        Assert.True(_world.HasComponent<PositionComponent>(entity));

        var retrievedPosition = _world.GetComponent<PositionComponent>(entity);
        Assert.NotNull(retrievedPosition);
        Assert.Equal(1.0f, retrievedPosition.Value.X);
        Assert.Equal(2.0f, retrievedPosition.Value.Y);
        Assert.Equal(3.0f, retrievedPosition.Value.Z);
    }

    [Fact]
    public void Adding_Same_Component_Twice_Should_Fail()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var position1 = new PositionComponent(1.0f, 2.0f, 3.0f);
        var position2 = new PositionComponent(4.0f, 5.0f, 6.0f);

        // Act
        var result1 = _world.AddComponent(entity, position1);
        var result2 = _world.AddComponent(entity, position2);

        // Assert
        Assert.True(result1);
        Assert.False(result2);

        // Original component should remain unchanged
        var retrievedPosition = _world.GetComponent<PositionComponent>(entity);
        Assert.NotNull(retrievedPosition);
        Assert.Equal(1.0f, retrievedPosition.Value.X);
    }

    [Fact]
    public void Component_Removal_Should_Work_Correctly()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var position = new PositionComponent(1.0f, 2.0f, 3.0f);
        _world.AddComponent(entity, position);

        // Act
        var result = _world.RemoveComponent<PositionComponent>(entity);

        // Assert
        Assert.True(result);
        Assert.False(_world.HasComponent<PositionComponent>(entity));
        Assert.Null(_world.GetComponent<PositionComponent>(entity));
    }

    [Fact]
    public void Removing_Nonexistent_Component_Should_Return_False()
    {
        // Arrange
        var entity = _world.CreateEntity();

        // Act
        var result = _world.RemoveComponent<PositionComponent>(entity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Component_Update_Should_Work_Correctly()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var position = new PositionComponent(1.0f, 2.0f, 3.0f);
        _world.AddComponent(entity, position);

        // Act
        var newPosition = new PositionComponent(4.0f, 5.0f, 6.0f);
        var result = _world.UpdateComponent(entity, newPosition);

        // Assert
        Assert.True(result);
        var retrievedPosition = _world.GetComponent<PositionComponent>(entity);
        Assert.NotNull(retrievedPosition);
        Assert.Equal(4.0f, retrievedPosition.Value.X);
        Assert.Equal(5.0f, retrievedPosition.Value.Y);
        Assert.Equal(6.0f, retrievedPosition.Value.Z);
    }

    [Fact]
    public void Updating_Nonexistent_Component_Should_Return_False()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var position = new PositionComponent(1.0f, 2.0f, 3.0f);

        // Act
        var result = _world.UpdateComponent(entity, position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Entity_Destruction_Should_Remove_All_Components()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var position = new PositionComponent(1.0f, 2.0f, 3.0f);
        var velocity = new VelocityComponent(0.5f, 1.0f, 1.5f);
        var health = new HealthComponent(100, 100);

        _world.AddComponent(entity, position);
        _world.AddComponent(entity, velocity);
        _world.AddComponent(entity, health);

        // Act
        var result = _world.DestroyEntity(entity);

        // Assert
        Assert.True(result);
        Assert.False(_world.HasComponent<PositionComponent>(entity));
        Assert.False(_world.HasComponent<VelocityComponent>(entity));
        Assert.False(_world.HasComponent<HealthComponent>(entity));
        Assert.Equal(0, _world.EntityCount);
    }

    [Fact]
    public void Operations_On_Invalid_Entity_Should_Return_False()
    {
        // Arrange
        var entity = Entity.None;
        var position = new PositionComponent(1.0f, 2.0f, 3.0f);

        // Act & Assert
        Assert.False(_world.AddComponent(entity, position));
        Assert.False(_world.RemoveComponent<PositionComponent>(entity));
        Assert.False(_world.HasComponent<PositionComponent>(entity));
        Assert.Null(_world.GetComponent<PositionComponent>(entity));
        Assert.False(_world.UpdateComponent(entity, position));
        Assert.False(_world.DestroyEntity(entity));
    }

    [Fact]
    public void Operations_On_Null_Entity_Should_Return_False()
    {
        // Arrange
        IEntity? entity = null;
        var position = new PositionComponent(1.0f, 2.0f, 3.0f);

        // Act & Assert
        Assert.False(_world.AddComponent(entity!, position));
        Assert.False(_world.RemoveComponent<PositionComponent>(entity!));
        Assert.False(_world.HasComponent<PositionComponent>(entity!));
        Assert.Null(_world.GetComponent<PositionComponent>(entity!));
        Assert.False(_world.UpdateComponent(entity!, position));
        Assert.False(_world.DestroyEntity(entity!));
    }

    public async ValueTask DisposeAsync()
    {
        await _world.DisposeAsync();
    }
}