using GameConsole.ECS.Core;
using Moq;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Tests for IECSWorld interface contracts and behavior.
/// </summary>
public class IECSWorldTests
{
    [Fact]
    public void World_Should_Inherit_From_IService()
    {
        // Arrange
        var world = CreateMockWorld();

        // Act & Assert
        Assert.IsAssignableFrom<GameConsole.Core.Abstractions.IService>(world);
    }

    [Fact]
    public void World_Should_Have_Unique_Id()
    {
        // Arrange
        var world1 = CreateMockWorld();
        var world2 = CreateMockWorld();

        // Act & Assert
        Assert.NotEqual(world1.WorldId, world2.WorldId);
        Assert.NotEqual(Guid.Empty, world1.WorldId);
    }

    [Fact]
    public void World_Should_Have_Name()
    {
        // Arrange
        var world = CreateMockWorld(name: "TestWorld");

        // Act & Assert
        Assert.Equal("TestWorld", world.Name);
    }

    [Fact]
    public async Task World_Should_Create_Entities()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        Mock.Get(world)
            .Setup(w => w.CreateEntityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await world.CreateEntityAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity, result);
    }

    [Fact]
    public async Task World_Should_Destroy_Entities()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        Mock.Get(world)
            .Setup(w => w.DestroyEntityAsync(entity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await world.DestroyEntityAsync(entity);

        // Verify
        Mock.Get(world).Verify(w => w.DestroyEntityAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task World_Should_Check_Entity_Alive_Status()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        Mock.Get(world)
            .Setup(w => w.IsEntityAliveAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var isAlive = await world.IsEntityAliveAsync(entity);

        // Assert
        Assert.True(isAlive);
    }

    [Fact]
    public async Task World_Should_Return_Entity_Count()
    {
        // Arrange
        var world = CreateMockWorld();
        Mock.Get(world)
            .Setup(w => w.GetEntityCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var count = await world.GetEntityCountAsync();

        // Assert
        Assert.Equal(42, count);
    }

    [Fact]
    public async Task World_Should_Add_Components()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        var component = new PositionComponent(10, 20);
        Mock.Get(world)
            .Setup(w => w.AddComponentAsync(entity, component, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await world.AddComponentAsync(entity, component);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task World_Should_Remove_Components()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        Mock.Get(world)
            .Setup(w => w.RemoveComponentAsync<PositionComponent>(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await world.RemoveComponentAsync<PositionComponent>(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task World_Should_Get_Components()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        var component = new PositionComponent(10, 20);
        Mock.Get(world)
            .Setup(w => w.GetComponentAsync<PositionComponent>(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(component);

        // Act
        var result = await world.GetComponentAsync<PositionComponent>(entity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(component, result);
    }

    [Fact]
    public async Task World_Should_Check_Component_Existence()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        Mock.Get(world)
            .Setup(w => w.HasComponentAsync<PositionComponent>(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var hasComponent = await world.HasComponentAsync<PositionComponent>(entity);

        // Assert
        Assert.True(hasComponent);
    }

    [Fact]
    public async Task World_Should_Get_Entity_Component_Types()
    {
        // Arrange
        var world = CreateMockWorld();
        var entity = CreateMockEntity();
        var componentTypes = new HashSet<Type> { typeof(PositionComponent), typeof(VelocityComponent) };
        Mock.Get(world)
            .Setup(w => w.GetEntityComponentTypesAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(componentTypes);

        // Act
        var result = await world.GetEntityComponentTypesAsync(entity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(typeof(PositionComponent), result);
        Assert.Contains(typeof(VelocityComponent), result);
    }

    [Fact]
    public async Task World_Should_Create_Queries()
    {
        // Arrange
        var world = CreateMockWorld();
        var query = CreateMockQuery();
        Mock.Get(world)
            .Setup(w => w.CreateQueryAsync<PositionComponent>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(query);

        // Act
        var result = await world.CreateQueryAsync<PositionComponent>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(query, result);
    }

    [Fact]
    public async Task World_Should_Create_Multi_Component_Queries()
    {
        // Arrange
        var world = CreateMockWorld();
        var query = CreateMockQuery();
        Mock.Get(world)
            .Setup(w => w.CreateQueryAsync<PositionComponent, VelocityComponent>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(query);

        // Act
        var result = await world.CreateQueryAsync<PositionComponent, VelocityComponent>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(query, result);
    }

    [Fact]
    public async Task World_Should_Add_Systems()
    {
        // Arrange
        var world = CreateMockWorld();
        var system = CreateMockSystem();
        Mock.Get(world)
            .Setup(w => w.AddSystemAsync(system, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await world.AddSystemAsync(system);

        // Verify
        Mock.Get(world).Verify(w => w.AddSystemAsync(system, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task World_Should_Remove_Systems()
    {
        // Arrange
        var world = CreateMockWorld();
        var system = CreateMockSystem();
        Mock.Get(world)
            .Setup(w => w.RemoveSystemAsync(system, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await world.RemoveSystemAsync(system);

        // Verify
        Mock.Get(world).Verify(w => w.RemoveSystemAsync(system, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task World_Should_Get_Systems_In_Order()
    {
        // Arrange
        var world = CreateMockWorld();
        var systems = new List<ISystem> { CreateMockSystem(priority: 1), CreateMockSystem(priority: 2) };
        Mock.Get(world)
            .Setup(w => w.GetSystemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(systems);

        // Act
        var result = await world.GetSystemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(systems[0], result[0]);
        Assert.Equal(systems[1], result[1]);
    }

    [Fact]
    public async Task World_Should_Update_Systems()
    {
        // Arrange
        var world = CreateMockWorld();
        var deltaTime = 0.016f;
        Mock.Get(world)
            .Setup(w => w.UpdateAsync(deltaTime, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert (should not throw)
        await world.UpdateAsync(deltaTime);

        // Verify
        Mock.Get(world).Verify(w => w.UpdateAsync(deltaTime, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IECSWorld CreateMockWorld(string name = "TestWorld")
    {
        var mock = new Mock<IECSWorld>();
        mock.Setup(w => w.WorldId).Returns(Guid.NewGuid());
        mock.Setup(w => w.Name).Returns(name);
        mock.Setup(w => w.IsRunning).Returns(false);

        // Setup service methods
        mock.Setup(w => w.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(w => w.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(w => w.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(w => w.DisposeAsync()).Returns(ValueTask.CompletedTask);

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

    private static IEntityQuery CreateMockQuery()
    {
        var mock = new Mock<IEntityQuery>();
        mock.Setup(q => q.RequiredComponents).Returns(new HashSet<Type>());
        mock.Setup(q => q.ExcludedComponents).Returns(new HashSet<Type>());
        mock.Setup(q => q.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return mock.Object;
    }

    private static ISystem CreateMockSystem(int priority = 0)
    {
        var mock = new Mock<ISystem>();
        mock.Setup(s => s.Priority).Returns(priority);
        mock.Setup(s => s.CanExecuteInParallel).Returns(false);
        mock.Setup(s => s.ComponentTypes).Returns(new HashSet<Type>());
        mock.Setup(s => s.IsRunning).Returns(false);

        // Setup service methods
        mock.Setup(s => s.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
        mock.Setup(s => s.UpdateAsync(It.IsAny<IECSWorld>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock.Object;
    }
}