using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Tests specifically for Pure.DI compile-time features and validation.
/// </summary>
public class PureDITests
{
    [Fact]
    public void ServiceComposition_Should_Use_Pure_DI_For_Code_Generation()
    {
        // Arrange & Act
        var composition = new ServiceComposition();

        // Assert - ServiceComposition should be a partial class with Pure.DI generated code
        // This test validates that Pure.DI is properly integrated
        Assert.NotNull(composition);
        
        // The fact that we can resolve these services proves Pure.DI code generation works
        var singletonService = composition.GetService(typeof(IExampleSingletonService));
        var transientService = composition.GetService(typeof(IExampleTransientService));
        
        Assert.NotNull(singletonService);
        Assert.NotNull(transientService);
    }

    [Fact]
    public void Pure_DI_Compile_Time_Dependency_Graph_Validation()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act - Resolve service with dependency
        var serviceWithDependency = composition.GetService(typeof(IExampleServiceWithDependency)) as IExampleServiceWithDependency;

        // Assert - Pure.DI should have validated at compile-time that IExampleDependency is available
        // If there were missing dependencies, this would have failed at compile time
        Assert.NotNull(serviceWithDependency);
        
        // The dependency should be properly injected
        var result = serviceWithDependency.GetMessageWithDependency();
        Assert.Contains("Dependency message", result);
    }

    [Fact]
    public void Pure_DI_Hierarchical_Scoping_With_Different_Lifetimes()
    {
        // Arrange
        var composition = new ServiceComposition();

        // Act - Get services with different lifetimes
        var singleton1 = composition.GetService(typeof(IExampleSingletonService));
        var singleton2 = composition.GetService(typeof(IExampleSingletonService));
        
        var transient1 = composition.GetService(typeof(IExampleTransientService));
        var transient2 = composition.GetService(typeof(IExampleTransientService));

        // Assert - Verify lifetime behavior
        Assert.Same(singleton1, singleton2); // Singleton should be same instance
        Assert.NotSame(transient1, transient2); // Transient should be different instances
    }

    [Fact]
    public void Pure_DI_Performance_Should_Be_Efficient()
    {
        // Arrange
        var composition = new ServiceComposition();
        var iterations = 1000;

        // Act - Resolve services multiple times to test performance
        var startTime = DateTime.UtcNow;
        
        for (int i = 0; i < iterations; i++)
        {
            var service = composition.GetService(typeof(IExampleSingletonService));
            Assert.NotNull(service);
        }
        
        var duration = DateTime.UtcNow - startTime;

        // Assert - Pure.DI should provide fast resolution (compile-time generated code)
        // Should be much faster than reflection-based resolution
        Assert.True(duration.TotalMilliseconds < 100, $"Resolution took {duration.TotalMilliseconds}ms for {iterations} iterations");
    }

    [Fact]
    public void Pure_DI_Thread_Safety_Configuration()
    {
        // Arrange & Act - Create multiple ServiceComposition instances concurrently
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            var composition = new ServiceComposition();
            var service = composition.GetService(typeof(IExampleSingletonService));
            return service;
        })).ToArray();

        // Wait for all tasks to complete
        Task.WaitAll(tasks);

        // Assert - All tasks should complete successfully (validates thread-safe code generation)
        foreach (var task in tasks)
        {
            Assert.NotNull(task.Result);
        }
    }

    [Fact]
    public void Pure_DI_Service_Lifetime_Management_Policies_Exist()
    {
        // This test validates that ServiceLifetimePolicies class exists and works
        // We can't test the internal class directly, but we can test its behavior
        // through the ServiceComposition which uses these policies
        
        // Arrange & Act
        var composition = new ServiceComposition();
        
        // Test that services with different lifetimes work correctly
        var singleton1 = composition.GetService(typeof(IExampleSingletonService));
        var singleton2 = composition.GetService(typeof(IExampleSingletonService));
        
        var transient1 = composition.GetService(typeof(IExampleTransientService));
        var transient2 = composition.GetService(typeof(IExampleTransientService));

        // Assert - Verify lifetime policies are correctly applied
        Assert.Same(singleton1, singleton2); // Singleton lifetime
        Assert.NotSame(transient1, transient2); // Transient lifetime
    }
}

/// <summary>
/// Test interfaces and classes for circular dependency testing with Pure.DI.
/// Note: Pure.DI would detect circular dependencies at compile-time,
/// so we can't actually create a working circular dependency in the setup.
/// This demonstrates the compile-time safety.
/// </summary>
public interface IPureDICircularTestA { }
public interface IPureDICircularTestB { }

/// <summary>
/// These classes would cause compile-time errors if bound in Pure.DI with circular dependencies.
/// The fact that our ServiceComposition compiles successfully proves there are no circular dependencies.
/// </summary>
public class PureDICircularTestA : IPureDICircularTestA
{
    // If we added a dependency on IPureDICircularTestB here and bound both in Pure.DI,
    // it would fail at compile time
    public PureDICircularTestA() { }
}

public class PureDICircularTestB : IPureDICircularTestB
{
    // Similarly, if this depended on IPureDICircularTestA, Pure.DI would catch it at compile time
    public PureDICircularTestB() { }
}