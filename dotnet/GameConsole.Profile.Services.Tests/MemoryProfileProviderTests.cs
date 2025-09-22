using GameConsole.Profile.Core;
using GameConsole.Profile.Services;
using Xunit;

namespace GameConsole.Profile.Services.Tests;

/// <summary>
/// Tests for the MemoryProfileProvider class.
/// </summary>
public class MemoryProfileProviderTests
{
    [Fact]
    public async Task LoadProfilesAsync_ReturnsDefaultProfiles()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        var profiles = await provider.LoadProfilesAsync();
        var profileList = profiles.ToList();

        // Assert
        Assert.Equal(4, profileList.Count); // default, unity, godot, minimal
        Assert.Contains(profileList, p => p.Type == ProfileType.Default);
        Assert.Contains(profileList, p => p.Type == ProfileType.Unity);
        Assert.Contains(profileList, p => p.Type == ProfileType.Godot);
        Assert.Contains(profileList, p => p.Type == ProfileType.Minimal);
    }

    [Fact]
    public async Task LoadProfileAsync_WithValidId_ReturnsProfile()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        var profile = await provider.LoadProfileAsync("default");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("default", profile.Id);
        Assert.Equal("Default Profile", profile.Name);
        Assert.Equal(ProfileType.Default, profile.Type);
    }

    [Fact]
    public async Task LoadProfileAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        var profile = await provider.LoadProfileAsync("nonexistent");

        // Assert
        Assert.Null(profile);
    }

    [Fact]
    public async Task SaveProfileAsync_AddsNewProfile()
    {
        // Arrange
        var provider = new MemoryProfileProvider();
        var newProfile = new GameConsole.Profile.Core.Profile("test-id", "Test Profile", ProfileType.Custom, "Test description");

        // Act
        await provider.SaveProfileAsync(newProfile);

        // Assert
        var savedProfile = await provider.LoadProfileAsync("test-id");
        Assert.NotNull(savedProfile);
        Assert.Equal("test-id", savedProfile.Id);
        Assert.Equal("Test Profile", savedProfile.Name);
    }

    [Fact]
    public async Task SaveProfileAsync_UpdatesExistingProfile()
    {
        // Arrange
        var provider = new MemoryProfileProvider();
        var profile1 = new GameConsole.Profile.Core.Profile("test-id", "Original Name", ProfileType.Custom);
        var profile2 = new GameConsole.Profile.Core.Profile("test-id", "Updated Name", ProfileType.Custom);

        // Act
        await provider.SaveProfileAsync(profile1);
        await provider.SaveProfileAsync(profile2);

        // Assert
        var savedProfile = await provider.LoadProfileAsync("test-id");
        Assert.NotNull(savedProfile);
        Assert.Equal("Updated Name", savedProfile.Name);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithExistingProfile_ReturnsTrue()
    {
        // Arrange
        var provider = new MemoryProfileProvider();
        var profile = new GameConsole.Profile.Core.Profile("test-id", "Test Profile", ProfileType.Custom);
        await provider.SaveProfileAsync(profile);

        // Act
        var result = await provider.DeleteProfileAsync("test-id");

        // Assert
        Assert.True(result);
        var deletedProfile = await provider.LoadProfileAsync("test-id");
        Assert.Null(deletedProfile);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithNonexistentProfile_ReturnsFalse()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        var result = await provider.DeleteProfileAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingProfile_ReturnsTrue()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        var result = await provider.ExistsAsync("default");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonexistentProfile_ReturnsFalse()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        var result = await provider.ExistsAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ActiveProfile_CanBeSetAndRetrieved()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        await provider.SetActiveProfileIdAsync("unity-style");
        var activeId = await provider.GetActiveProfileIdAsync();

        // Assert
        Assert.Equal("unity-style", activeId);
    }

    [Fact]
    public async Task DeleteActiveProfile_ClearsActiveProfileId()
    {
        // Arrange
        var provider = new MemoryProfileProvider();
        var profile = new GameConsole.Profile.Core.Profile("test-active", "Test Active", ProfileType.Custom);
        await provider.SaveProfileAsync(profile);
        await provider.SetActiveProfileIdAsync("test-active");

        // Act
        await provider.DeleteProfileAsync("test-active");

        // Assert
        var activeId = await provider.GetActiveProfileIdAsync();
        Assert.Null(activeId);
    }

    [Fact]
    public void Clear_RemovesAllProfiles()
    {
        // Arrange
        var provider = new MemoryProfileProvider();
        var initialCount = provider.Count;

        // Act
        provider.Clear();

        // Assert
        Assert.Equal(0, provider.Count);
        Assert.True(initialCount > 0); // Verify there were profiles initially
    }

    [Fact]
    public async Task DefaultProfiles_HaveCorrectConfigurations()
    {
        // Arrange
        var provider = new MemoryProfileProvider();

        // Act
        var unityProfile = await provider.LoadProfileAsync("unity-style");
        var godotProfile = await provider.LoadProfileAsync("godot-style");
        var minimalProfile = await provider.LoadProfileAsync("minimal");

        // Assert - Unity profile
        Assert.NotNull(unityProfile);
        Assert.True(unityProfile.ServiceConfigurations.ContainsKey("IInputService"));
        Assert.True(unityProfile.ServiceConfigurations.ContainsKey("IGraphicsService"));
        Assert.Equal("UnityStyleInputService", unityProfile.ServiceConfigurations["IInputService"].Implementation);

        // Assert - Godot profile
        Assert.NotNull(godotProfile);
        Assert.True(godotProfile.ServiceConfigurations.ContainsKey("IInputService"));
        Assert.True(godotProfile.ServiceConfigurations.ContainsKey("IGraphicsService"));
        Assert.Equal("GodotStyleInputService", godotProfile.ServiceConfigurations["IInputService"].Implementation);

        // Assert - Minimal profile
        Assert.NotNull(minimalProfile);
        Assert.True(minimalProfile.ServiceConfigurations.ContainsKey("IInputService"));
        Assert.Equal("BasicInputService", minimalProfile.ServiceConfigurations["IInputService"].Implementation);
    }
}