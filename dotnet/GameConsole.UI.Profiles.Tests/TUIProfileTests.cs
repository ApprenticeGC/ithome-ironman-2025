using GameConsole.UI.Profiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.UI.Profiles.Tests;

public class TUIProfileTests
{
    private readonly Mock<ILogger> _loggerMock;

    public TUIProfileTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Act
        var profile = new TUIProfile(_loggerMock.Object);

        // Assert
        Assert.Equal(TUIProfile.DefaultId, profile.Id);
        Assert.Equal(TUIProfile.DefaultName, profile.Name);
        Assert.Equal(TUIProfile.DefaultDescription, profile.Description);
        Assert.Equal(UIMode.TUI, profile.Mode);
        Assert.False(profile.IsActive);
    }

    [Fact]
    public void Constructor_CustomParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const string customId = "custom-tui";
        const string customName = "Custom TUI";
        const string customDescription = "Custom TUI Description";

        // Act
        var profile = new TUIProfile(customId, customName, customDescription, _loggerMock.Object);

        // Assert
        Assert.Equal(customId, profile.Id);
        Assert.Equal(customName, profile.Name);
        Assert.Equal(customDescription, profile.Description);
        Assert.Equal(UIMode.TUI, profile.Mode);
        Assert.False(profile.IsActive);
    }

    [Fact]
    public void GetProperty_DefaultProperties_ReturnsExpectedValues()
    {
        // Arrange
        var profile = new TUIProfile(_loggerMock.Object);

        // Act & Assert
        Assert.Equal("GameConsole", profile.GetProperty<string>("framework"));
        Assert.Equal("TUI", profile.GetProperty<string>("uiType"));
        Assert.True(profile.GetProperty<bool>("supportsColor"));
        Assert.True(profile.GetProperty<bool>("supportsInput"));
        Assert.Equal("Consolas", profile.GetProperty<string>("defaultFont"));
    }

    [Fact]
    public void GetProperty_NonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var profile = new TUIProfile(_loggerMock.Object);

        // Act & Assert
        Assert.Equal("default", profile.GetProperty("nonexistent", "default"));
        Assert.Equal(42, profile.GetProperty("nonexistent", 42));
        Assert.False(profile.GetProperty("nonexistent", false));
    }

    [Fact]
    public async Task ActivateAsync_WhenNotActive_ActivatesSuccessfully()
    {
        // Arrange
        var profile = new TUIProfile(_loggerMock.Object);

        // Act
        await profile.ActivateAsync();

        // Assert
        Assert.True(profile.IsActive);
        Assert.Equal("console", profile.GetProperty<string>("renderer"));
        Assert.Equal("keyboard", profile.GetProperty<string>("inputMethod"));
    }

    [Fact]
    public async Task ActivateAsync_WhenAlreadyActive_DoesNotThrow()
    {
        // Arrange
        var profile = new TUIProfile(_loggerMock.Object);
        await profile.ActivateAsync();

        // Act & Assert - Should not throw
        await profile.ActivateAsync();
        Assert.True(profile.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenActive_DeactivatesSuccessfully()
    {
        // Arrange
        var profile = new TUIProfile(_loggerMock.Object);
        await profile.ActivateAsync();

        // Act
        await profile.DeactivateAsync();

        // Assert
        Assert.False(profile.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenNotActive_DoesNotThrow()
    {
        // Arrange
        var profile = new TUIProfile(_loggerMock.Object);

        // Act & Assert - Should not throw
        await profile.DeactivateAsync();
        Assert.False(profile.IsActive);
    }

    [Fact]
    public void Properties_AfterActivation_ContainsActivationProperties()
    {
        // Arrange
        var profile = new TUIProfile(_loggerMock.Object);

        // Act
        profile.ActivateAsync().Wait();

        // Assert
        var properties = profile.Properties;
        Assert.Contains("renderer", properties.Keys);
        Assert.Contains("inputMethod", properties.Keys);
        Assert.Contains("maxWidth", properties.Keys);
        Assert.Contains("maxHeight", properties.Keys);
    }
}