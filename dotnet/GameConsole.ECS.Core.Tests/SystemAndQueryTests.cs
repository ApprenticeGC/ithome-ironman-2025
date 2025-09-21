using GameConsole.ECS.Core;
using Moq;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

/// <summary>
/// Tests for ISystem interface contracts and behavior.
/// </summary>
public class ISystemTests
{
    [Fact]
    public void System_Should_Inherit_From_IService()
    {
        // Arrange
        var system = CreateMockSystem();

        // Act & Assert
        Assert.IsAssignableFrom<GameConsole.Core.Abstractions.IService>(system);
    }

    [Fact]
    public void System_Should_Have_Priority()
    {
        // Arrange
        var system = CreateMockSystem(priority: 100);

        // Act & Assert
        Assert.Equal(100, system.Priority);
    }

    [Fact]
    public void System_Should_Have_Parallel_Execution_Flag()
    {
        // Arrange
        var parallelSystem = CreateMockSystem(canExecuteInParallel: true);
        var sequentialSystem = CreateMockSystem(canExecuteInParallel: false);

        // Act & Assert
        Assert.True(parallelSystem.CanExecuteInParallel);
        Assert.False(sequentialSystem.CanExecuteInParallel);
    }

    [Fact]
    public void System_Should_Have_Component_Types()
    {
        // Arrange
        var componentTypes = new HashSet<Type> { typeof(PositionComponent), typeof(VelocityComponent) };
        var system = CreateMockSystem(componentTypes: componentTypes);

        // Act & Assert
        Assert.NotNull(system.ComponentTypes);
        Assert.Equal(2, system.ComponentTypes.Count);
        Assert.Contains(typeof(PositionComponent), system.ComponentTypes);
        Assert.Contains(typeof(VelocityComponent), system.ComponentTypes);
    }

    [Fact]
    public async Task System_Should_Support_Async_Update()
    {
        // Arrange
        var world = new Mock<IECSWorld>();
        var system = CreateMockSystem();
        var deltaTime = 0.016f;
        var cancellationToken = CancellationToken.None;

        // Act & Assert (should not throw)
        await system.UpdateAsync(world.Object, deltaTime, cancellationToken);

        // Verify update was called
        Mock.Get(system).Verify(s => s.UpdateAsync(world.Object, deltaTime, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task System_Should_Support_Cancellation()
    {
        // Arrange
        var world = new Mock<IECSWorld>();
        var system = CreateMockSystem();
        var deltaTime = 0.016f;
        var cancellationTokenSource = new CancellationTokenSource();

        // Setup system to throw OperationCanceledException
        Mock.Get(system)
            .Setup(s => s.UpdateAsync(It.IsAny<IECSWorld>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            system.UpdateAsync(world.Object, deltaTime, cancellationTokenSource.Token));
    }

    private static ISystem CreateMockSystem(
        int priority = 0,
        bool canExecuteInParallel = false,
        IReadOnlySet<Type>? componentTypes = null)
    {
        var mock = new Mock<ISystem>();
        mock.Setup(s => s.Priority).Returns(priority);
        mock.Setup(s => s.CanExecuteInParallel).Returns(canExecuteInParallel);
        mock.Setup(s => s.ComponentTypes).Returns(componentTypes ?? new HashSet<Type>());
        mock.Setup(s => s.IsRunning).Returns(false);

        // Setup service methods
        mock.Setup(s => s.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        // Setup update method
        mock.Setup(s => s.UpdateAsync(It.IsAny<IECSWorld>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock.Object;
    }
}

/// <summary>
/// Tests for IEntityQuery interface contracts and behavior.
/// </summary>
public class IEntityQueryTests
{
    [Fact]
    public void Query_Should_Have_Required_Components()
    {
        // Arrange
        var requiredComponents = new HashSet<Type> { typeof(PositionComponent), typeof(VelocityComponent) };
        var query = CreateMockQuery(requiredComponents: requiredComponents);

        // Act & Assert
        Assert.NotNull(query.RequiredComponents);
        Assert.Equal(2, query.RequiredComponents.Count);
        Assert.Contains(typeof(PositionComponent), query.RequiredComponents);
        Assert.Contains(typeof(VelocityComponent), query.RequiredComponents);
    }

    [Fact]
    public void Query_Should_Have_Excluded_Components()
    {
        // Arrange
        var excludedComponents = new HashSet<Type> { typeof(HealthComponent) };
        var query = CreateMockQuery(excludedComponents: excludedComponents);

        // Act & Assert
        Assert.NotNull(query.ExcludedComponents);
        Assert.Single(query.ExcludedComponents);
        Assert.Contains(typeof(HealthComponent), query.ExcludedComponents);
    }

    [Fact]
    public async Task Query_Should_Return_Matching_Entities()
    {
        // Arrange
        var entities = new List<IEntity> { CreateMockEntity(), CreateMockEntity() };
        var query = CreateMockQuery();
        Mock.Get(query)
            .Setup(q => q.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await query.GetEntitiesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(entities[0], result[0]);
        Assert.Equal(entities[1], result[1]);
    }

    [Fact]
    public async Task Query_Should_Return_Entity_Count()
    {
        // Arrange
        var query = CreateMockQuery();
        Mock.Get(query)
            .Setup(q => q.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var count = await query.GetCountAsync();

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task Query_Should_Return_First_Entity()
    {
        // Arrange
        var entity = CreateMockEntity();
        var query = CreateMockQuery();
        Mock.Get(query)
            .Setup(q => q.GetFirstAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await query.GetFirstAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity, result);
    }

    [Fact]
    public async Task Query_Should_Check_If_Has_Any()
    {
        // Arrange
        var query = CreateMockQuery();
        Mock.Get(query)
            .Setup(q => q.HasAnyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var hasAny = await query.HasAnyAsync();

        // Assert
        Assert.True(hasAny);
    }

    private static IEntityQuery CreateMockQuery(
        IReadOnlySet<Type>? requiredComponents = null,
        IReadOnlySet<Type>? excludedComponents = null)
    {
        var mock = new Mock<IEntityQuery>();
        mock.Setup(q => q.RequiredComponents).Returns(requiredComponents ?? new HashSet<Type>());
        mock.Setup(q => q.ExcludedComponents).Returns(excludedComponents ?? new HashSet<Type>());
        mock.Setup(q => q.DisposeAsync()).Returns(ValueTask.CompletedTask);
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
}