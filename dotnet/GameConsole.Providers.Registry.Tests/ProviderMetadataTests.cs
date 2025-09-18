using GameConsole.Providers.Registry;

namespace GameConsole.Providers.Registry.Tests;

public class ProviderMetadataTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var name = "TestProvider";
        var priority = 10;
        var capabilities = new HashSet<string> { "Capability1", "Capability2" };
        var platforms = Platform.Windows | Platform.Linux;
        var version = new Version(1, 2, 3);

        // Act
        var metadata = new ProviderMetadata(name, priority, capabilities, platforms, version);

        // Assert
        Assert.Equal(name, metadata.Name);
        Assert.Equal(priority, metadata.Priority);
        Assert.Equal(capabilities, metadata.Capabilities);
        Assert.Equal(platforms, metadata.SupportedPlatforms);
        Assert.Equal(version, metadata.Version);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var capabilities = new HashSet<string> { "Capability1" };
        var metadata1 = new ProviderMetadata("Test", 5, capabilities, Platform.All, new Version(1, 0, 0));
        var metadata2 = new ProviderMetadata("Test", 5, capabilities, Platform.All, new Version(1, 0, 0));

        // Act & Assert
        Assert.Equal(metadata1, metadata2);
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var capabilities1 = new HashSet<string> { "Capability1" };
        var capabilities2 = new HashSet<string> { "Capability2" };
        var metadata1 = new ProviderMetadata("Test1", 5, capabilities1, Platform.All, new Version(1, 0, 0));
        var metadata2 = new ProviderMetadata("Test2", 5, capabilities2, Platform.All, new Version(1, 0, 0));

        // Act & Assert
        Assert.NotEqual(metadata1, metadata2);
    }
}