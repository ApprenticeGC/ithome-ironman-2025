using GameConsole.Profile.Core;
using Xunit;

namespace GameConsole.Profile.Core.Tests;

/// <summary>
/// Tests for the ServiceConfiguration class.
/// </summary>
public class ServiceConfigurationTests
{
    [Fact]
    public void Constructor_CreatesConfigurationWithDefaults()
    {
        // Act
        var config = new ServiceConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Implementation);
        Assert.Empty(config.Capabilities);
        Assert.Empty(config.Settings);
        Assert.Equal("Singleton", config.Lifetime);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var config = new ServiceConfiguration();
        var capabilities = new List<string> { "Cap1", "Cap2" };
        var settings = new Dictionary<string, object> { { "setting1", "value1" } };

        // Act
        config.Implementation = "TestImplementation";
        config.Capabilities = capabilities;
        config.Settings = settings;
        config.Lifetime = "Scoped";
        config.Enabled = false;

        // Assert
        Assert.Equal("TestImplementation", config.Implementation);
        Assert.Same(capabilities, config.Capabilities);
        Assert.Same(settings, config.Settings);
        Assert.Equal("Scoped", config.Lifetime);
        Assert.False(config.Enabled);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public void Lifetime_SupportsCommonValues(string lifetime)
    {
        // Arrange
        var config = new ServiceConfiguration();

        // Act
        config.Lifetime = lifetime;

        // Assert
        Assert.Equal(lifetime, config.Lifetime);
    }

    [Fact]
    public void Capabilities_CanBeModified()
    {
        // Arrange
        var config = new ServiceConfiguration();

        // Act
        config.Capabilities.Add("Capability1");
        config.Capabilities.Add("Capability2");

        // Assert
        Assert.Equal(2, config.Capabilities.Count);
        Assert.Contains("Capability1", config.Capabilities);
        Assert.Contains("Capability2", config.Capabilities);
    }

    [Fact]
    public void Settings_CanBeModified()
    {
        // Arrange
        var config = new ServiceConfiguration();

        // Act
        config.Settings["stringValue"] = "test";
        config.Settings["intValue"] = 42;
        config.Settings["boolValue"] = true;

        // Assert
        Assert.Equal(3, config.Settings.Count);
        Assert.Equal("test", config.Settings["stringValue"]);
        Assert.Equal(42, config.Settings["intValue"]);
        Assert.Equal(true, config.Settings["boolValue"]);
    }
}