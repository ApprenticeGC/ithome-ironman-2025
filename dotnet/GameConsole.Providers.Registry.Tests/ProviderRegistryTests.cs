using GameConsole.Core.Abstractions;
using GameConsole.Providers.Registry;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Providers.Registry.Tests;

/// <summary>
/// Tests for the ProviderRegistry implementation.
/// </summary>
public class ProviderRegistryTests : IAsyncDisposable
{
    private readonly ProviderRegistry<ITestService> _registry;

    public ProviderRegistryTests()
    {
        _registry = new ProviderRegistry<ITestService>();
    }

    [Fact]
    public async Task RegisterProviderAsync_WithValidProvider_ShouldSucceed()
    {
        // Arrange
        var provider = new TestServiceProvider();
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "test" },
            SupportedPlatforms: Platform.All
        );

        // Act
        await _registry.RegisterProviderAsync(provider, metadata);

        // Assert
        Assert.Equal(1, _registry.ProviderCount);
        var retrievedProvider = await _registry.GetProviderAsync();
        Assert.Same(provider, retrievedProvider);
    }

    [Fact]
    public async Task RegisterProviderAsync_WithDuplicateProvider_ShouldThrowException()
    {
        // Arrange
        var provider1 = new TestServiceProvider();
        var provider2 = new TestServiceProvider();
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "test" },
            SupportedPlatforms: Platform.All
        );

        await _registry.RegisterProviderAsync(provider1, metadata);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _registry.RegisterProviderAsync(provider2, metadata));
    }

    [Fact]
    public async Task GetProviderAsync_WithMatchingCriteria_ShouldReturnCorrectProvider()
    {
        // Arrange
        var provider1 = new TestServiceProvider();
        var metadata1 = new ProviderMetadata(
            Name: "Provider1",
            Version: new Version(1, 0, 0),
            Priority: 5,
            Capabilities: new HashSet<string> { "audio" },
            SupportedPlatforms: Platform.Windows
        );

        var provider2 = new TestServiceProvider();
        var metadata2 = new ProviderMetadata(
            Name: "Provider2",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio", "video" },
            SupportedPlatforms: Platform.All
        );

        await _registry.RegisterProviderAsync(provider1, metadata1);
        await _registry.RegisterProviderAsync(provider2, metadata2);

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "video" }
        );

        // Act
        var result = await _registry.GetProviderAsync(criteria);

        // Assert
        Assert.Same(provider2, result);
    }

    [Fact]
    public async Task GetProviderAsync_WithNoMatchingCriteria_ShouldReturnNull()
    {
        // Arrange
        var provider = new TestServiceProvider();
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio" },
            SupportedPlatforms: Platform.Windows
        );

        await _registry.RegisterProviderAsync(provider, metadata);

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "video" }
        );

        // Act
        var result = await _registry.GetProviderAsync(criteria);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProvidersAsync_ShouldReturnAllMatchingProviders()
    {
        // Arrange
        var provider1 = new TestServiceProvider();
        var metadata1 = new ProviderMetadata(
            Name: "Provider1",
            Version: new Version(1, 0, 0),
            Priority: 5,
            Capabilities: new HashSet<string> { "audio" },
            SupportedPlatforms: Platform.All
        );

        var provider2 = new TestServiceProvider();
        var metadata2 = new ProviderMetadata(
            Name: "Provider2",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio" },
            SupportedPlatforms: Platform.All
        );

        await _registry.RegisterProviderAsync(provider1, metadata1);
        await _registry.RegisterProviderAsync(provider2, metadata2);

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "audio" }
        );

        // Act
        var result = await _registry.GetProvidersAsync(criteria);

        // Assert
        Assert.Equal(2, result.Count);
        // Should be ordered by priority (higher first)
        Assert.Same(provider2, result[0]);
        Assert.Same(provider1, result[1]);
    }

    [Fact]
    public async Task UnregisterProviderAsync_WithExistingProvider_ShouldSucceed()
    {
        // Arrange
        var provider = new TestServiceProvider();
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "test" },
            SupportedPlatforms: Platform.All
        );

        await _registry.RegisterProviderAsync(provider, metadata);
        Assert.Equal(1, _registry.ProviderCount);

        // Act
        var result = await _registry.UnregisterProviderAsync("TestProvider");

        // Assert
        Assert.True(result);
        Assert.Equal(0, _registry.ProviderCount);
    }

    [Fact]
    public async Task SupportsCapabilitiesAsync_WithSupportedCapabilities_ShouldReturnTrue()
    {
        // Arrange
        var provider = new TestServiceProvider();
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio", "video", "input" },
            SupportedPlatforms: Platform.All
        );

        await _registry.RegisterProviderAsync(provider, metadata);

        var requiredCapabilities = new HashSet<string> { "audio", "video" };

        // Act
        var result = await _registry.SupportsCapabilitiesAsync(requiredCapabilities);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAvailableCapabilitiesAsync_ShouldReturnAllCapabilities()
    {
        // Arrange
        var provider1 = new TestServiceProvider();
        var metadata1 = new ProviderMetadata(
            Name: "Provider1",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio", "video" },
            SupportedPlatforms: Platform.All
        );

        var provider2 = new TestServiceProvider();
        var metadata2 = new ProviderMetadata(
            Name: "Provider2",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "input", "networking" },
            SupportedPlatforms: Platform.All
        );

        await _registry.RegisterProviderAsync(provider1, metadata1);
        await _registry.RegisterProviderAsync(provider2, metadata2);

        // Act
        var result = await _registry.GetAvailableCapabilitiesAsync();

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains("audio", result);
        Assert.Contains("video", result);
        Assert.Contains("input", result);
        Assert.Contains("networking", result);
    }

    [Fact]
    public async Task ProviderChanged_Event_ShouldBeRaisedOnRegistration()
    {
        // Arrange
        var provider = new TestServiceProvider();
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "test" },
            SupportedPlatforms: Platform.All
        );

        ProviderChangedEventArgs<ITestService>? eventArgs = null;
        _registry.ProviderChanged += (sender, args) => eventArgs = args;

        // Act
        await _registry.RegisterProviderAsync(provider, metadata);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ProviderChangeType.Registered, eventArgs.ChangeType);
        Assert.Same(provider, eventArgs.Provider);
        Assert.Equal("TestProvider", eventArgs.Metadata.Name);
    }

    public async ValueTask DisposeAsync()
    {
        await _registry.DisposeAsync();
    }
}

// Test service interface for testing
public interface ITestService : IService
{
    Task<string> GetTestDataAsync();
}

// Test provider implementation
[Provider(Name = "TestProvider", Version = "1.0.0", Priority = 10, Capabilities = new[] { "test" })]
public class TestServiceProvider : ITestService
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

    public Task<string> GetTestDataAsync()
    {
        return Task.FromResult("Test data");
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}