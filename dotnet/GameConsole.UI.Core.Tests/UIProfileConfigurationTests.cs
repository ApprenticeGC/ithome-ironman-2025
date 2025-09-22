using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UIProfileConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new UIProfileConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Id);
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Description);
        Assert.Equal(UIProfileType.Default, config.ProfileType);
        Assert.False(config.IsDefault);
        Assert.True(config.IsEnabled);
        Assert.NotNull(config.Properties);
        Assert.Empty(config.Properties);
        Assert.Equal("1.0.0", config.Version);
        Assert.True(config.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(config.LastModified <= DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("test-id")]
    [InlineData("unity-profile")]
    [InlineData("godot-profile")]
    public void Id_CanBeSetAndRetrieved(string expectedId)
    {
        // Arrange
        var config = new UIProfileConfiguration();

        // Act
        config.Id = expectedId;

        // Assert
        Assert.Equal(expectedId, config.Id);
    }

    [Theory]
    [InlineData(UIProfileType.Unity)]
    [InlineData(UIProfileType.Godot)]
    [InlineData(UIProfileType.CustomTUI)]
    [InlineData(UIProfileType.Default)]
    public void ProfileType_CanBeSetAndRetrieved(UIProfileType expectedType)
    {
        // Arrange
        var config = new UIProfileConfiguration();

        // Act
        config.ProfileType = expectedType;

        // Assert
        Assert.Equal(expectedType, config.ProfileType);
    }

    [Fact]
    public void Properties_CanStoreAndRetrieveValues()
    {
        // Arrange
        var config = new UIProfileConfiguration();
        const string key = "TestKey";
        const string value = "TestValue";

        // Act
        config.Properties[key] = value;

        // Assert
        Assert.True(config.Properties.ContainsKey(key));
        Assert.Equal(value, config.Properties[key]);
    }

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        // Arrange & Act
        var config = new UIProfileConfiguration();

        // Assert
        Assert.True(config.IsEnabled);
    }

    [Fact]
    public void IsDefault_DefaultsToFalse()
    {
        // Arrange & Act
        var config = new UIProfileConfiguration();

        // Assert
        Assert.False(config.IsDefault);
    }
}