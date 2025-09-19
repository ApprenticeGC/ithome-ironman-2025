using GameConsole.Core.Abstractions;
using GameConsole.Providers.Registry;
using System.Reflection;
using Xunit;

namespace GameConsole.Providers.Registry.Tests;

/// <summary>
/// Tests for the ProviderDiscovery functionality.
/// </summary>
public class ProviderDiscoveryTests
{
    private readonly ProviderDiscovery _discovery;

    public ProviderDiscoveryTests()
    {
        _discovery = new ProviderDiscovery();
    }

    [Fact]
    public async Task DiscoverProvidersAsync_WithValidAssembly_ShouldFindProviders()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };

        // Act
        var providers = await _discovery.DiscoverProvidersAsync<ITestService>(assemblies);

        // Assert
        Assert.NotEmpty(providers);
        var testProvider = providers.FirstOrDefault(p => p.Metadata.Name == "TestProvider");
        Assert.NotNull(testProvider);
        Assert.Equal("TestProvider", testProvider.Metadata.Name);
        Assert.Equal(new Version(1, 0, 0), testProvider.Metadata.Version);
        Assert.Equal(10, testProvider.Metadata.Priority);
        Assert.Contains("test", testProvider.Metadata.Capabilities);
    }

    [Fact]
    public async Task DiscoverProvidersFromAssemblyAsync_WithCurrentAssembly_ShouldFindTestProviders()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var providers = await _discovery.DiscoverProvidersFromAssemblyAsync<ITestService>(assembly);

        // Assert
        Assert.NotEmpty(providers);
        
        var testProvider = providers.FirstOrDefault(p => p.Metadata.Name == "TestProvider");
        Assert.NotNull(testProvider);
        Assert.Equal(typeof(TestServiceProvider), testProvider.ImplementationType);
    }

    [Fact]
    public async Task DiscoverProvidersAsync_WithEmptyAssemblies_ShouldReturnEmpty()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();

        // Act
        var providers = await _discovery.DiscoverProvidersAsync<ITestService>(assemblies);

        // Assert
        Assert.Empty(providers);
    }

    [Fact]
    public async Task DiscoverProvidersFromDirectoryAsync_WithNonExistentDirectory_ShouldReturnEmpty()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var providers = await _discovery.DiscoverProvidersFromDirectoryAsync<ITestService>(nonExistentPath);

        // Assert
        Assert.Empty(providers);
    }
}

// Additional test provider for discovery tests
[Provider(
    Name = "AdvancedTestProvider", 
    Version = "2.0.0", 
    Priority = 20,
    Capabilities = new[] { "advanced", "test", "caching" },
    SupportedPlatforms = Platform.Windows | Platform.Linux,
    Description = "Advanced test provider with caching capabilities",
    Author = "Test Team"
)]
public class AdvancedTestServiceProvider : ITestService
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
        return Task.FromResult("Advanced test data with caching");
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

// Provider without attribute (should not be discovered)
public class UnmarkedTestServiceProvider : ITestService
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
        return Task.FromResult("Unmarked test data");
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}