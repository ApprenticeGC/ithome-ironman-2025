using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Integration tests for Pure.DI ServiceComposition container functionality.
/// </summary>
public class ServiceCompositionTests
{
    [Fact]
    public void ServiceComposition_CanBeCreated()
    {
        // Arrange & Act
        var composition = new ServiceComposition();

        // Assert
        Assert.NotNull(composition);
        Assert.Null(composition.Parent);
    }

    [Fact]
    public void ServiceComposition_ImplementsIServiceProvider()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act
        var serviceProvider = composition as IServiceProvider;

        // Assert
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void GetService_WithIServiceProvider_ReturnsItself()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act
        var result = composition.GetService(typeof(IServiceProvider));

        // Assert
        Assert.NotNull(result);
        Assert.Same(composition, result);
    }

    [Fact]
    public void GetService_WithUnregisteredService_ReturnsNull()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act
        var result = composition.GetService(typeof(ILogger<string>));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRequiredService_WithIServiceProvider_ReturnsItself()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act
        var result = composition.GetRequiredService(typeof(IServiceProvider));

        // Assert
        Assert.NotNull(result);
        Assert.Same(composition, result);
    }

    [Fact]
    public void GetRequiredService_WithUnregisteredService_ThrowsInvalidOperationException()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            composition.GetRequiredService(typeof(ILogger<string>)));
        
        Assert.Contains("ILogger", exception.Message);
    }

    [Fact]
    public void SetParent_StoresParentServiceProvider()
    {
        // Arrange
        var composition = new ServiceComposition();
        var parentProvider = new ServiceProvider();

        // Act
        composition.SetParent(parentProvider);

        // Assert
        Assert.Same(parentProvider, composition.Parent);
    }

    [Fact]
    public void GetService_WithParentSet_FallsBackToParent()
    {
        // Arrange
        var composition = new ServiceComposition();
        var parentProvider = new ServiceProvider();
        parentProvider.RegisterSingleton<ILogger<string>, TestLogger>();
        composition.SetParent(parentProvider);

        // Act
        var result = composition.GetService(typeof(ILogger<string>));

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestLogger>(result);
    }

    [Fact]
    public void CreateScope_ReturnsServiceScope()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act
        var scope = composition.CreateScope();

        // Assert
        Assert.NotNull(scope);
        Assert.IsAssignableFrom<IServiceScope>(scope);
        Assert.NotNull(scope.ServiceProvider);
        Assert.IsAssignableFrom<IServiceProvider>(scope.ServiceProvider);
    }

    [Fact]
    public void HierarchicalScoping_WorksCorrectly()
    {
        // Arrange
        var rootComposition = new ServiceComposition();
        var parentProvider = new ServiceProvider();
        
        // Setup hierarchy: root -> parent
        parentProvider.RegisterSingleton<ILogger<string>, TestLogger>();
        rootComposition.SetParent(parentProvider);

        // Act
        var result = rootComposition.GetService(typeof(ILogger<string>));

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestLogger>(result);
    }

    /// <summary>
    /// Test logger implementation for testing purposes.
    /// </summary>
    private class TestLogger : ILogger<string>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}