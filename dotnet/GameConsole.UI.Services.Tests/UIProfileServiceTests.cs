using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.UI.Services.Tests;

/// <summary>
/// Unit tests for UI Profile functionality.
/// </summary>
public class UIProfileServiceTests
{
    private readonly ILogger<UIProfileService> _logger;
    private readonly IConfiguration _configuration;

    public UIProfileServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<UIProfileService>();
        
        var configBuilder = new ConfigurationBuilder();
        _configuration = configBuilder.Build();
    }

    [Fact]
    public async Task InitializeAsync_CreatesDefaultProfiles()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);

        // Act
        await service.InitializeAsync();

        // Assert
        var profiles = await service.GetAllProfilesAsync();
        var profileList = profiles.ToList();
        
        Assert.NotEmpty(profileList);
        Assert.Contains(profileList, p => p.Id == "default");
        Assert.Contains(profileList, p => p.Id == "unity-style");
        Assert.Contains(profileList, p => p.Id == "godot-style");
    }

    [Fact]
    public async Task ActiveProfile_SetsToDefaultAfterInitialization()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        // Act & Assert
        Assert.NotNull(service.ActiveProfile);
        Assert.Equal("default", service.ActiveProfile!.Id);
    }

    [Fact]
    public async Task CreateProfileAsync_CreatesNewProfile()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        var graphicsSettings = new Dictionary<string, object>
        {
            { "Resolution", "1920x1080" },
            { "RefreshRate", 144 }
        };

        var uiSettings = new Dictionary<string, object>
        {
            { "Theme", "Custom" },
            { "ShowToolbar", false }
        };

        // Act
        var profile = await service.CreateProfileAsync(
            "custom-test",
            "Custom Test Profile",
            "A profile for testing custom settings",
            "custom-input",
            graphicsSettings,
            uiSettings);

        // Assert
        Assert.Equal("custom-test", profile.Id);
        Assert.Equal("Custom Test Profile", profile.Name);
        Assert.Equal("A profile for testing custom settings", profile.Description);
        Assert.Equal("custom-input", profile.InputProfileName);
        Assert.Equal("1920x1080", profile.GraphicsSettings["Resolution"]);
        Assert.Equal(144, profile.GraphicsSettings["RefreshRate"]);
        Assert.Equal("Custom", profile.UISettings["Theme"]);
        Assert.Equal(false, profile.UISettings["ShowToolbar"]);
    }

    [Fact]
    public async Task CreateProfileAsync_ThrowsForDuplicateId()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.CreateProfileAsync("default", "Duplicate", "This should fail"));
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesExistingProfile()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        var newGraphicsSettings = new Dictionary<string, object>
        {
            { "Resolution", "2560x1440" },
            { "MSAA", 4 }
        };

        var newUISettings = new Dictionary<string, object>
        {
            { "Theme", "CustomDark" }
        };

        // Act
        var updatedProfile = await service.UpdateProfileAsync(
            "default",
            "Updated Default",
            "Updated description",
            "new-input-profile",
            newGraphicsSettings,
            newUISettings);

        // Assert
        Assert.Equal("default", updatedProfile.Id);
        Assert.Equal("Updated Default", updatedProfile.Name);
        Assert.Equal("Updated description", updatedProfile.Description);
        Assert.Equal("new-input-profile", updatedProfile.InputProfileName);
        Assert.Equal("2560x1440", updatedProfile.GraphicsSettings["Resolution"]);
        Assert.Equal(4, updatedProfile.GraphicsSettings["MSAA"]);
        Assert.Equal("CustomDark", updatedProfile.UISettings["Theme"]);
    }

    [Fact]
    public async Task UpdateProfileAsync_ThrowsForNonExistentProfile()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.UpdateProfileAsync("non-existent", "New Name"));
    }

    [Fact]
    public async Task DeleteProfileAsync_RemovesProfile()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        var profile = await service.CreateProfileAsync("temp", "Temporary", "Temporary profile");

        // Act
        await service.DeleteProfileAsync("temp");

        // Assert
        var retrievedProfile = await service.GetProfileAsync("temp");
        Assert.Null(retrievedProfile);
    }

    [Fact]
    public async Task DeleteProfileAsync_DeactivatesActiveProfile()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        var profile = await service.CreateProfileAsync("temp", "Temporary", "Temporary profile");
        await service.ActivateProfileAsync("temp");
        Assert.Equal("temp", service.ActiveProfile!.Id);

        // Act
        await service.DeleteProfileAsync("temp");

        // Assert
        Assert.Null(service.ActiveProfile);
    }

    [Fact]
    public async Task ActivateProfileAsync_ChangesActiveProfile()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        IUIProfile? previousProfile = null;
        IUIProfile? newProfile = null;
        var eventFired = false;

        service.ActiveProfileChanged += (sender, args) =>
        {
            previousProfile = args.PreviousProfile;
            newProfile = args.NewProfile;
            eventFired = true;
        };

        // Act
        await service.ActivateProfileAsync("unity-style");

        // Assert
        Assert.Equal("unity-style", service.ActiveProfile!.Id);
        Assert.True(eventFired);
        Assert.Equal("default", previousProfile!.Id);
        Assert.Equal("unity-style", newProfile!.Id);
    }

    [Fact]
    public async Task ActivateProfileAsync_ThrowsForNonExistentProfile()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.ActivateProfileAsync("non-existent"));
    }

    [Fact]
    public async Task GetAllProfilesAsync_ReturnsAllProfiles()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        await service.CreateProfileAsync("custom1", "Custom 1", "First custom profile");
        await service.CreateProfileAsync("custom2", "Custom 2", "Second custom profile");

        // Act
        var profiles = await service.GetAllProfilesAsync();
        var profileList = profiles.ToList();

        // Assert
        Assert.Equal(5, profileList.Count); // 3 default + 2 custom
        Assert.Contains(profileList, p => p.Id == "default");
        Assert.Contains(profileList, p => p.Id == "unity-style");
        Assert.Contains(profileList, p => p.Id == "godot-style");
        Assert.Contains(profileList, p => p.Id == "custom1");
        Assert.Contains(profileList, p => p.Id == "custom2");
    }

    [Fact]
    public async Task StartStopAsync_ManagesServiceState()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);
        await service.InitializeAsync();

        // Act & Assert - Start
        Assert.False(service.IsRunning);
        await service.StartAsync();
        Assert.True(service.IsRunning);

        // Act & Assert - Stop
        await service.StopAsync();
        Assert.False(service.IsRunning);
    }

    [Fact]
    public async Task StartAsync_ThrowsIfNotInitialized()
    {
        // Arrange
        var service = new UIProfileService(_logger, _configuration);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync());
    }
}

