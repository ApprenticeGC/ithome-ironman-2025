using System.Reflection;
using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for attribute-based service registration.
/// </summary>
public class AttributeRegistrationTests
{
    [Fact]
    public void RegisterFromAttributes_Should_Register_Services_With_Attributes()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceProvider.RegisterFromAttributes(assembly);

        // Assert
        Assert.True(serviceProvider.IsRegistered<IAttributeTestService>());
        Assert.True(serviceProvider.IsRegistered<IAttributeTestService2>());
        
        var service = serviceProvider.GetService<IAttributeTestService>();
        Assert.NotNull(service);
        Assert.IsType<AttributeTestService>(service);
    }

    [Fact]
    public void RegisterFromAttributes_Should_Filter_By_Categories()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceProvider.RegisterFromAttributes(assembly, "test-category");

        // Assert
        Assert.True(serviceProvider.IsRegistered<IAttributeTestService>());
        Assert.False(serviceProvider.IsRegistered<IAttributeTestService2>()); // different category
    }

    [Fact]
    public void RegisterFromAttributes_Should_Respect_Lifetime_From_Attribute()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        serviceProvider.RegisterFromAttributes(assembly);

        // Assert - SingletonAttributeService should be singleton
        var instance1 = serviceProvider.GetService<ISingletonAttributeService>();
        var instance2 = serviceProvider.GetService<ISingletonAttributeService>();
        Assert.Same(instance1, instance2);
        
        // Assert - TransientAttributeService should be transient
        var transient1 = serviceProvider.GetService<ITransientAttributeService>();
        var transient2 = serviceProvider.GetService<ITransientAttributeService>();
        Assert.NotSame(transient1, transient2);
    }
}

// Test interfaces
public interface IAttributeTestService { }
public interface IAttributeTestService2 { }
public interface ISingletonAttributeService { }
public interface ITransientAttributeService { }

// Test implementations with attributes
[Service("AttributeTestService", "1.0.0", "Test service for attribute registration", Categories = new[] { "test-category" })]
public class AttributeTestService : IAttributeTestService
{
}

[Service("AttributeTestService2", "1.0.0", "Second test service", Categories = new[] { "other-category" })]
public class AttributeTestService2 : IAttributeTestService2
{
}

[Service("SingletonAttributeService", "1.0.0", "Singleton test service", Lifetime = ServiceLifetime.Singleton)]
public class SingletonAttributeService : ISingletonAttributeService
{
}

[Service("TransientAttributeService", "1.0.0", "Transient test service", Lifetime = ServiceLifetime.Transient)]
public class TransientAttributeService : ITransientAttributeService
{
}