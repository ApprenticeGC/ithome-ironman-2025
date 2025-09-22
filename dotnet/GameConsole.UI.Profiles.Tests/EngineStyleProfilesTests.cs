using GameConsole.UI.Profiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.UI.Profiles.Tests;

public class EngineStyleProfilesTests
{
    private readonly Mock<ILogger> _loggerMock;

    public EngineStyleProfilesTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    #region UnityStyleProfile Tests

    [Fact]
    public void UnityStyleProfile_Constructor_SetsPropertiesCorrectly()
    {
        // Act
        var profile = new UnityStyleProfile(_loggerMock.Object);

        // Assert
        Assert.Equal(UnityStyleProfile.DefaultId, profile.Id);
        Assert.Equal(UnityStyleProfile.DefaultName, profile.Name);
        Assert.Equal(UnityStyleProfile.DefaultDescription, profile.Description);
        Assert.Equal(UIMode.UnityStyle, profile.Mode);
        Assert.False(profile.IsActive);
    }

    [Fact]
    public void UnityStyleProfile_DefaultProperties_AreSetCorrectly()
    {
        // Arrange
        var profile = new UnityStyleProfile(_loggerMock.Object);

        // Act & Assert
        Assert.Equal("Unity-Style", profile.GetProperty<string>("framework"));
        Assert.Equal("Component-Based", profile.GetProperty<string>("uiType"));
        Assert.True(profile.GetProperty<bool>("supportsGameObjects"));
        Assert.True(profile.GetProperty<bool>("supportsComponents"));
        Assert.Equal("Canvas", profile.GetProperty<string>("defaultCanvas"));
        Assert.True(profile.GetProperty<bool>("eventSystemRequired"));
        Assert.Equal("rect-transform", profile.GetProperty<string>("layoutMode"));
        Assert.Equal("flexible", profile.GetProperty<string>("anchorMode"));
        Assert.Equal("scale-with-screen-size", profile.GetProperty<string>("scalingMode"));
    }

    [Fact]
    public async Task UnityStyleProfile_Activation_SetsRuntimeProperties()
    {
        // Arrange
        var profile = new UnityStyleProfile(_loggerMock.Object);

        // Act
        await profile.ActivateAsync();

        // Assert
        Assert.True(profile.IsActive);
        Assert.Equal("component-based", profile.GetProperty<string>("renderer"));
        Assert.Equal("gameobject-tree", profile.GetProperty<string>("hierarchy"));
        Assert.Equal("unity-input-system", profile.GetProperty<string>("inputMethod"));
        Assert.Equal("screen-space-overlay", profile.GetProperty<string>("canvasMode"));
        Assert.Equal("unity-ui-events", profile.GetProperty<string>("eventSystem"));
    }

    [Fact]
    public void UnityStyleProfile_CustomConstructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string customId = "custom-unity";
        const string customName = "Custom Unity";
        const string customDescription = "Custom Unity Description";

        // Act
        var profile = new UnityStyleProfile(customId, customName, customDescription, _loggerMock.Object);

