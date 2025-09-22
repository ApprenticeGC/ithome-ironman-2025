using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services.Tests;

public class UIProfileConfigurationServiceTests : IDisposable
{
    private readonly UIProfileConfigurationService _service;
    private readonly ILogger<UIProfileConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _testConfigDirectory;

    public UIProfileConfigurationServiceTests()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<UIProfileConfigurationService>();
        _configuration = new ConfigurationBuilder().Build();
        _service = new UIProfileConfigurationService(_logger, _configuration);
        _testConfigDirectory = Path.Combine(Path.GetTempPath(), "GameConsole_Tests", Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task Service_Should_Initialize_Successfully()
    {
        // Act
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
        
        var profiles = await _service.GetAllProfilesAsync();
        Assert.NotEmpty(profiles);
        
        // Should have built-in profiles
        Assert.Contains(profiles, p => p.Id == "tui-default");
        Assert.Contains(profiles, p => p.Id == "gui-default");
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task GetActiveProfile_Should_Return_Default_Profile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var activeProfile = await _service.GetActiveProfileAsync();

        // Assert
        Assert.NotNull(activeProfile);
        Assert.Equal("tui-default", activeProfile.Id);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task CreateProfile_Should_Create_Valid_Profile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var newProfile = new UIProfile
        {
            Id = "custom-profile",
            Name = "Custom Profile",
            Description = "A custom test profile",
            Mode = "TUI",
            Settings = new UIProfileSettings
            {
                Theme = new UIThemeSettings
                {
                    ColorScheme = "Custom",
                    FontSize = 16
                }
            }
        };

        // Act
        var result = await _service.CreateProfileAsync(newProfile);

        // Assert
        Assert.True(result);
        
        var retrievedProfile = await _service.GetProfileAsync("custom-profile");
        Assert.NotNull(retrievedProfile);
        Assert.Equal("Custom Profile", retrievedProfile.Name);
        Assert.Equal("Custom", retrievedProfile.Settings.Theme.ColorScheme);
        Assert.Equal(16, retrievedProfile.Settings.Theme.FontSize);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task CreateProfile_With_Invalid_Data_Should_Fail()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var invalidProfile = new UIProfile
        {
            Id = "", // Invalid: empty ID
            Name = "Test",
            Mode = "TUI"
        };

        // Act
        var result = await _service.CreateProfileAsync(invalidProfile);

        // Assert
        Assert.False(result);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task UpdateProfile_Should_Update_Existing_Profile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var newProfile = new UIProfile
        {
            Id = "update-test",
            Name = "Original Name",
            Mode = "TUI"
        };

        await _service.CreateProfileAsync(newProfile);

        var updatedProfile = newProfile with
        {
            Name = "Updated Name",
            Description = "Updated description"
        };

        // Act
        var result = await _service.UpdateProfileAsync(updatedProfile);

        // Assert
        Assert.True(result);
        
        var retrievedProfile = await _service.GetProfileAsync("update-test");
        Assert.NotNull(retrievedProfile);
        Assert.Equal("Updated Name", retrievedProfile.Name);
        Assert.Equal("Updated description", retrievedProfile.Description);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task UpdateProfile_BuiltIn_Should_Fail()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var builtInProfile = await _service.GetProfileAsync("tui-default");
        Assert.NotNull(builtInProfile);
        
        var modifiedProfile = builtInProfile with
        {
            Name = "Modified Built-in"
        };

        // Act
        var result = await _service.UpdateProfileAsync(modifiedProfile);

        // Assert
        Assert.False(result);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task DeleteProfile_Should_Remove_Custom_Profile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var newProfile = new UIProfile
        {
            Id = "delete-test",
            Name = "To Delete",
            Mode = "TUI"
        };

        await _service.CreateProfileAsync(newProfile);
        
        // Verify it exists
        var profile = await _service.GetProfileAsync("delete-test");
        Assert.NotNull(profile);

        // Act
        var result = await _service.DeleteProfileAsync("delete-test");

        // Assert
        Assert.True(result);
        
        var deletedProfile = await _service.GetProfileAsync("delete-test");
        Assert.Null(deletedProfile);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task DeleteProfile_BuiltIn_Should_Fail()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.DeleteProfileAsync("tui-default");

        // Assert
        Assert.False(result);
        
        // Verify built-in profile still exists
        var profile = await _service.GetProfileAsync("tui-default");
        Assert.NotNull(profile);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task ActivateProfile_Should_Change_Active_Profile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.ActivateProfileAsync("gui-default");

        // Assert
        Assert.True(result);
        
        var activeProfile = await _service.GetActiveProfileAsync();
        Assert.NotNull(activeProfile);
        Assert.Equal("gui-default", activeProfile.Id);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task ValidateProfile_Should_Return_Success_For_Valid_Profile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var validProfile = new UIProfile
        {
            Id = "valid-profile",
            Name = "Valid Profile",
            Mode = "TUI",
            Settings = new UIProfileSettings
            {
                Theme = new UIThemeSettings { FontSize = 12 },
                Rendering = new UIRenderingSettings { MaxFPS = 60 }
            }
        };

        // Act
        var result = await _service.ValidateProfileAsync(validProfile);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task ValidateProfile_Should_Return_Errors_For_Invalid_Profile()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var invalidProfile = new UIProfile
        {
            Id = "", // Invalid
            Name = "", // Invalid
            Mode = "" // Invalid
        };

        // Act
        var result = await _service.ValidateProfileAsync(invalidProfile);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Profile ID cannot be empty"));
        Assert.Contains(result.Errors, e => e.Contains("Profile name cannot be empty"));
        Assert.Contains(result.Errors, e => e.Contains("Profile mode cannot be empty"));
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task ValidateProfile_Should_Return_Warnings_For_Edge_Cases()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var edgeCaseProfile = new UIProfile
        {
            Id = "edge-case",
            Name = "Edge Case",
            Mode = "TUI",
            Settings = new UIProfileSettings
            {
                Theme = new UIThemeSettings { FontSize = 5 }, // Too small
                Rendering = new UIRenderingSettings { MaxFPS = 300 } // Too high
            }
        };

        // Act
        var result = await _service.ValidateProfileAsync(edgeCaseProfile);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task GetCurrentMode_Should_Return_Active_Profile_Mode()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        await _service.ActivateProfileAsync("gui-default");

        // Act
        var currentMode = await _service.GetCurrentModeAsync();

        // Assert
        Assert.Equal("GUI", currentMode);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task GetSupportedModes_Should_Return_Expected_Modes()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var supportedModes = await _service.GetSupportedModesAsync();

        // Assert
        Assert.Contains("TUI", supportedModes);
        Assert.Contains("GUI", supportedModes);
        Assert.Contains("Mixed", supportedModes);
        
        await _service.StopAsync();
    }

    [Fact]
    public async Task ProfileEvents_Should_Fire_When_Profile_Changes()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        UIProfileChangedEventArgs? createdEvent = null;
        UIProfileChangedEventArgs? activatedEvent = null;
        
        _service.ProfileCreated += (sender, args) => createdEvent = args;
        _service.ProfileActivated += (sender, args) => activatedEvent = args;

        var newProfile = new UIProfile
        {
            Id = "event-test",
            Name = "Event Test",
            Mode = "TUI"
        };

        // Act
        await _service.CreateProfileAsync(newProfile);
        await _service.ActivateProfileAsync("event-test");

        // Assert
        Assert.NotNull(createdEvent);
        Assert.Equal("event-test", createdEvent.Profile.Id);
        Assert.Equal(UIProfileChangeType.Created, createdEvent.ChangeType);
        
        Assert.NotNull(activatedEvent);
        Assert.Equal("event-test", activatedEvent.Profile.Id);
        Assert.Equal(UIProfileChangeType.Activated, activatedEvent.ChangeType);
        
        await _service.StopAsync();
    }

    public void Dispose()
    {
        _service?.DisposeAsync().AsTask().Wait();
        
        // Clean up test directory if it exists
        if (Directory.Exists(_testConfigDirectory))
        {
            Directory.Delete(_testConfigDirectory, true);
        }
    }
}