using GameConsole.Profile.Core;
using Xunit;

namespace GameConsole.Profile.Core.Tests;

/// <summary>
/// Tests for the built-in profile configurations.
/// </summary>
public class BuiltInProfilesTests
{
    [Fact]
    public async Task TuiProfile_HasCorrectProperties()
    {
        // Arrange
        var profile = new TuiProfile();

        // Act & Assert
        Assert.Equal("tui", profile.ProfileId);
        Assert.Equal("Terminal User Interface (TUI)", profile.DisplayName);
        Assert.Equal(100, profile.Priority);
        Assert.True(await profile.IsSupported());
    }

    [Fact]
    public async Task TuiProfile_HasCorrectConfiguration()
    {
        // Arrange
        var profile = new TuiProfile();

        // Act
        var settings = await profile.GetConfigurationSettings();

        // Assert
        Assert.Equal("terminal", settings["ui.mode"]);
        Assert.Equal("text", settings["graphics.mode"]);
        Assert.Equal("keyboard", settings["input.method"]);
        Assert.Equal(true, settings["output.colors"]);
        Assert.Equal(80, settings["display.width"]);
        Assert.Equal(24, settings["display.height"]);
    }

    [Fact]
    public async Task UnityProfile_HasCorrectProperties()
    {
        // Arrange
        var profile = new UnityProfile();

        // Act & Assert
        Assert.Equal("unity", profile.ProfileId);
        Assert.Equal("Unity-like Interface", profile.DisplayName);
        Assert.Equal(50, profile.Priority);
        Assert.True(await profile.IsSupported());
    }

    [Fact]
    public async Task UnityProfile_HasCorrectConfiguration()
    {
        // Arrange
        var profile = new UnityProfile();

        // Act
        var settings = await profile.GetConfigurationSettings();

        // Assert
        Assert.Equal("unity", settings["ui.mode"]);
        Assert.Equal("gameobject", settings["graphics.mode"]);
        Assert.Equal("unity_input", settings["input.method"]);
        Assert.Equal(true, settings["scene.management"]);
        Assert.Equal("unity", settings["component.system"]);
        Assert.Equal(true, settings["inspector.enabled"]);
        Assert.Equal(true, settings["hierarchy.enabled"]);
    }

    [Fact]
    public async Task GodotProfile_HasCorrectProperties()
    {
        // Arrange
        var profile = new GodotProfile();

        // Act & Assert
        Assert.Equal("godot", profile.ProfileId);
        Assert.Equal("Godot-like Interface", profile.DisplayName);
        Assert.Equal(40, profile.Priority);
        Assert.True(await profile.IsSupported());
    }

    [Fact]
    public async Task GodotProfile_HasCorrectConfiguration()
    {
        // Arrange
        var profile = new GodotProfile();

        // Act
        var settings = await profile.GetConfigurationSettings();

        // Assert
        Assert.Equal("godot", settings["ui.mode"]);
        Assert.Equal("node2d", settings["graphics.mode"]);
        Assert.Equal("godot_input", settings["input.method"]);
        Assert.Equal(true, settings["scene.tree"]);
        Assert.Equal("godot", settings["node.system"]);
        Assert.Equal(true, settings["signals.enabled"]);
        Assert.Equal("gdscript", settings["scripting.language"]);
    }

    [Fact]
    public void ProfilePriorityOrder_IsCorrect()
    {
        // Arrange
        var tuiProfile = new TuiProfile();
        var unityProfile = new UnityProfile();
        var godotProfile = new GodotProfile();
        var profiles = new IProfileConfiguration[] { godotProfile, unityProfile, tuiProfile };

        // Act
        var sortedProfiles = profiles.OrderByDescending(p => p.Priority).ToArray();

        // Assert
        Assert.Equal(tuiProfile, sortedProfiles[0]);   // Priority 100
        Assert.Equal(unityProfile, sortedProfiles[1]); // Priority 50
        Assert.Equal(godotProfile, sortedProfiles[2]); // Priority 40
    }

    [Fact]
    public async Task AllBuiltInProfiles_ReturnEmptyCapabilityProviders()
    {
        // Arrange
        var profiles = new IProfileConfiguration[]
        {
            new TuiProfile(),
            new UnityProfile(),
            new GodotProfile()
        };

        // Act & Assert
        foreach (var profile in profiles)
        {
            var providers = await profile.GetCapabilityProviders();
            Assert.NotNull(providers);
            Assert.Empty(providers); // Currently empty as providers aren't implemented yet
        }
    }

    [Fact]
    public async Task AllBuiltInProfiles_AreAlwaysSupported()
    {
        // Arrange
        var profiles = new IProfileConfiguration[]
        {
            new TuiProfile(),
            new UnityProfile(),
            new GodotProfile()
        };

        // Act & Assert
        foreach (var profile in profiles)
        {
            Assert.True(await profile.IsSupported());
        }
    }
}