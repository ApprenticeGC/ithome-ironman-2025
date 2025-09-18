using System.Diagnostics;
using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Performance tests to validate service resolution meets requirements.
/// </summary>
public class PerformanceTests
{
    [Fact]
    public void Singleton_Service_Resolution_Should_Be_Fast()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterSingleton<IPerformanceTestService, PerformanceTestService>();
        
        // Warm up - first resolution
        _ = serviceProvider.GetService<IPerformanceTestService>();

        // Act - Measure cached resolution time
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = serviceProvider.GetService<IPerformanceTestService>();
        }
        stopwatch.Stop();

        // Assert - Should be well under 1ms per resolution for cached services
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 1000;
        Assert.True(averageTime < 1.0, $"Average resolution time {averageTime:F4}ms exceeds 1ms requirement");
    }

    [Fact]
    public void Transient_Service_Resolution_Should_Be_Reasonable()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterTransient<IPerformanceTestService, PerformanceTestService>();

        // Act - Measure transient resolution time
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _ = serviceProvider.GetService<IPerformanceTestService>();
        }
        stopwatch.Stop();

        // Assert - Should complete reasonably fast even for transient services
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 100;
        Assert.True(averageTime < 10.0, $"Average transient resolution time {averageTime:F4}ms is too slow");
    }

    [Fact]
    public void Complex_Dependency_Resolution_Should_Be_Efficient()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterSingleton<IPerformanceTestService, PerformanceTestService>();
        serviceProvider.RegisterSingleton<IPerformanceTestService2, PerformanceTestService2>();
        serviceProvider.RegisterTransient<IComplexPerformanceService, ComplexPerformanceService>();

        // Warm up
        _ = serviceProvider.GetService<IComplexPerformanceService>();

        // Act - Measure complex resolution
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 500; i++)
        {
            _ = serviceProvider.GetService<IComplexPerformanceService>();
        }
        stopwatch.Stop();

        // Assert
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 500;
        Assert.True(averageTime < 5.0, $"Average complex resolution time {averageTime:F4}ms is too slow");
    }

    [Fact]
    public void Service_Registration_Should_Be_Fast()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            serviceProvider.RegisterTransient<IPerformanceTestService, PerformanceTestService>();
        }
        stopwatch.Stop();

        // Assert
        var averageTime = stopwatch.Elapsed.TotalMilliseconds / 1000;
        Assert.True(averageTime < 1.0, $"Average registration time {averageTime:F4}ms is too slow");
    }

    [Fact]
    public void Memory_Usage_Should_Remain_Stable_During_Resolution_Cycles()
    {
        // Arrange
        var serviceProvider = new ServiceProvider();
        serviceProvider.RegisterSingleton<IPerformanceTestService, PerformanceTestService>();

        // Warm up
        _ = serviceProvider.GetService<IPerformanceTestService>();

        // Act - Multiple resolution cycles
        var initialMemory = GC.GetTotalMemory(true);
        
        for (int cycle = 0; cycle < 10; cycle++)
        {
            for (int i = 0; i < 1000; i++)
            {
                _ = serviceProvider.GetService<IPerformanceTestService>();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);

        // Assert - Memory shouldn't grow significantly
        var memoryGrowth = finalMemory - initialMemory;
        Assert.True(memoryGrowth < 1024 * 1024, $"Memory grew by {memoryGrowth} bytes, which is too much"); // Less than 1MB growth
    }
}

// Test interfaces and implementations for performance testing
public interface IPerformanceTestService { }
public interface IPerformanceTestService2 { }
public interface IComplexPerformanceService { }

public class PerformanceTestService : IPerformanceTestService { }

public class PerformanceTestService2 : IPerformanceTestService2 { }

public class ComplexPerformanceService : IComplexPerformanceService
{
    public ComplexPerformanceService(IPerformanceTestService service1, IPerformanceTestService2 service2)
    {
        Service1 = service1;
        Service2 = service2;
    }

    public IPerformanceTestService Service1 { get; }
    public IPerformanceTestService2 Service2 { get; }
}