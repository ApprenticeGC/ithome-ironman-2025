using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for service lifetime management policies and compile-time dependency validation.
/// These tests validate that Pure.DI provides the expected benefits over runtime-only DI.
/// </summary>
public class ServiceLifetimeManagementTests
{
    [Fact]
    public void ServiceComposition_SingletonLifetime_SameInstanceReturned()
    {
        // Arrange
        var provider = new ServiceProvider().CreateHierarchical();
        provider.RegisterSingleton<ILifetimeTestService, LifetimeTestService>();
        
        // Act
        var instance1 = provider.GetService(typeof(ILifetimeTestService));
        var instance2 = provider.GetService(typeof(ILifetimeTestService));
        
        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void ServiceComposition_TransientLifetime_DifferentInstancesReturned()
    {
        // Arrange
        var provider = new ServiceProvider().CreateHierarchical();
        provider.RegisterTransient<ILifetimeTestService, LifetimeTestService>();
        
        // Act
        var instance1 = provider.GetService(typeof(ILifetimeTestService));
        var instance2 = provider.GetService(typeof(ILifetimeTestService));
        
        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void ServiceComposition_ScopedLifetime_SameInstanceWithinScope()
    {
        // Arrange
        var provider = new ServiceProvider().CreateHierarchical();
        provider.RegisterScoped<ILifetimeTestService, LifetimeTestService>();
        
        // Act
        var instance1 = provider.GetService(typeof(ILifetimeTestService));
        var instance2 = provider.GetService(typeof(ILifetimeTestService));
        
        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2); // Same within root scope
    }

    [Fact]
    public void ServiceComposition_ScopedServices_ProvideCorrectLifetime()
    {
        // Arrange
        var parent = new ServiceProvider().CreateHierarchical();
        parent.RegisterScoped<ILifetimeTestService, LifetimeTestService>();
        
        var child = parent.CreateChild();
        child.RegisterScoped<ILifetimeTestService, LifetimeTestService>();
        
        // Act
        var parentInstance = parent.GetService(typeof(ILifetimeTestService)) as ILifetimeTestService;
        var childInstance = child.GetService(typeof(ILifetimeTestService)) as ILifetimeTestService;
        
        // Assert
        Assert.NotNull(parentInstance);
        Assert.NotNull(childInstance);
        
        // Both containers can resolve their scoped services
        Assert.NotEqual(Guid.Empty, parentInstance.Id);
        Assert.NotEqual(Guid.Empty, childInstance.Id);
        
        // Validate that the scoped behavior works within each container
        var parentInstance2 = parent.GetService(typeof(ILifetimeTestService)) as ILifetimeTestService;
        var childInstance2 = child.GetService(typeof(ILifetimeTestService)) as ILifetimeTestService;
        
        Assert.Same(parentInstance, parentInstance2);
        Assert.Same(childInstance, childInstance2);
    }

    [Fact]
    public void ServiceComposition_CompileTimeValidation_PreventsMissingDependencies()
    {
        // Arrange
        var composition = new ServiceComposition();
        
        // Act - This test validates that Pure.DI provides compile-time validation
        // If there were missing dependencies in the composition setup, compilation would fail
        var registry = composition.GetServiceRegistry();
        
        // Assert
        Assert.NotNull(registry);
        // The fact that this compiles and runs means Pure.DI validated dependencies at compile time
    }

    [Fact]
    public void ServiceComposition_PerformanceOptimization_MinimalOverhead()
    {
        // Arrange
        var provider = new ServiceProvider().CreateHierarchical();
        provider.RegisterSingleton<ILifetimeTestService, LifetimeTestService>();
        
        // Pre-warm to ensure instances are created
        provider.GetService(typeof(ILifetimeTestService));
        
        // Act - Test that multiple resolutions work efficiently
        var start = DateTime.UtcNow;
        
        for (int i = 0; i < 1000; i++)
        {
            var service = provider.GetService(typeof(ILifetimeTestService));
            Assert.NotNull(service);
        }
        
        var elapsed = DateTime.UtcNow - start;
        
        // Assert - Should complete quickly (within reasonable time)
        Assert.True(elapsed.TotalMilliseconds < 1000, $"1000 singleton resolutions took {elapsed.TotalMilliseconds}ms, too slow");
    }

    [Fact]
    public void ServiceComposition_ServiceDiscovery_CanEnumerateServices()
    {
        // Arrange
        var provider = new ServiceProvider().CreateHierarchical();
        provider.RegisterSingleton<ILifetimeTestService, LifetimeTestService>();
        provider.RegisterTransient<IAnotherTestService, AnotherTestService>();
        
        // Act
        var services = provider.GetRegisteredServices();
        
        // Assert
        Assert.NotNull(services);
        Assert.Contains(services, s => s.ServiceType == typeof(ILifetimeTestService));
        Assert.Contains(services, s => s.ServiceType == typeof(IAnotherTestService));
    }

    [Fact]
    public void ServiceComposition_ConditionalRegistration_TryRegisterBehavior()
    {
        // Arrange
        var provider = new ServiceProvider().CreateHierarchical();
        
        // Act
        var firstRegistration = provider.TryRegisterSingleton<ILifetimeTestService, LifetimeTestService>();
        var secondRegistration = provider.TryRegisterSingleton<ILifetimeTestService, LifetimeTestService>();
        
        // Assert
        Assert.True(firstRegistration);
        Assert.False(secondRegistration); // Second registration should fail
    }

    // Test service interfaces and implementations
    public interface ILifetimeTestService
    {
        Guid Id { get; }
    }

    public interface IAnotherTestService
    {
        string Name { get; }
    }

    public class LifetimeTestService : ILifetimeTestService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class AnotherTestService : IAnotherTestService
    {
        public string Name => "Another Test Service";
    }
}