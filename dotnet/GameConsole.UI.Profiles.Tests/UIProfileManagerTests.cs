using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the UIProfileManager class, validating profile registration, switching, and lifecycle management.
/// These tests ensure the acceptance criteria for RFC-011-01 are met.
/// </summary>
public class UIProfileManagerTests
{
    /// <summary>
    /// Test that profiles can be registered and retrieved successfully.
    /// Validates: Profile manager handles registration and discovery
    /// </summary>
    [Fact]
    public void RegisterProfile_Should_Store_Profile_Successfully()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var profile = new TestUIProfile("TestProfile", ConsoleMode.Game);

        // Act
        manager.RegisterProfile(profile);
        var retrievedProfile = manager.GetProfile("TestProfile");

        // Assert
        Assert.NotNull(retrievedProfile);
        Assert.Equal("TestProfile", retrievedProfile.Name);
        Assert.Equal(ConsoleMode.Game, retrievedProfile.TargetMode);
    }

    /// <summary>
    /// Test that profiles can be unregistered successfully.
    /// Validates: Profile manager handles cleanup and removal
    /// </summary>
    [Fact]
    public void UnregisterProfile_Should_Remove_Profile_Successfully()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var profile = new TestUIProfile("TestProfile", ConsoleMode.Game);
        manager.RegisterProfile(profile);

        // Act
        var result = manager.UnregisterProfile("TestProfile");
        var retrievedProfile = manager.GetProfile("TestProfile");

        // Assert
        Assert.True(result);
        Assert.Null(retrievedProfile);
    }

    /// <summary>
    /// Test that profiles are grouped correctly by console mode.
    /// Validates: Profile manager organizes profiles by mode
    /// </summary>
    [Fact]
    public void GetProfilesForMode_Should_Return_Correct_Profiles()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var gameProfile = new TestUIProfile("GameProfile", ConsoleMode.Game);
        var editorProfile = new TestUIProfile("EditorProfile", ConsoleMode.Editor);
        var webProfile = new TestUIProfile("WebProfile", ConsoleMode.Web);

        manager.RegisterProfile(gameProfile);
        manager.RegisterProfile(editorProfile);
        manager.RegisterProfile(webProfile);

        // Act
        var gameProfiles = manager.GetProfilesForMode(ConsoleMode.Game);
        var editorProfiles = manager.GetProfilesForMode(ConsoleMode.Editor);
        var desktopProfiles = manager.GetProfilesForMode(ConsoleMode.Desktop);

        // Assert
        Assert.Single(gameProfiles);
        Assert.Equal("GameProfile", gameProfiles.First().Name);
        
        Assert.Single(editorProfiles);
        Assert.Equal("EditorProfile", editorProfiles.First().Name);
        
        Assert.Empty(desktopProfiles);
    }

    /// <summary>
    /// Test successful profile switching with validation.
    /// Validates: Dynamic switching between Console, Web, Desktop profiles
    /// </summary>
    [Fact]
    public async Task SwitchToProfileAsync_Should_Switch_Successfully_With_Valid_Profile()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var profile = new TestUIProfile("TestProfile", ConsoleMode.Game);
        var context = new TestUIContext(ConsoleMode.Game);

        manager.RegisterProfile(profile);
        await manager.InitializeAsync();
        await manager.StartAsync();

        // Act
        var result = await manager.SwitchToProfileAsync("TestProfile", context);

        // Assert
        Assert.True(result);
        Assert.NotNull(manager.ActiveProfile);
        Assert.Equal("TestProfile", manager.ActiveProfile.Name);
    }

    /// <summary>
    /// Test that switching to non-existent profile fails gracefully.
    /// Validates: Profile manager handles invalid profile requests
    /// </summary>
    [Fact]
    public async Task SwitchToProfileAsync_Should_Fail_With_Invalid_Profile()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var context = new TestUIContext(ConsoleMode.Game);

        await manager.InitializeAsync();
        await manager.StartAsync();

        // Act
        var result = await manager.SwitchToProfileAsync("NonExistentProfile", context);

        // Assert
        Assert.False(result);
        Assert.Null(manager.ActiveProfile);
    }

    /// <summary>
    /// Test that manager lifecycle operations work correctly.
    /// Validates: Service lifecycle implementation
    /// </summary>
    [Fact]
    public async Task Manager_Lifecycle_Should_Work_Correctly()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);

        // Act & Assert
        Assert.False(manager.IsRunning);

        await manager.InitializeAsync();
        Assert.False(manager.IsRunning);

        await manager.StartAsync();
        Assert.True(manager.IsRunning);

        await manager.StopAsync();
        Assert.False(manager.IsRunning);
    }
}