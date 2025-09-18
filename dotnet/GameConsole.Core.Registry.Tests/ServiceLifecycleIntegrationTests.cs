using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Integration tests validating service registry with IService lifecycle methods.
/// </summary>
public class ServiceLifecycleIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public ServiceLifecycleIntegrationTests()
    {
        _serviceProvider = new ServiceProvider();
    }

    [Fact]
    public async Task GetServiceAsync_Should_Initialize_And_Start_IService()
    {
        // Arrange
        _serviceProvider.RegisterTransient<ILifecycleTestService, LifecycleTestService>();

        // Act
        var service = await _serviceProvider.GetServiceAsync<ILifecycleTestService>();

        // Assert
        Assert.NotNull(service);
        Assert.True(service.IsInitialized);
        Assert.True(service.IsRunning);
        Assert.True(service.WasStarted);
    }

    [Fact]
    public async Task GetServiceAsync_Should_Not_Reinitialize_Running_Service()
    {
        // Arrange
        _serviceProvider.RegisterSingleton<ILifecycleTestService, LifecycleTestService>();

        // Act
        var service1 = await _serviceProvider.GetServiceAsync<ILifecycleTestService>();
        Assert.NotNull(service1);
        var initializeCount1 = service1.InitializeCount;
        var startCount1 = service1.StartCount;

        var service2 = await _serviceProvider.GetServiceAsync<ILifecycleTestService>();
        Assert.NotNull(service2);
        var initializeCount2 = service2.InitializeCount;
        var startCount2 = service2.StartCount;

        // Assert
        Assert.Same(service1, service2);
        Assert.Equal(initializeCount1, initializeCount2);
        Assert.Equal(startCount1, startCount2);
    }

    [Fact]
    public async Task Service_With_IService_Dependencies_Should_Work()
    {
        // Arrange
        _serviceProvider.RegisterSingleton<ILifecycleTestService, LifecycleTestService>();
        _serviceProvider.RegisterTransient<IServiceWithLifecycleDependency, ServiceWithLifecycleDependency>();

        // Act
        var service = await _serviceProvider.GetServiceAsync<IServiceWithLifecycleDependency>();

        // Assert
        Assert.NotNull(service);
        Assert.NotNull(service.Dependency);
        Assert.True(service.Dependency.IsRunning);
    }

    [Fact]
    public void ServiceProvider_Implements_IServiceProvider()
    {
        // Arrange & Act
        IServiceProvider provider = _serviceProvider;

        // Assert
        Assert.NotNull(provider);
        Assert.Same(_serviceProvider, provider);
    }

    [Fact]
    public void ServiceProvider_Implements_IServiceRegistry()
    {
        // Arrange & Act
        IServiceRegistry registry = _serviceProvider;

        // Assert
        Assert.NotNull(registry);
        Assert.Same(_serviceProvider, registry);
    }

    [Fact]
    public void ServiceProvider_Should_Register_Self_As_IServiceProvider()
    {
        // Act
        var provider = _serviceProvider.GetService<IServiceProvider>();

        // Assert
        Assert.Same(_serviceProvider, provider);
    }

    [Fact]
    public void ServiceProvider_Should_Register_Self_As_IServiceRegistry()
    {
        // Act
        var registry = _serviceProvider.GetService<IServiceRegistry>();

        // Assert
        Assert.Same(_serviceProvider, registry);
    }

    [Fact]
    public async Task Complex_Service_Graph_With_Mixed_Lifetimes_Should_Work()
    {
        // Arrange
        _serviceProvider.RegisterSingleton<ISingletonService, SingletonService>();
        _serviceProvider.RegisterScoped<IScopedService, ScopedService>();
        _serviceProvider.RegisterTransient<ITransientService, TransientService>();
        _serviceProvider.RegisterTransient<IComplexService, ComplexService>();

        // Act
        var complex1 = await _serviceProvider.GetServiceAsync<IComplexService>();
        var complex2 = await _serviceProvider.GetServiceAsync<IComplexService>();

        // Assert
        Assert.NotNull(complex1);
        Assert.NotNull(complex2);
        Assert.NotSame(complex1, complex2); // Transient

        // Singleton dependency should be the same
        Assert.Same(complex1.SingletonDep, complex2.SingletonDep);

        // All should be initialized and running
        Assert.True(complex1.SingletonDep.IsRunning);
        Assert.True(complex1.ScopedDep.IsRunning);
        Assert.True(complex1.TransientDep.IsRunning);
    }

    public void Dispose() => _serviceProvider?.Dispose();
}

// Test interfaces and implementations
public interface ILifecycleTestService : IService
{
    bool IsInitialized { get; }
    bool WasStarted { get; }
    int InitializeCount { get; }
    int StartCount { get; }
}

public interface IServiceWithLifecycleDependency
{
    ILifecycleTestService Dependency { get; }
}

public interface ISingletonService : IService { }
public interface IScopedService : IService { }
public interface ITransientService : IService { }
public interface IComplexService
{
    ISingletonService SingletonDep { get; }
    IScopedService ScopedDep { get; }
    ITransientService TransientDep { get; }
}

public class LifecycleTestService : ILifecycleTestService
{
    private bool _isInitialized;
    private bool _isRunning;
    private bool _wasStarted;
    private int _initializeCount;
    private int _startCount;

    public bool IsInitialized => _isInitialized;
    public bool IsRunning => _isRunning;
    public bool WasStarted => _wasStarted;
    public int InitializeCount => _initializeCount;
    public int StartCount => _startCount;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _isInitialized = true;
        _initializeCount++;
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        _wasStarted = true;
        _startCount++;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _isRunning = false;
        return ValueTask.CompletedTask;
    }
}

public class ServiceWithLifecycleDependency : IServiceWithLifecycleDependency
{
    public ServiceWithLifecycleDependency(ILifecycleTestService dependency)
    {
        Dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
    }

    public ILifecycleTestService Dependency { get; }
}

public class SingletonService : ISingletonService
{
    public bool IsRunning { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

public class ScopedService : IScopedService
{
    public bool IsRunning { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

public class TransientService : ITransientService
{
    public bool IsRunning { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

public class ComplexService : IComplexService
{
    public ComplexService(ISingletonService singletonDep, IScopedService scopedDep, ITransientService transientDep)
    {
        SingletonDep = singletonDep ?? throw new ArgumentNullException(nameof(singletonDep));
        ScopedDep = scopedDep ?? throw new ArgumentNullException(nameof(scopedDep));
        TransientDep = transientDep ?? throw new ArgumentNullException(nameof(transientDep));
    }

    public ISingletonService SingletonDep { get; }
    public IScopedService ScopedDep { get; }
    public ITransientService TransientDep { get; }
}