using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using GameConsole.Input.Services;
using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.UI.Services.Tests;

/// <summary>
/// Integration tests that demonstrate how UI Profiles coordinate with Input system profiles.
/// </summary>
public class UIProfileIntegrationTests
{
    private readonly ILogger<UIProfileService> _uiLogger;
    private readonly ILogger<InputMappingService> _inputLogger;
    private readonly IConfiguration _configuration;

    public UIProfileIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _uiLogger = loggerFactory.CreateLogger<UIProfileService>();
        _inputLogger = loggerFactory.CreateLogger<InputMappingService>();
        
        var configBuilder = new ConfigurationBuilder();
        _configuration = configBuilder.Build();
    }

    [Fact]
    public async Task UIProfileSystem_IntegratesWithInputProfiles()
    {
        // Arrange
        var uiProfileService = new UIProfileService(_uiLogger, _configuration);
        var inputMappingService = new InputMappingService(_inputLogger);
        
        await uiProfileService.InitializeAsync();
        await inputMappingService.InitializeAsync();
        
        await uiProfileService.StartAsync();
        await inputMappingService.StartAsync();

        // Create corresponding input profiles for our UI profiles
        var unityInputProfile = await inputMappingService.CreateProfileAsync("unity", cancellationToken: default);
        unityInputProfile.SetMapping("Ctrl+N", "NewScene");
        unityInputProfile.SetMapping("Ctrl+S", "SaveScene");
        unityInputProfile.SetMapping("F", "FrameSelected");
        
        var godotInputProfile = await inputMappingService.CreateProfileAsync("godot", cancellationToken: default);
        godotInputProfile.SetMapping("Ctrl+N", "NewScene");
        godotInputProfile.SetMapping("Ctrl+S", "SaveScene");
        godotInputProfile.SetMapping("F", "FocusSelected");
        godotInputProfile.SetMapping("G", "MoveGizmo");

        // Act & Assert - Test Unity-style profile coordination
        await uiProfileService.ActivateProfileAsync("unity-style");
        var activeUIProfile = uiProfileService.ActiveProfile!;
        
        Assert.Equal("unity-style", activeUIProfile.Id);
        Assert.Equal("unity", activeUIProfile.InputProfileName);
        Assert.Equal("Dark", activeUIProfile.UISettings["Theme"]);
        Assert.Equal(true, activeUIProfile.UISettings["ShowInspector"]);
        
        // Simulate coordinated profile switching by manually switching input profile
        await inputMappingService.SwitchProfileAsync("unity");
        var activeInputProfile = await inputMappingService.GetMappingConfigurationAsync();
        Assert.Equal("unity", activeInputProfile.ProfileName);
        Assert.Equal("FrameSelected", activeInputProfile.GetMapping("F"));

        // Act & Assert - Test Godot-style profile coordination
        await uiProfileService.ActivateProfileAsync("godot-style");
        activeUIProfile = uiProfileService.ActiveProfile!;
        
        Assert.Equal("godot-style", activeUIProfile.Id);
        Assert.Equal("godot", activeUIProfile.InputProfileName);
        Assert.Equal("Light", activeUIProfile.UISettings["Theme"]);
        Assert.Equal(true, activeUIProfile.UISettings["ShowFileSystem"]);
        
        // Simulate coordinated profile switching by manually switching input profile
        await inputMappingService.SwitchProfileAsync("godot");
        activeInputProfile = await inputMappingService.GetMappingConfigurationAsync();
        Assert.Equal("godot", activeInputProfile.ProfileName);
        Assert.Equal("FocusSelected", activeInputProfile.GetMapping("F"));
        Assert.Equal("MoveGizmo", activeInputProfile.GetMapping("G"));
    }

    [Fact]
    public async Task UIProfile_SupportsCustomEngineSimulation()
    {
        // Arrange
        var uiProfileService = new UIProfileService(_uiLogger, _configuration);
        var inputMappingService = new InputMappingService(_inputLogger);
        
        await uiProfileService.InitializeAsync();
        await inputMappingService.InitializeAsync();

        // Create a custom engine simulation profile (e.g., Unreal-style)
        var customInputProfile = await inputMappingService.CreateProfileAsync("unreal", cancellationToken: default);
        customInputProfile.SetMapping("Ctrl+N", "NewLevel");
        customInputProfile.SetMapping("Ctrl+S", "SaveLevel");
        customInputProfile.SetMapping("Alt+LMB", "OrbitCamera");
        customInputProfile.SetMapping("W", "MoveForward");

        var customUIProfile = await uiProfileService.CreateProfileAsync(
            "unreal-style",
            "Unreal Engine Style",
            "UI profile mimicking Unreal Engine Editor behavior",
            "unreal",
            new Dictionary<string, object>
            {
                { "WindowMode", "Maximized" },
                { "Resolution", "1920x1080" },
                { "GizmoStyle", "Unreal" },
                { "GridSnapping", true }
            },
            new Dictionary<string, object>
            {
                { "Theme", "BlueDark" },
                { "Layout", "Unreal" },
                { "ShowContentBrowser", true },
                { "ShowWorldOutliner", true },
                { "ShowDetails", true },
                { "TabsOnSides", true }
            });

        // Act
        await uiProfileService.ActivateProfileAsync("unreal-style");
        await inputMappingService.SwitchProfileAsync("unreal");

        // Assert
        var activeUIProfile = uiProfileService.ActiveProfile!;
        Assert.Equal("unreal-style", activeUIProfile.Id);
        Assert.Equal("unreal", activeUIProfile.InputProfileName);
        Assert.Equal("BlueDark", activeUIProfile.UISettings["Theme"]);
        Assert.Equal(true, activeUIProfile.UISettings["ShowContentBrowser"]);
        Assert.Equal("Unreal", activeUIProfile.GraphicsSettings["GizmoStyle"]);
        Assert.Equal(true, activeUIProfile.GraphicsSettings["GridSnapping"]);

        var activeInputProfile = await inputMappingService.GetMappingConfigurationAsync();
        Assert.Equal("NewLevel", activeInputProfile.GetMapping("Ctrl+N"));
        Assert.Equal("OrbitCamera", activeInputProfile.GetMapping("Alt+LMB"));
    }

    [Fact]
    public async Task UIProfileEvents_NotifyOfProfileChanges()
    {
        // Arrange
        var uiProfileService = new UIProfileService(_uiLogger, _configuration);
        await uiProfileService.InitializeAsync();

        var eventsFired = new List<(IUIProfile? Previous, IUIProfile? New)>();
        uiProfileService.ActiveProfileChanged += (sender, args) =>
        {
            eventsFired.Add((args.PreviousProfile, args.NewProfile));
        };

        // Act
        await uiProfileService.ActivateProfileAsync("unity-style");
        await uiProfileService.ActivateProfileAsync("godot-style");

        // Assert
        Assert.Equal(2, eventsFired.Count);
        
        // First event: default -> unity-style
        Assert.Equal("default", eventsFired[0].Previous!.Id);
        Assert.Equal("unity-style", eventsFired[0].New!.Id);
        
        // Second event: unity-style -> godot-style  
        Assert.Equal("unity-style", eventsFired[1].Previous!.Id);
        Assert.Equal("godot-style", eventsFired[1].New!.Id);
    }

    [Fact]
    public async Task UIProfile_CanBePersistedAndRestored()
    {
        // Arrange
        var uiProfileService = new UIProfileService(_uiLogger, _configuration);
        await uiProfileService.InitializeAsync();

        // Create a custom profile with specific settings
        var originalProfile = await uiProfileService.CreateProfileAsync(
            "persistent-test",
            "Persistent Test Profile", 
            "A profile to test persistence",
            "test-input",
            new Dictionary<string, object>
            {
                { "Resolution", "2560x1440" },
                { "RefreshRate", 144 }
            },
            new Dictionary<string, object>
            {
                { "Theme", "CustomTheme" },
                { "Layout", "CustomLayout" }
            });

        // Act - Simulate persistence by cloning the profile
        var restoredProfile = ((UIProfile)originalProfile).Clone("persistent-test-copy", "Restored Profile");

        // Assert - Verify all settings are preserved
        Assert.Equal("persistent-test-copy", restoredProfile.Id);
        Assert.Equal("Restored Profile", restoredProfile.Name);
        Assert.Equal("A profile to test persistence", restoredProfile.Description);
        Assert.Equal("test-input", restoredProfile.InputProfileName);
        Assert.Equal("2560x1440", restoredProfile.GraphicsSettings["Resolution"]);
        Assert.Equal(144, restoredProfile.GraphicsSettings["RefreshRate"]);
        Assert.Equal("CustomTheme", restoredProfile.UISettings["Theme"]);
        Assert.Equal("CustomLayout", restoredProfile.UISettings["Layout"]);
    }
}