        // Assert
        Assert.Equal(customId, profile.Id);
        Assert.Equal(customName, profile.Name);
        Assert.Equal(customDescription, profile.Description);
        Assert.Equal(UIMode.UnityStyle, profile.Mode);
    }

    #endregion

    #region GodotStyleProfile Tests

    [Fact]
    public void GodotStyleProfile_Constructor_SetsPropertiesCorrectly()
    {
        // Act
        var profile = new GodotStyleProfile(_loggerMock.Object);

        // Assert
        Assert.Equal(GodotStyleProfile.DefaultId, profile.Id);
        Assert.Equal(GodotStyleProfile.DefaultName, profile.Name);
        Assert.Equal(GodotStyleProfile.DefaultDescription, profile.Description);
        Assert.Equal(UIMode.GodotStyle, profile.Mode);
        Assert.False(profile.IsActive);
    }

    [Fact]
    public void GodotStyleProfile_DefaultProperties_AreSetCorrectly()
    {
        // Arrange
        var profile = new GodotStyleProfile(_loggerMock.Object);

        // Act & Assert
        Assert.Equal("Godot-Style", profile.GetProperty<string>("framework"));
        Assert.Equal("Scene-Based", profile.GetProperty<string>("uiType"));
        Assert.True(profile.GetProperty<bool>("supportsNodes"));
        Assert.True(profile.GetProperty<bool>("supportsScenes"));
        Assert.Equal("Main", profile.GetProperty<string>("defaultScene"));
        Assert.True(profile.GetProperty<bool>("signalSystemEnabled"));
        Assert.Equal("container-based", profile.GetProperty<string>("layoutMode"));
        Assert.Equal("anchor-points", profile.GetProperty<string>("anchorMode"));
        Assert.Equal("viewport-scaling", profile.GetProperty<string>("scalingMode"));
    }

    [Fact]
    public async Task GodotStyleProfile_Activation_SetsRuntimeProperties()
    {
        // Arrange
        var profile = new GodotStyleProfile(_loggerMock.Object);

        // Act
        await profile.ActivateAsync();

        // Assert
        Assert.True(profile.IsActive);
        Assert.Equal("scene-based", profile.GetProperty<string>("renderer"));
        Assert.Equal("node-tree", profile.GetProperty<string>("hierarchy"));
        Assert.Equal("godot-input", profile.GetProperty<string>("inputMethod"));
        Assert.Equal("2d-scene", profile.GetProperty<string>("sceneMode"));
        Assert.Equal("godot-signals", profile.GetProperty<string>("signalSystem"));
    }

    [Fact]
    public void GodotStyleProfile_CustomConstructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string customId = "custom-godot";
        const string customName = "Custom Godot";
        const string customDescription = "Custom Godot Description";

        // Act
        var profile = new GodotStyleProfile(customId, customName, customDescription, _loggerMock.Object);

        // Assert
        Assert.Equal(customId, profile.Id);
        Assert.Equal(customName, profile.Name);
        Assert.Equal(customDescription, profile.Description);
        Assert.Equal(UIMode.GodotStyle, profile.Mode);
    }

    #endregion

    #region Profile Comparison Tests

    [Fact]
    public void DifferentProfileTypes_HaveDifferentModes()
    {
        // Arrange & Act
        var unityProfile = new UnityStyleProfile(_loggerMock.Object);
        var godotProfile = new GodotStyleProfile(_loggerMock.Object);

        // Assert
        Assert.Equal(UIMode.UnityStyle, unityProfile.Mode);
        Assert.Equal(UIMode.GodotStyle, godotProfile.Mode);
        Assert.NotEqual(unityProfile.Mode, godotProfile.Mode);
    }

    [Fact]
    public void DifferentProfileTypes_HaveDistinctFrameworkProperties()
    {
        // Arrange & Act
        var unityProfile = new UnityStyleProfile(_loggerMock.Object);
        var godotProfile = new GodotStyleProfile(_loggerMock.Object);

        // Assert
        Assert.Equal("Unity-Style", unityProfile.GetProperty<string>("framework"));
        Assert.Equal("Godot-Style", godotProfile.GetProperty<string>("framework"));

        Assert.Equal("Component-Based", unityProfile.GetProperty<string>("uiType"));
        Assert.Equal("Scene-Based", godotProfile.GetProperty<string>("uiType"));
    }

    [Fact]
    public async Task ProfileActivationAndDeactivation_WorksIndependently()
    {
        // Arrange
        var unityProfile = new UnityStyleProfile(_loggerMock.Object);
        var godotProfile = new GodotStyleProfile(_loggerMock.Object);

        // Act - Activate both
        await unityProfile.ActivateAsync();
        await godotProfile.ActivateAsync();

        // Assert - Both should be active
        Assert.True(unityProfile.IsActive);
        Assert.True(godotProfile.IsActive);

        // Act - Deactivate Unity
        await unityProfile.DeactivateAsync();

        // Assert - Only Unity should be deactivated
        Assert.False(unityProfile.IsActive);
        Assert.True(godotProfile.IsActive);

        // Cleanup
        await godotProfile.DeactivateAsync();
        Assert.False(godotProfile.IsActive);
    }

    #endregion
}