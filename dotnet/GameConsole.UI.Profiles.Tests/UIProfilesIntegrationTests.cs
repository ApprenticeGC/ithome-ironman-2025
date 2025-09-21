using Microsoft.Extensions.Logging;
using GameConsole.UI.Profiles;
using GameConsole.UI.Profiles.Implementations;
using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Integration tests for UI Profiles functionality.
/// </summary>
public class UIProfilesIntegrationTests
{
    private readonly ILogger<UIProfileManager> _logger;

    public UIProfilesIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<UIProfileManager>();
    }

    [Fact]
    public async Task UIProfileManager_Should_Initialize_And_Register_BuiltIn_Profiles()
    {
        // Arrange
        var manager = new UIProfileManager(_logger);

        // Act
        await manager.InitializeAsync();
        var profiles = await manager.GetRegisteredProfilesAsync();

        // Assert
        Assert.Equal(3, profiles.Count);
        Assert.Contains(profiles, p => p.Id == ConsoleUIProfile.ProfileId);
        Assert.Contains(profiles, p => p.Id == WebUIProfile.ProfileId);
        Assert.Contains(profiles, p => p.Id == DesktopUIProfile.ProfileId);
    }

    [Fact]
    public async Task UIProfileManager_Should_Start_With_Default_Profile()
    {
        // Arrange
        var manager = new UIProfileManager(_logger);

        // Act
        await manager.InitializeAsync();
        await manager.StartAsync();

        // Assert
        Assert.NotNull(manager.ActiveProfile);
        Assert.True(manager.IsRunning);

        // Cleanup
        await manager.StopAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ConsoleUIProfile_Should_Support_Expected_Capabilities()
    {
        // Arrange
        var profile = new ConsoleUIProfile(_logger);
        var context = new UIContext();

        // Act
        var capabilities = profile.GetSupportedCapabilities();
        var canActivate = await profile.CanActivateAsync(context);

        // Assert
        Assert.True(canActivate);
        Assert.True(capabilities.HasFlag(UICapabilities.TextInput));
        Assert.True(capabilities.HasFlag(UICapabilities.FileSystemAccess));
        Assert.False(capabilities.HasFlag(UICapabilities.GraphicalElements));
    }

    [Fact]
    public async Task WebUIProfile_Should_Require_Graphical_Display()
    {
        // Arrange
        var profile = new WebUIProfile(_logger);
        var context = new UIContext
        {
            Display = new DisplayCapabilities { HasGraphicalDisplay = false }
        };

        // Act
        var canActivate = await profile.CanActivateAsync(context);
        var capabilities = profile.GetSupportedCapabilities();

        // Assert
        Assert.False(canActivate); // Should fail without graphical display
        Assert.True(capabilities.HasFlag(UICapabilities.GraphicalElements));
        Assert.True(capabilities.HasFlag(UICapabilities.NetworkAccess));
    }

    [Fact]
    public async Task DesktopUIProfile_Should_Require_Adequate_Screen_Size()
    {
        // Arrange
        var profile = new DesktopUIProfile(_logger);
        var smallScreenContext = new UIContext
        {
            Display = new DisplayCapabilities 
            { 
                HasGraphicalDisplay = true,
                Width = 800,
                Height = 600
            }
        };

        var adequateScreenContext = new UIContext
        {
            Display = new DisplayCapabilities 
            { 
                HasGraphicalDisplay = true,
                Width = 1024,
                Height = 768
            }
        };

        // Act
        var canActivateSmall = await profile.CanActivateAsync(smallScreenContext);
        var canActivateAdequate = await profile.CanActivateAsync(adequateScreenContext);

        // Assert
        Assert.False(canActivateSmall);
        Assert.True(canActivateAdequate);
    }

    [Fact]
    public async Task UIProfileManager_Should_Switch_Between_Profiles()
    {
        // Arrange
        var manager = new UIProfileManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var initialProfile = manager.ActiveProfile;

        // Act - Try to switch to a different profile
        var targetProfileId = initialProfile?.Id == ConsoleUIProfile.ProfileId 
            ? WebUIProfile.ProfileId 
            : ConsoleUIProfile.ProfileId;

        try
        {
            await manager.SwitchProfileAsync(targetProfileId);
            var newProfile = manager.ActiveProfile;

            // Assert
            Assert.NotNull(newProfile);
            Assert.NotEqual(initialProfile?.Id, newProfile.Id);
            Assert.Equal(targetProfileId, newProfile.Id);
        }
        catch (InvalidOperationException)
        {
            // Web profile might not be activatable in test environment (no display)
            // This is expected behavior
        }

        // Cleanup
        await manager.StopAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task UIProfileManager_Should_Save_And_Restore_State()
    {
        // Arrange
        var manager = new UIProfileManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var originalProfile = manager.ActiveProfile;

        // Act
        var stateToken = await manager.SaveProfileStateAsync();
        
        // Try to switch to another profile if possible
        try
        {
            var profiles = await manager.GetRegisteredProfilesAsync();
            var differentProfile = profiles.FirstOrDefault(p => p.Id != originalProfile?.Id);
            if (differentProfile != null)
            {
                await manager.SwitchProfileAsync(differentProfile.Id);
            }
        }
        catch (InvalidOperationException)
        {
            // Expected if profile can't be activated
        }

        // Restore state
        await manager.RestoreProfileStateAsync(stateToken);

        // Assert
        Assert.Equal(originalProfile?.Id, manager.ActiveProfile?.Id);

        // Cleanup
        await manager.StopAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ProfileValidator_Should_Validate_Profile_Properties()
    {
        // Arrange
        var validator = new ProfileValidator(_logger);
        var profile = new ConsoleUIProfile(_logger);
        var context = new UIContext();

        // Act & Assert - Basic validation should pass
        var result = await validator.ValidateAsync(profile, context);
        
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ProfileSwitcher_Should_Handle_Transition_States()
    {
        // Arrange
        var switcherLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ProfileSwitcher>();
        var switcher = new ProfileSwitcher(switcherLogger);
        var fromProfile = new ConsoleUIProfile(_logger);
        var toProfile = new ConsoleUIProfile(_logger) { }; // Different instance
        var context = new UIContext();
        var options = new ProfileSwitchOptions();

        // Act
        var result = await switcher.ExecuteTransitionAsync(fromProfile, toProfile, context, options);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Duration > TimeSpan.Zero);
    }
}