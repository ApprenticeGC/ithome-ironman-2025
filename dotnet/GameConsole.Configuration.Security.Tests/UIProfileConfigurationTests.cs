using GameConsole.Configuration.Security;
using Xunit;

namespace GameConsole.Configuration.Security.Tests;

/// <summary>
/// Tests for the UIProfileConfiguration class.
/// </summary>
public class UIProfileConfigurationTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const string profileId = "test-profile";
        const string name = "Test Profile";
        const string description = "Test description";
        const UIMode mode = UIMode.TUI;

        // Act
        var profile = new UIProfileConfiguration(profileId, name, description, mode);

        // Assert
        Assert.Equal(profileId, profile.ProfileId);
        Assert.Equal(name, profile.Name);
        Assert.Equal(description, profile.Description);
        Assert.Equal(mode, profile.Mode);
        Assert.Empty(profile.Settings);
        Assert.Empty(profile.ProviderMappings);
    }

    [Theory]
    [InlineData(null)]
    public void Constructor_WithInvalidProfileId_ThrowsArgumentException(string? profileId)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIProfileConfiguration(profileId!, "Test", "Description", UIMode.TUI));
    }

    [Fact]
    public void Constructor_WithEmptyProfileId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new UIProfileConfiguration("", "Test", "Description", UIMode.TUI));
    }

    [Theory]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIProfileConfiguration("test", name!, "Description", UIMode.TUI));
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new UIProfileConfiguration("test", "", "Description", UIMode.TUI));
    }

    [Fact]
    public void SetSetting_WithValidKeyValue_StoredCorrectly()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        const string key = "testKey";
        const string value = "testValue";

        // Act
        profile.SetSetting(key, value);

        // Assert
        Assert.Single(profile.Settings);
        Assert.Equal(value, profile.Settings[key]);
    }

    [Fact]
    public void SetSetting_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => profile.SetSetting(null!, "value"));
    }

    [Fact]
    public void GetSetting_WithExistingKey_ReturnsCorrectValue()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        const string key = "testKey";
        const int value = 42;
        profile.SetSetting(key, value);

        // Act
        var result = profile.GetSetting<int>(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void GetSetting_WithNonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        const string key = "nonExistentKey";
        const int defaultValue = 100;

        // Act
        var result = profile.GetSetting(key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GetSetting_WithWrongType_ReturnsDefaultValue()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        const string key = "testKey";
        profile.SetSetting(key, "string value");

        // Act
        var result = profile.GetSetting<int>(key, 999);

        // Assert
        Assert.Equal(999, result);
    }

    [Fact]
    public void MapProvider_WithValidTypes_StoredCorrectly()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        var capabilityType = typeof(IDisposable);
        var providerType = typeof(object);

        // Act
        profile.MapProvider(capabilityType, providerType);

        // Assert
        Assert.Single(profile.ProviderMappings);
        Assert.Equal(providerType, profile.ProviderMappings[capabilityType]);
    }

    [Fact]
    public void MapProvider_WithNullCapabilityType_ThrowsArgumentNullException()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => profile.MapProvider(null!, typeof(object)));
    }

    [Fact]
    public void MapProvider_WithNullProviderType_ThrowsArgumentNullException()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => profile.MapProvider(typeof(IDisposable), null!));
    }

    [Fact]
    public void GetProviderType_WithExistingMapping_ReturnsCorrectType()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        var capabilityType = typeof(IDisposable);
        var providerType = typeof(object);
        profile.MapProvider(capabilityType, providerType);

        // Act
        var result = profile.GetProviderType(capabilityType);

        // Assert
        Assert.Equal(providerType, result);
    }

    [Fact]
    public void GetProviderType_WithNonExistentMapping_ReturnsNull()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);

        // Act
        var result = profile.GetProviderType(typeof(IDisposable));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RemoveSetting_WithExistingKey_RemovesAndReturnsTrue()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        const string key = "testKey";
        profile.SetSetting(key, "value");

        // Act
        var result = profile.RemoveSetting(key);

        // Assert
        Assert.True(result);
        Assert.Empty(profile.Settings);
    }

    [Fact]
    public void RemoveSetting_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);

        // Act
        var result = profile.RemoveSetting("nonExistentKey");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveProviderMapping_WithExistingMapping_RemovesAndReturnsTrue()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);
        var capabilityType = typeof(IDisposable);
        profile.MapProvider(capabilityType, typeof(object));

        // Act
        var result = profile.RemoveProviderMapping(capabilityType);

        // Assert
        Assert.True(result);
        Assert.Empty(profile.ProviderMappings);
    }

    [Fact]
    public void RemoveProviderMapping_WithNonExistentMapping_ReturnsFalse()
    {
        // Arrange
        var profile = new UIProfileConfiguration("test", "Test", "Description", UIMode.TUI);

        // Act
        var result = profile.RemoveProviderMapping(typeof(IDisposable));

        // Assert
        Assert.False(result);
    }
}