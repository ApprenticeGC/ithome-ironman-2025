using GameConsole.Profile.Core;
using GameConsole.Profile.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.Profile.Services.Tests;

/// <summary>
/// Tests for the ProfileManager class.
/// </summary>
public class ProfileManagerTests
{
    private readonly Mock<ILogger<ProfileManager>> _mockLogger;
    private readonly MemoryProfileProvider _profileProvider;
    private readonly ProfileManager _profileManager;

    public ProfileManagerTests()
    {
        _mockLogger = new Mock<ILogger<ProfileManager>>();
        _profileProvider = new MemoryProfileProvider();
        _profileManager = new ProfileManager(_mockLogger.Object, _profileProvider);
    }

    [Fact]
    public async Task InitializeAsync_LoadsActiveProfile()
    {
        // Arrange
        await _profileProvider.SetActiveProfileIdAsync("unity-style");

        // Act
        await _profileManager.InitializeAsync();

        // Assert
        var activeProfile = await _profileManager.GetActiveProfileAsync();
        Assert.Equal("unity-style", activeProfile.Id);
        Assert.Equal("Unity-Style", activeProfile.Name);
    }

    [Fact]
    public async Task GetActiveProfileAsync_WithNoActiveProfile_ReturnsDefault()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var activeProfile = await _profileManager.GetActiveProfileAsync();

