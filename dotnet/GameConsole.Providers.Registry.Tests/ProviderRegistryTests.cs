using GameConsole.Providers.Registry;

namespace GameConsole.Providers.Registry.Tests;

// Test interface for provider testing
public interface ITestService
{
    string GetName();
}

// Test implementations
public class HighPriorityTestService : ITestService
{
    public string GetName() => "HighPriority";
}

public class LowPriorityTestService : ITestService
{
    public string GetName() => "LowPriority";
}

public class WindowsOnlyTestService : ITestService
{
    public string GetName() => "WindowsOnly";
}

public class ProviderRegistryTests
{
    [Fact]
    public void RegisterProvider_WithValidProvider_RegistersSuccessfully()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        var provider = new HighPriorityTestService();
        var metadata = new ProviderMetadata(
            "HighPriority",
            10,
            new HashSet<string> { "TestCapability" },
            Platform.All,
            new Version(1, 0, 0));

        var eventRaised = false;
        registry.ProviderChanged += (sender, args) =>
        {
            eventRaised = true;
            Assert.Equal(ProviderChangeType.Registered, args.ChangeType);
            Assert.Equal(provider, args.Provider);
            Assert.Equal(metadata, args.Metadata);
        };

        // Act
        registry.RegisterProvider(provider, metadata);

