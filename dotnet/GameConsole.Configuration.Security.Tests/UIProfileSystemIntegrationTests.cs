using GameConsole.Configuration.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Configuration.Security.Tests;

/// <summary>
/// Integration tests demonstrating the UI Profile Configuration System usage.
/// </summary>
public class UIProfileSystemIntegrationTests : IDisposable
{
    private readonly UIProfileService _service;

    public UIProfileSystemIntegrationTests()
    {
        var logger = new TestLogger<UIProfileService>();
        var configuration = new ConfigurationBuilder().Build();
        _service = new UIProfileService(logger, configuration);
    }

    [Fact]
    public async Task UIProfileSystem_FullWorkflow_WorksCorrectly()
    {
        // Initialize the service - this registers default profiles
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Verify default profiles are available
        Assert.True(_service.AvailableProfiles.Count >= 3, $"Expected at least 3 profiles, got {_service.AvailableProfiles.Count}");
        Assert.NotNull(_service.ActiveProfile);
        Assert.Equal("tui-default", _service.ActiveProfile!.ProfileId);

        // Test profile switching
        await _service.SwitchToProfileAsync("unity-simulation");
        Assert.Equal("unity-simulation", _service.ActiveProfile!.ProfileId);
        Assert.Equal(UIMode.Unity, _service.ActiveProfile.Mode);

        // Test getting profile settings
        var unityProfile = _service.GetProfile("unity-simulation");
        Assert.NotNull(unityProfile);
        Assert.Equal(1920, unityProfile.Settings["WindowWidth"]);
        Assert.Equal(1080, unityProfile.Settings["WindowHeight"]);

        // Test custom profile registration
        var customProfile = new UIProfileConfiguration(
            "my-custom-profile",
            "My Custom Profile",
            "A custom UI profile for testing",
            UIMode.Custom);
        customProfile.SetSetting("CustomSetting", "CustomValue");

        _service.RegisterProfile(customProfile);
        Assert.Equal(4, _service.AvailableProfiles.Count);

        // Switch to custom profile
        await _service.SwitchToProfileAsync("my-custom-profile");
        Assert.Equal("my-custom-profile", _service.ActiveProfile!.ProfileId);
        Assert.Equal(UIMode.Custom, _service.ActiveProfile.Mode);

        // Verify custom setting
        var customProfileResult = _service.GetProfile("my-custom-profile") as UIProfileConfiguration;
        Assert.NotNull(customProfileResult);
        Assert.Equal("CustomValue", customProfileResult.GetSetting<string>("CustomSetting"));

        // Test profile change event
        UIProfileChangedEventArgs? eventArgs = null;
        _service.ProfileChanged += (sender, args) => eventArgs = args;
        
        await _service.SwitchToProfileAsync("godot-simulation");
        
        Assert.NotNull(eventArgs);
        Assert.Equal("my-custom-profile", eventArgs.PreviousProfile?.ProfileId);
        Assert.Equal("godot-simulation", eventArgs.NewProfile?.ProfileId);

        // Test Godot profile settings
        var godotProfile = _service.GetProfile("godot-simulation");
        Assert.NotNull(godotProfile);
        Assert.Equal(1280, godotProfile.Settings["WindowWidth"]);
        Assert.Equal(720, godotProfile.Settings["WindowHeight"]);
        Assert.Equal(false, godotProfile.Settings["Fullscreen"]);

        await _service.StopAsync();
    }

    [Fact]
    public void UIProfileConfiguration_ProviderMapping_WorksCorrectly()
    {
        // Test provider mapping functionality
        var profile = new UIProfileConfiguration(
            "provider-test-profile",
            "Provider Test Profile",
            "Profile for testing provider mappings",
            UIMode.Custom);

        // Map some example capability interfaces to provider types
        profile.MapProvider(typeof(IDisposable), typeof(object));
        profile.MapProvider(typeof(IComparable), typeof(string));

        Assert.Equal(2, profile.ProviderMappings.Count);
        Assert.Equal(typeof(object), profile.GetProviderType(typeof(IDisposable)));
        Assert.Equal(typeof(string), profile.GetProviderType(typeof(IComparable)));
        Assert.Null(profile.GetProviderType(typeof(ICloneable)));

        // Test removing provider mappings
        Assert.True(profile.RemoveProviderMapping(typeof(IDisposable)));
        Assert.False(profile.RemoveProviderMapping(typeof(IDisposable))); // Already removed
        Assert.Equal(1, profile.ProviderMappings.Count);
    }

    [Fact]
    public void UIProfileSystem_ProfileModes_AreCorrect()
    {
        // Verify that the UI modes are correctly defined
        Assert.Equal(0, (int)UIMode.TUI);
        Assert.Equal(1, (int)UIMode.Unity);
        Assert.Equal(2, (int)UIMode.Godot);
        Assert.Equal(99, (int)UIMode.Custom);
    }

    public void Dispose()
    {
        _service?.DisposeAsync().AsTask().Wait();
    }
}