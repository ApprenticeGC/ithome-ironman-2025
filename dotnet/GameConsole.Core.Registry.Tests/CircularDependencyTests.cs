using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests for circular dependency detection.
/// </summary>
public class CircularDependencyTests
{
    [Fact]
    public void Circular_Dependency_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterTransient<ICircularA, CircularA>();
        serviceProvider.RegisterTransient<ICircularB, CircularB>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<ICircularA>());
        Assert.Contains("Circular dependency detected", exception.Message);
    }

    [Fact]
    public void Self_Dependency_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterTransient<ISelfDependent, SelfDependent>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<ISelfDependent>());
        Assert.Contains("Circular dependency detected", exception.Message);
    }

    [Fact]
    public void Complex_Circular_Dependency_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterTransient<IComplexA, ComplexA>();
        serviceProvider.RegisterTransient<IComplexB, ComplexB>();
        serviceProvider.RegisterTransient<IComplexC, ComplexC>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<IComplexA>());
        Assert.Contains("Circular dependency detected", exception.Message);
    }
}

// Test interfaces and classes for circular dependency testing
public interface ICircularA { }
public interface ICircularB { }

public class CircularA : ICircularA
{
    public CircularA(ICircularB circularB) { }
}

public class CircularB : ICircularB
{
    public CircularB(ICircularA circularA) { }
}

public interface ISelfDependent { }

public class SelfDependent : ISelfDependent
{
    public SelfDependent(ISelfDependent selfDependent) { }
}

public interface IComplexA { }
public interface IComplexB { }
public interface IComplexC { }

public class ComplexA : IComplexA
{
    public ComplexA(IComplexB complexB) { }
}

public class ComplexB : IComplexB
{
    public ComplexB(IComplexC complexC) { }
}

public class ComplexC : IComplexC
{
    public ComplexC(IComplexA complexA) { }
}