/// <summary>
/// Unit tests for UIProfile class.
/// </summary>
public class UIProfileTests
{
    [Fact]
    public void Constructor_CreatesProfileWithCorrectProperties()
    {
        // Arrange
        var graphicsSettings = new Dictionary<string, object>
        {
            { "Resolution", "1920x1080" },
            { "RefreshRate", 60 }
        };

        var uiSettings = new Dictionary<string, object>
        {
            { "Theme", "Dark" },
            { "ShowToolbar", true }
        };

        var createdAt = DateTime.UtcNow;

        // Act
        var profile = new UIProfile(
            "test-id",
            "Test Profile",
            "A test profile",
            "test-input",
            graphicsSettings,
            uiSettings,
            createdAt);

        // Assert
        Assert.Equal("test-id", profile.Id);
        Assert.Equal("Test Profile", profile.Name);
        Assert.Equal("A test profile", profile.Description);
        Assert.Equal("test-input", profile.InputProfileName);
        Assert.Equal(createdAt, profile.CreatedAt);
        Assert.Equal(createdAt, profile.LastModified);
        
        Assert.Equal("1920x1080", profile.GraphicsSettings["Resolution"]);
        Assert.Equal(60, profile.GraphicsSettings["RefreshRate"]);
        Assert.Equal("Dark", profile.UISettings["Theme"]);
        Assert.Equal(true, profile.UISettings["ShowToolbar"]);
    }

    [Fact]
    public void Update_ModifiesProfileProperties()
    {
        // Arrange
        var profile = new UIProfile("test", "Original", "Original description");
        var originalModified = profile.LastModified;

        // Wait a tiny bit to ensure LastModified changes
        Thread.Sleep(1);

        // Act
        profile.Update("Updated", "Updated description", "new-input");

        // Assert
        Assert.Equal("Updated", profile.Name);
        Assert.Equal("Updated description", profile.Description);
        Assert.Equal("new-input", profile.InputProfileName);
        Assert.True(profile.LastModified > originalModified);
    }

    [Fact]
    public void SetGraphicsSetting_AddsOrUpdatesSetting()
    {
        // Arrange
        var profile = new UIProfile("test", "Test", "Test profile");

        // Act
        profile.SetGraphicsSetting("Resolution", "1920x1080");
        profile.SetGraphicsSetting("MSAA", 4);

        // Assert
        Assert.Equal("1920x1080", profile.GraphicsSettings["Resolution"]);
        Assert.Equal(4, profile.GraphicsSettings["MSAA"]);
    }

    [Fact]
    public void RemoveGraphicsSetting_RemovesSetting()
    {
        // Arrange
        var initialSettings = new Dictionary<string, object>
        {
            { "Resolution", "1920x1080" },
            { "RefreshRate", 60 }
        };

        var profile = new UIProfile("test", "Test", "Test profile", 
            graphicsSettings: initialSettings);

        // Act
        profile.RemoveGraphicsSetting("RefreshRate");

        // Assert
        Assert.Contains("Resolution", profile.GraphicsSettings.Keys);
        Assert.DoesNotContain("RefreshRate", profile.GraphicsSettings.Keys);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var originalSettings = new Dictionary<string, object>
        {
            { "Resolution", "1920x1080" }
        };

        var original = new UIProfile("original", "Original", "Original profile",
            graphicsSettings: originalSettings);

        // Act
        var clone = original.Clone("clone", "Cloned Profile");

        // Assert
        Assert.Equal("clone", clone.Id);
        Assert.Equal("Cloned Profile", clone.Name);
        Assert.Equal("Original profile", clone.Description);
        Assert.Equal("1920x1080", clone.GraphicsSettings["Resolution"]);

        // Verify independence
        clone.SetGraphicsSetting("MSAA", 4);
        Assert.DoesNotContain("MSAA", original.GraphicsSettings.Keys);
        Assert.Contains("MSAA", clone.GraphicsSettings.Keys);
    }
}