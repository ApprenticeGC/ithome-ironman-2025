using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace GameConsole.UI.Services.Tests;

/// <summary>
/// Integration tests for UI Profile Service demonstrating its functionality.
/// </summary>
public class UIProfileServiceIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<UIProfileService> _logger;

    public UIProfileServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _logger = loggerFactory.CreateLogger<UIProfileService>();
    }

    [Fact]
    public async Task UIProfileService_Should_Initialize_With_Default_Profiles()
    {
        // Arrange
        var service = new UIProfileService(_logger);

        // Act
        await service.InitializeAsync();
        await service.StartAsync();

        // Assert
        Assert.True(service.IsRunning);

        var profiles = await service.GetProfilesAsync();
        var profileList = profiles.ToList();
        
        Assert.NotEmpty(profileList);
        Assert.True(profileList.Count >= 3); // Should have TUI, Unity, and Godot profiles
        
        // Check that specific default profiles exist
        var tuiProfile = profileList.FirstOrDefault(p => p.Id == "tui-default");
        var unityProfile = profileList.FirstOrDefault(p => p.Id == "unity-style");
        var godotProfile = profileList.FirstOrDefault(p => p.Id == "godot-style");
        
        Assert.NotNull(tuiProfile);
        Assert.NotNull(unityProfile);
        Assert.NotNull(godotProfile);

        // Cleanup
        await service.StopAsync();
        await service.DisposeAsync();
    }

    [Fact]
    public async Task UIProfileService_Should_Have_Active_Profile_After_Initialization()
    {
        // Arrange
        var service = new UIProfileService(_logger);

        // Act
        await service.InitializeAsync();
        await service.StartAsync();

        var activeProfile = await service.GetActiveProfileAsync();

        // Assert
        Assert.NotNull(activeProfile);
        Assert.True(activeProfile.IsActive);
        
        // Should default to TUI profile
        Assert.Equal("tui-default", activeProfile.Id);

        // Cleanup
        await service.StopAsync();
        await service.DisposeAsync();
    }

    [Fact]
    public async Task UIProfileService_Should_Switch_Active_Profiles()
    {
        // Arrange
        var service = new UIProfileService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act - Switch to Unity profile
        var switchResult = await service.ActivateProfileAsync("unity-style");
        var activeProfile = await service.GetActiveProfileAsync();

        // Assert
        Assert.True(switchResult);
        Assert.NotNull(activeProfile);
        Assert.Equal("unity-style", activeProfile.Id);
        Assert.Equal("Unity Style", activeProfile.Name);
        Assert.True(activeProfile.IsActive);

        // Verify old profile is no longer active
        var tuiProfile = await service.GetProfileByIdAsync("tui-default");
        Assert.NotNull(tuiProfile);
        Assert.False(tuiProfile.IsActive);

        // Cleanup
        await service.StopAsync();
        await service.DisposeAsync();
    }

    [Fact]
    public async Task UIProfileService_Should_Handle_Invalid_Profile_Activation()
    {
        // Arrange
        var service = new UIProfileService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act
        var switchResult = await service.ActivateProfileAsync("non-existent-profile");

        // Assert
        Assert.False(switchResult);

        // Active profile should remain unchanged
        var activeProfile = await service.GetActiveProfileAsync();
        Assert.NotNull(activeProfile);
        Assert.Equal("tui-default", activeProfile.Id); // Should still be default

        // Cleanup
        await service.StopAsync();
        await service.DisposeAsync();
    }

    [Fact]
    public async Task UIProfileService_Should_Provide_Capabilities()
    {
        // Arrange
        var service = new UIProfileService(_logger);
        await service.InitializeAsync();

        // Act
        var capabilities = await service.GetCapabilitiesAsync();
        var hasCapability = await service.HasCapabilityAsync<IUIProfileProvider>();
        var capability = await service.GetCapabilityAsync<IUIProfileProvider>();

        // Assert
        Assert.Contains(typeof(IUIProfileProvider), capabilities);
        Assert.True(hasCapability);
        Assert.NotNull(capability);
        Assert.Same(service, capability);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public async Task UIProfile_Should_Have_Expected_Properties()
    {
        // Arrange
        var service = new UIProfileService(_logger);
        await service.InitializeAsync();

        // Act
        var unityProfile = await service.GetProfileByIdAsync("unity-style");

        // Assert
        Assert.NotNull(unityProfile);
        Assert.Equal("unity-style", unityProfile.Id);
        Assert.Equal("Unity Style", unityProfile.Name);
        Assert.Equal("UI profile that simulates Unity engine behavior", unityProfile.Description);
        
        var settings = unityProfile.Settings;
        Assert.NotNull(settings);
        Assert.Equal("Unity", settings.RenderingMode);
        Assert.Equal("Unity", settings.InputMode);
        Assert.Equal("OpenGL", settings.GraphicsBackend);
        Assert.False(settings.TuiMode);
        Assert.Equal(50, settings.Priority);
        
        Assert.True(settings.CustomProperties.ContainsKey("EngineMode"));
        Assert.Equal("Unity", settings.CustomProperties["EngineMode"]);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public async Task UIProfileService_Should_Work_With_Configuration()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["UIProfiles:TestProfile:Name"] = "Test Profile",
            ["UIProfiles:TestProfile:Description"] = "A test profile"
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var service = new UIProfileService(_logger, configuration);

        // Act
        await service.InitializeAsync();

        // Assert
        // Configuration loading is not yet implemented but service should still initialize
        var profiles = await service.GetProfilesAsync();
        Assert.NotEmpty(profiles);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public void UIProfile_ToString_Should_Return_Expected_Format()
    {
        // Arrange
        var settings = new UIProfileSettings();
        var profile = new UIProfile("test-id", "Test Name", "Test Description", settings);

        // Act
        var result = profile.ToString();

        // Assert
        Assert.Equal("Test Name (test-id)", result);
    }
}