        // Assert
        Assert.NotNull(activeProfile);
        Assert.Equal(ProfileType.Default, activeProfile.Type);
    }

    [Fact]
    public async Task SetActiveProfileAsync_ChangesActiveProfile()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var eventRaised = false;
        IProfile? newProfile = null;

        _profileManager.ActiveProfileChanged += (sender, args) =>
        {
            eventRaised = true;
            newProfile = args.NewProfile;
        };

        // Act
        await _profileManager.SetActiveProfileAsync("unity-style");

        // Assert
        var activeProfile = await _profileManager.GetActiveProfileAsync();
        Assert.Equal("unity-style", activeProfile.Id);
        Assert.True(eventRaised);
        Assert.NotNull(newProfile);
        Assert.Equal("unity-style", newProfile.Id);
    }

    [Fact]
    public async Task SetActiveProfileAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _profileManager.SetActiveProfileAsync("nonexistent"));
    }

    [Fact]
    public async Task CreateProfileAsync_Unity_AppliesUnityDefaults()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var profile = await _profileManager.CreateProfileAsync("My Unity Profile", ProfileType.Unity, "Unity-style gaming setup");

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("My Unity Profile", profile.Name);
        Assert.Equal(ProfileType.Unity, profile.Type);
        Assert.Equal("Unity-style gaming setup", profile.Description);
        
        // Check that Unity-specific configurations were applied
        Assert.True(profile.ServiceConfigurations.ContainsKey("IInputService"));
        Assert.True(profile.ServiceConfigurations.ContainsKey("IGraphicsService"));
        Assert.True(profile.ServiceConfigurations.ContainsKey("IPhysicsService"));
        
        var inputConfig = profile.ServiceConfigurations["IInputService"];
        Assert.Equal("UnityStyleInputService", inputConfig.Implementation);
        Assert.Contains("unityCompat", inputConfig.Settings.Keys);
        Assert.Contains("KeyboardInput", inputConfig.Capabilities);
    }

    [Fact]
    public async Task CreateProfileAsync_Godot_AppliesGodotDefaults()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var profile = await _profileManager.CreateProfileAsync("My Godot Profile", ProfileType.Godot);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(ProfileType.Godot, profile.Type);
        
        // Check that Godot-specific configurations were applied
        Assert.True(profile.ServiceConfigurations.ContainsKey("IInputService"));
        Assert.True(profile.ServiceConfigurations.ContainsKey("IGraphicsService"));
        Assert.True(profile.ServiceConfigurations.ContainsKey("IPhysicsService"));
        
        var inputConfig = profile.ServiceConfigurations["IInputService"];
        Assert.Equal("GodotStyleInputService", inputConfig.Implementation);
        Assert.Contains("godotCompat", inputConfig.Settings.Keys);
        Assert.Contains("ActionMapInput", inputConfig.Capabilities);
    }

    [Fact]
    public async Task CreateProfileAsync_Minimal_AppliesMinimalDefaults()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var profile = await _profileManager.CreateProfileAsync("My Minimal Profile", ProfileType.Minimal);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(ProfileType.Minimal, profile.Type);
        
        // Check that minimal configurations were applied
        Assert.True(profile.ServiceConfigurations.ContainsKey("IInputService"));
        Assert.True(profile.ServiceConfigurations.ContainsKey("IGraphicsService"));
        
        var inputConfig = profile.ServiceConfigurations["IInputService"];
        Assert.Equal("BasicInputService", inputConfig.Implementation);
        Assert.Contains("minimal", inputConfig.Settings.Keys);
        Assert.Equal(1, inputConfig.Capabilities.Count); // Only keyboard input
    }

    [Fact]
    public async Task CreateProfileAsync_Development_AppliesDevelopmentDefaults()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var profile = await _profileManager.CreateProfileAsync("My Dev Profile", ProfileType.Development);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(ProfileType.Development, profile.Type);
        
        // Check that development configurations were applied
        Assert.True(profile.ServiceConfigurations.ContainsKey("IInputService"));
        Assert.True(profile.ServiceConfigurations.ContainsKey("IGraphicsService"));
        Assert.True(profile.ServiceConfigurations.ContainsKey("ILoggingService"));
        
        var inputConfig = profile.ServiceConfigurations["IInputService"];
        Assert.Equal("DebugInputService", inputConfig.Implementation);
        Assert.Contains("debugMode", inputConfig.Settings.Keys);
        Assert.Contains("InputDebugging", inputConfig.Capabilities);
    }

    [Fact]
    public async Task CreateProfileAsync_Custom_StartsEmpty()
    {
        // Arrange
        await _profileManager.InitializeAsync();

        // Act
        var profile = await _profileManager.CreateProfileAsync("My Custom Profile", ProfileType.Custom);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(ProfileType.Custom, profile.Type);
        Assert.Empty(profile.ServiceConfigurations); // Custom profiles start empty
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesProfile()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var profile = await _profileManager.CreateProfileAsync("Test Profile", ProfileType.Custom);
        var mutableProfile = (GameConsole.Profile.Core.Profile)profile; // Cast to access mutation methods
        mutableProfile.SetServiceConfiguration("ITestService", new ServiceConfiguration 
        { 
            Implementation = "TestImplementation" 
        });

        // Act
        await _profileManager.UpdateProfileAsync(profile);

        // Assert
        var updatedProfile = await _profileManager.GetProfileAsync(profile.Id);
        Assert.NotNull(updatedProfile);
        Assert.True(updatedProfile.ServiceConfigurations.ContainsKey("ITestService"));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithReadOnlyProfile_ThrowsException()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var readOnlyProfile = await _profileManager.GetProfileAsync("default"); // Default profile is read-only

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _profileManager.UpdateProfileAsync(readOnlyProfile!));
    }

    [Fact]
    public async Task DeleteProfileAsync_DeletesProfile()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var profile = await _profileManager.CreateProfileAsync("To Delete", ProfileType.Custom);

        // Act
        var result = await _profileManager.DeleteProfileAsync(profile.Id);

        // Assert
        Assert.True(result);
        var deletedProfile = await _profileManager.GetProfileAsync(profile.Id);
        Assert.Null(deletedProfile);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithActiveProfile_SwitchesToDefault()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var profile = await _profileManager.CreateProfileAsync("Active Profile", ProfileType.Custom);
        await _profileManager.SetActiveProfileAsync(profile.Id);

        // Act
        await _profileManager.DeleteProfileAsync(profile.Id);

        // Assert
        var activeProfile = await _profileManager.GetActiveProfileAsync();
        Assert.Equal(ProfileType.Default, activeProfile.Type);
    }

    [Fact]
    public async Task ValidateProfileAsync_WithValidProfile_ReturnsTrue()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var profile = await _profileManager.CreateProfileAsync("Valid Profile", ProfileType.Unity);

        // Act
        var result = await _profileManager.ValidateProfileAsync(profile);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateProfileAsync_WithInvalidProfile_ReturnsFalse()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        var profile = new GameConsole.Profile.Core.Profile("", "", ProfileType.Custom); // Invalid - empty ID and name

        // Act
        var result = await _profileManager.ValidateProfileAsync(profile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllProfilesAsync_ReturnsAllProfiles()
    {
        // Arrange
        await _profileManager.InitializeAsync();
        await _profileManager.CreateProfileAsync("Custom 1", ProfileType.Custom);
        await _profileManager.CreateProfileAsync("Custom 2", ProfileType.Custom);

        // Act
        var profiles = await _profileManager.GetAllProfilesAsync();
        var profileList = profiles.ToList();

        // Assert
        Assert.True(profileList.Count >= 6); // 4 default + 2 custom
        Assert.Contains(profileList, p => p.Name == "Custom 1");
        Assert.Contains(profileList, p => p.Name == "Custom 2");
    }

    [Fact]
    public async Task ServiceLifecycle_WorksCorrectly()
    {
        // Test the IService implementation
        
        // Start
        Assert.False(_profileManager.IsRunning);
        await _profileManager.InitializeAsync();
        await _profileManager.StartAsync();
        Assert.True(_profileManager.IsRunning);

        // Stop
        await _profileManager.StopAsync();
        Assert.False(_profileManager.IsRunning);

        // Dispose
        await _profileManager.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _profileManager.InitializeAsync());
    }
}