        // Assert
        Assert.True(eventRaised);
        var registeredProvider = registry.GetProvider();
        Assert.Equal(provider, registeredProvider);
    }

    [Fact]
    public void RegisterProvider_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        var metadata = new ProviderMetadata(
            "Test",
            1,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.RegisterProvider(null!, metadata));
    }

    [Fact]
    public void UnregisterProvider_WithExistingProvider_ReturnsTrue()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        var provider = new HighPriorityTestService();
        var metadata = new ProviderMetadata(
            "HighPriority",
            10,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        registry.RegisterProvider(provider, metadata);

        var eventRaised = false;
        registry.ProviderChanged += (sender, args) =>
        {
            if (args.ChangeType == ProviderChangeType.Unregistered)
            {
                eventRaised = true;
                Assert.Equal(provider, args.Provider);
                Assert.Equal(metadata, args.Metadata);
            }
        };

        // Act
        var result = registry.UnregisterProvider("HighPriority");

        // Assert
        Assert.True(result);
        Assert.True(eventRaised);
        Assert.Null(registry.GetProvider());
    }

    [Fact]
    public void UnregisterProvider_WithNonExistentProvider_ReturnsFalse()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();

        // Act
        var result = registry.UnregisterProvider("NonExistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetProvider_WithNoCriteria_ReturnsHighestPriorityProvider()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        
        var lowPriorityProvider = new LowPriorityTestService();
        var lowPriorityMetadata = new ProviderMetadata(
            "LowPriority",
            1,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        var highPriorityProvider = new HighPriorityTestService();
        var highPriorityMetadata = new ProviderMetadata(
            "HighPriority",
            10,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        registry.RegisterProvider(lowPriorityProvider, lowPriorityMetadata);
        registry.RegisterProvider(highPriorityProvider, highPriorityMetadata);

        // Act
        var provider = registry.GetProvider();

        // Assert
        Assert.Equal(highPriorityProvider, provider);
    }

    [Fact]
    public void GetProviders_WithCapabilityCriteria_ReturnsMatchingProviders()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();

        var provider1 = new HighPriorityTestService();
        var metadata1 = new ProviderMetadata(
            "Provider1",
            10,
            new HashSet<string> { "Capability1", "Capability2" },
            Platform.All,
            new Version(1, 0, 0));

        var provider2 = new LowPriorityTestService();
        var metadata2 = new ProviderMetadata(
            "Provider2",
            5,
            new HashSet<string> { "Capability1" },
            Platform.All,
            new Version(1, 0, 0));

        var provider3 = new WindowsOnlyTestService();
        var metadata3 = new ProviderMetadata(
            "Provider3",
            8,
            new HashSet<string> { "DifferentCapability" },
            Platform.All,
            new Version(1, 0, 0));

        registry.RegisterProvider(provider1, metadata1);
        registry.RegisterProvider(provider2, metadata2);
        registry.RegisterProvider(provider3, metadata3);

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "Capability1" });

        // Act
        var providers = registry.GetProviders(criteria);

        // Assert
        Assert.Equal(2, providers.Count);
        Assert.Contains(provider1, providers);
        Assert.Contains(provider2, providers);
        Assert.DoesNotContain(provider3, providers);
        
        // Should be sorted by priority (highest first)
        Assert.Equal(provider1, providers[0]);
        Assert.Equal(provider2, providers[1]);
    }

    [Fact]
    public void SupportsCapabilities_WithSupportedCapabilities_ReturnsTrue()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        var provider = new HighPriorityTestService();
        var metadata = new ProviderMetadata(
            "Test",
            1,
            new HashSet<string> { "Capability1", "Capability2" },
            Platform.All,
            new Version(1, 0, 0));

        registry.RegisterProvider(provider, metadata);

        var requiredCapabilities = new HashSet<string> { "Capability1" };

        // Act
        var result = registry.SupportsCapabilities(requiredCapabilities);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SupportsCapabilities_WithUnsupportedCapabilities_ReturnsFalse()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        var provider = new HighPriorityTestService();
        var metadata = new ProviderMetadata(
            "Test",
            1,
            new HashSet<string> { "Capability1" },
            Platform.All,
            new Version(1, 0, 0));

        registry.RegisterProvider(provider, metadata);

        var requiredCapabilities = new HashSet<string> { "UnsupportedCapability" };

        // Act
        var result = registry.SupportsCapabilities(requiredCapabilities);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetProviderMetadata_WithExistingProvider_ReturnsMetadata()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        var provider = new HighPriorityTestService();
        var metadata = new ProviderMetadata(
            "Test",
            1,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        registry.RegisterProvider(provider, metadata);

        // Act
        var result = registry.GetProviderMetadata("Test");

        // Assert
        Assert.Equal(metadata, result);
    }

    [Fact]
    public void GetProviderMetadata_WithNonExistentProvider_ReturnsNull()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();

        // Act
        var result = registry.GetProviderMetadata("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllProviderMetadata_WithMultipleProviders_ReturnsAllMetadata()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();

        var provider1 = new HighPriorityTestService();
        var metadata1 = new ProviderMetadata(
            "Provider1",
            10,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        var provider2 = new LowPriorityTestService();
        var metadata2 = new ProviderMetadata(
            "Provider2",
            5,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        registry.RegisterProvider(provider1, metadata1);
        registry.RegisterProvider(provider2, metadata2);

        // Act
        var allMetadata = registry.GetAllProviderMetadata();

        // Assert
        Assert.Equal(2, allMetadata.Count);
        Assert.Contains(metadata1, allMetadata);
        Assert.Contains(metadata2, allMetadata);
    }

    [Fact]
    public void RegisterProvider_WithSameName_UpdatesExistingProvider()
    {
        // Arrange
        var registry = new ProviderRegistry<ITestService>();
        var provider1 = new HighPriorityTestService();
        var metadata1 = new ProviderMetadata(
            "TestProvider",
            5,
            new HashSet<string>(),
            Platform.All,
            new Version(1, 0, 0));

        var provider2 = new LowPriorityTestService();
        var metadata2 = new ProviderMetadata(
            "TestProvider",
            10,
            new HashSet<string> { "NewCapability" },
            Platform.Windows,
            new Version(2, 0, 0));

        var updateEventRaised = false;
        registry.RegisterProvider(provider1, metadata1);

        registry.ProviderChanged += (sender, args) =>
        {
            if (args.ChangeType == ProviderChangeType.Updated)
            {
                updateEventRaised = true;
                Assert.Equal(provider2, args.Provider);
                Assert.Equal(metadata2, args.Metadata);
            }
        };

        // Act
        registry.RegisterProvider(provider2, metadata2);

        // Assert
        Assert.True(updateEventRaised);
        var currentProvider = registry.GetProvider();
        Assert.Equal(provider2, currentProvider);
        
        var currentMetadata = registry.GetProviderMetadata("TestProvider");
        Assert.Equal(metadata2, currentMetadata);
    }
}