using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for the ServiceProvider class focusing on basic registration and resolution.
/// </summary>
public class ServiceProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public ServiceProviderTests()
    {
        _serviceProvider = new ServiceProvider();
    }

    [Fact]
    public void RegisterTransient_Should_Create_New_Instance_Each_Time()
    {
        // Arrange
        _serviceProvider.RegisterTransient<ITestService, TestService>();

        // Act
        var instance1 = _serviceProvider.GetService<ITestService>();
        var instance2 = _serviceProvider.GetService<ITestService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void RegisterSingleton_Should_Return_Same_Instance()
    {
        // Arrange
        _serviceProvider.RegisterSingleton<ITestService, TestService>();

        // Act
        var instance1 = _serviceProvider.GetService<ITestService>();
        var instance2 = _serviceProvider.GetService<ITestService>();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void RegisterSingleton_With_Instance_Should_Return_That_Instance()
    {
        // Arrange
        var instance = new TestService();
        _serviceProvider.RegisterSingleton<ITestService>(instance);

        // Act
        var result = _serviceProvider.GetService<ITestService>();

        // Assert
        Assert.Same(instance, result);
    }

    [Fact]
    public void RegisterSingleton_With_Factory_Should_Use_Factory()
    {
        // Arrange
        var expected = new TestService();
        _serviceProvider.RegisterSingleton<ITestService>(_ => expected);

        // Act
        var result = _serviceProvider.GetService<ITestService>();

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public void GetRequiredService_Should_Throw_When_Not_Registered()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _serviceProvider.GetRequiredService<ITestService>());
    }

    [Fact]
    public void GetService_Should_Return_Null_When_Not_Registered()
    {
        // Act
        var result = _serviceProvider.GetService<ITestService>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryRegister_Should_Return_False_When_Already_Registered()
    {
        // Arrange
        _serviceProvider.RegisterSingleton<ITestService, TestService>();
        var descriptor = ServiceDescriptor.Transient<ITestService, TestService>();

        // Act
        var result = _serviceProvider.TryRegister(descriptor);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryRegister_Should_Return_True_When_Not_Registered()
    {
        // Arrange
        var descriptor = ServiceDescriptor.Transient<ITestService, TestService>();

        // Act
        var result = _serviceProvider.TryRegister(descriptor);

        // Assert
        Assert.True(result);
        Assert.NotNull(_serviceProvider.GetService<ITestService>());
    }

    [Fact]
    public void IsRegistered_Should_Return_True_When_Service_Is_Registered()
    {
        // Arrange
        _serviceProvider.RegisterTransient<ITestService, TestService>();

        // Act
        var result = _serviceProvider.IsRegistered<ITestService>();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRegistered_Should_Return_False_When_Service_Not_Registered()
    {
        // Act
        var result = _serviceProvider.IsRegistered<ITestService>();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_Dependencies_Should_Be_Resolved()
    {
        // Arrange
        _serviceProvider.RegisterSingleton<ITestService, TestService>();
        _serviceProvider.RegisterTransient<IServiceWithDependency, ServiceWithDependency>();

        // Act
        var result = _serviceProvider.GetService<IServiceWithDependency>();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.TestService);
    }

    [Fact]
    public void GetRegisteredServices_Should_Return_All_Descriptors()
    {
        // Arrange
        _serviceProvider.RegisterTransient<ITestService, TestService>();
        _serviceProvider.RegisterSingleton<IServiceWithDependency, ServiceWithDependency>();

        // Act
        var services = _serviceProvider.GetRegisteredServices().ToList();

        // Assert
        Assert.Contains(services, s => s.ServiceType == typeof(ITestService));
        Assert.Contains(services, s => s.ServiceType == typeof(IServiceWithDependency));
    }

    [Fact]
    public void CreateScope_Should_Return_Valid_Scope()
    {
        // Act
        using var scope = _serviceProvider.CreateScope();

        // Assert
        Assert.NotNull(scope);
        Assert.NotNull(scope.ServiceProvider);
    }

    public void Dispose() => _serviceProvider?.Dispose();
}

// Test interfaces and implementations
public interface ITestService
{
    string Name { get; }
}

public interface IServiceWithDependency
{
    ITestService TestService { get; }
}

public class TestService : ITestService
{
    public string Name => "TestService";
}

public class ServiceWithDependency : IServiceWithDependency
{
    public ServiceWithDependency(ITestService testService)
    {
        TestService = testService ?? throw new ArgumentNullException(nameof(testService));
    }

    public ITestService TestService { get; }
}