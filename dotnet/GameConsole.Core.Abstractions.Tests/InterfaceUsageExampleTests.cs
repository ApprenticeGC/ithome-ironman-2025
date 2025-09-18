using GameConsole.Core.Abstractions;
using System.Text;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

/// <summary>
/// Example implementations and usage tests for the interfaces.
/// </summary>
public class InterfaceUsageExampleTests
{
    [Fact]
    public async Task IService_Implementation_Should_Support_Lifecycle_Operations()
    {
        // Arrange
        var service = new TestService();

        // Act & Assert - Initial state
        Assert.False(service.IsRunning);

        // Initialize
        await service.InitializeAsync();
        Assert.False(service.IsRunning);
        Assert.True(service.IsInitialized);

        // Start
        await service.StartAsync();
        Assert.True(service.IsRunning);

        // Stop
        await service.StopAsync();
        Assert.False(service.IsRunning);

        // Dispose
        await service.DisposeAsync();
        Assert.True(service.IsDisposed);
    }

    [Fact]
    public async Task ICapabilityProvider_Implementation_Should_Support_Capability_Discovery()
    {
        // Arrange
        var provider = new TestCapabilityProvider();

        // Act & Assert
        var capabilities = await provider.GetCapabilitiesAsync();
        Assert.Contains(typeof(string), capabilities);
        Assert.Contains(typeof(StringBuilder), capabilities);

        Assert.True(await provider.HasCapabilityAsync<string>());
        Assert.True(await provider.HasCapabilityAsync<StringBuilder>());
        Assert.False(await provider.HasCapabilityAsync<List<string>>());

        var stringCapability = await provider.GetCapabilityAsync<string>();
        Assert.Equal("Test String Capability", stringCapability);

        var stringBuilderCapability = await provider.GetCapabilityAsync<StringBuilder>();
        Assert.NotNull(stringBuilderCapability);
        Assert.Equal("Test StringBuilder", stringBuilderCapability.ToString());

        var nullCapability = await provider.GetCapabilityAsync<List<string>>();
        Assert.Null(nullCapability);
    }

    [Fact]
    public void IServiceMetadata_Implementation_Should_Provide_Service_Information()
    {
        // Arrange & Act
        var metadata = new TestServiceMetadata();

        // Assert
        Assert.Equal("Test Service", metadata.Name);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.Equal("A test service for demonstration", metadata.Description);
        Assert.Contains("Test", metadata.Categories);
        Assert.Contains("Demo", metadata.Categories);
        Assert.Equal("TestValue", metadata.Properties["TestKey"]);
    }

    [Fact]
    public void ServiceAttribute_Should_Be_Usable_On_Classes()
    {
        // Arrange
        var serviceType = typeof(AttributedTestService);

        // Act
        var attribute = serviceType.GetCustomAttributes(typeof(ServiceAttribute), false)
            .Cast<ServiceAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Attributed Test Service", attribute.Name);
        Assert.Equal("2.0.0", attribute.Version);
        Assert.Equal("Test service with attribute", attribute.Description);
        Assert.Contains("Test", attribute.Categories);
        Assert.Equal(ServiceLifetime.Singleton, attribute.Lifetime);
    }

    #region Test Implementations

    private class TestService : IService
    {
        public bool IsRunning { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            IsInitialized = true;
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
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class TestCapabilityProvider : ICapabilityProvider
    {
        private readonly Dictionary<Type, object> _capabilities = new()
        {
            { typeof(string), "Test String Capability" },
            { typeof(StringBuilder), new StringBuilder("Test StringBuilder") }
        };

        public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_capabilities.Keys.AsEnumerable());
        }

        public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_capabilities.ContainsKey(typeof(T)));
        }

        public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            _capabilities.TryGetValue(typeof(T), out var capability);
            return Task.FromResult(capability as T);
        }
    }

    private class TestServiceMetadata : IServiceMetadata
    {
        public string Name => "Test Service";
        public string Version => "1.0.0";
        public string Description => "A test service for demonstration";
        public IEnumerable<string> Categories => new[] { "Test", "Demo" };
        public IReadOnlyDictionary<string, object> Properties => new Dictionary<string, object>
        {
            { "TestKey", "TestValue" },
            { "NumberValue", 123 }
        };
    }

    [Service("Attributed Test Service", "2.0.0", "Test service with attribute",
        Categories = new[] { "Test", "Attributed" },
        Lifetime = ServiceLifetime.Singleton)]
    private class AttributedTestService : IService
    {
        public bool IsRunning => false;
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    #endregion
}
