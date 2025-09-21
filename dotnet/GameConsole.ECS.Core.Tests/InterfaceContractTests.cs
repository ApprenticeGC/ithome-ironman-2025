using GameConsole.ECS.Core;
using Moq;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Tests for IEntity interface contracts.
/// </summary>
public class IEntityTests
{
    [Fact]
    public void Entity_Should_Have_Unique_Id()
    {
        // Arrange
        var entity1 = CreateMockEntity();
        var entity2 = CreateMockEntity();

        // Act & Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
        Assert.NotEqual(Guid.Empty, entity1.Id);
        Assert.NotEqual(Guid.Empty, entity2.Id);
    }

    [Fact]
    public void Entity_Should_Have_Generation()
    {
        // Arrange
        var entity = CreateMockEntity();

        // Act & Assert
        Assert.True(entity.Generation >= 0);
    }

    [Fact]
    public void Entity_Should_Have_IsAlive_Property()
    {
        // Arrange
        var entity = CreateMockEntity(isAlive: true);

        // Act & Assert
        Assert.True(entity.IsAlive);
    }

    [Fact]
    public void Entity_Should_Have_World_Reference()
    {
        // Arrange
        var world = new Mock<IECSWorld>();
        var entity = CreateMockEntity(world: world.Object);

        // Act & Assert
        Assert.NotNull(entity.World);
        Assert.Equal(world.Object, entity.World);
    }

    private static IEntity CreateMockEntity(bool isAlive = true, uint generation = 0, IECSWorld? world = null)
    {
        var mock = new Mock<IEntity>();
        mock.Setup(e => e.Id).Returns(Guid.NewGuid());
        mock.Setup(e => e.IsAlive).Returns(isAlive);
        mock.Setup(e => e.Generation).Returns(generation);
        mock.Setup(e => e.World).Returns(world ?? new Mock<IECSWorld>().Object);
        return mock.Object;
    }
}

/// <summary>
/// Tests for IComponent interface contracts.
/// </summary>
public class IComponentTests
{
    [Fact]
    public void Component_Should_Implement_IComponent()
    {
        // Arrange & Act
        var position = new PositionComponent();
        var velocity = new VelocityComponent();
        var health = new HealthComponent();

        // Assert
        Assert.IsAssignableFrom<IComponent>(position);
        Assert.IsAssignableFrom<IComponent>(velocity);
        Assert.IsAssignableFrom<IComponent>(health);
    }

    [Fact]
    public void Components_Should_Hold_Data()
    {
        // Arrange & Act
        var position = new PositionComponent(10, 20, 30);
        var velocity = new VelocityComponent(1, -2, 0.5f);
        var health = new HealthComponent(80, 100);
        var name = new NameComponent("TestEntity");

        // Assert
        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
        Assert.Equal(30, position.Z);

        Assert.Equal(1, velocity.X);
        Assert.Equal(-2, velocity.Y);
        Assert.Equal(0.5f, velocity.Z);

        Assert.Equal(80, health.Current);
        Assert.Equal(100, health.Maximum);
        Assert.True(health.IsAlive);
        Assert.False(health.IsFull);

        Assert.Equal("TestEntity", name.Name);
    }

    [Fact]
    public void Components_Should_Have_Value_Semantics()
    {
        // Arrange
        var pos1 = new PositionComponent(5, 10);
        var pos2 = new PositionComponent(5, 10);
        var pos3 = new PositionComponent(5, 11);

        // Act & Assert
        Assert.Equal(pos1, pos2);
        Assert.NotEqual(pos1, pos3);
        Assert.Equal(pos1.GetHashCode(), pos2.GetHashCode());
    }
}