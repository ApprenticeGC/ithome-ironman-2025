using GameConsole.Core.Abstractions;
using GameConsole.Input.Services;
using GameConsole.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Demo;

/// <summary>
/// Demo application showcasing the UI Profile Configuration System.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var uiLogger = loggerFactory.CreateLogger<UIProfileService>();
        var inputLogger = loggerFactory.CreateLogger<InputMappingService>();
        
        // Setup configuration
        var configuration = new ConfigurationBuilder().Build();

        // Initialize services
        var uiProfileService = new UIProfileService(uiLogger, configuration);
        var inputMappingService = new InputMappingService(inputLogger);

        Console.WriteLine("=== GameConsole UI Profile Configuration System Demo ===\n");

        try
        {
            // Initialize both services
            await uiProfileService.InitializeAsync();
            await inputMappingService.InitializeAsync();
            
            await uiProfileService.StartAsync();
            await inputMappingService.StartAsync();

            // Setup event handlers
            uiProfileService.ActiveProfileChanged += (sender, args) =>
            {
                Console.WriteLine($"🎯 Profile Changed: {args.PreviousProfile?.Name} → {args.NewProfile?.Name}");
            };

            // Demo 1: Show all available profiles
            Console.WriteLine("📋 Available UI Profiles:");
            var profiles = await uiProfileService.GetAllProfilesAsync();
            foreach (var profile in profiles)
            {
                Console.WriteLine($"  • {profile.Name} ({profile.Id})");
                Console.WriteLine($"    Description: {profile.Description}");
                Console.WriteLine($"    Input Profile: {profile.InputProfileName ?? "None"}");
                Console.WriteLine($"    Theme: {profile.UISettings.GetValueOrDefault("Theme", "Default")}");
                Console.WriteLine();
            }

            // Demo 2: Show current active profile
            Console.WriteLine($"🎮 Currently Active Profile: {uiProfileService.ActiveProfile?.Name}");
            DisplayProfileDetails(uiProfileService.ActiveProfile!);

            // Demo 3: Create corresponding input profiles
            Console.WriteLine("\n⚙️  Setting up coordinated input profiles...");
            
            var unityInputProfile = await inputMappingService.CreateProfileAsync("unity");
            unityInputProfile.SetMapping("Ctrl+N", "NewScene");
            unityInputProfile.SetMapping("Ctrl+S", "SaveScene");
            unityInputProfile.SetMapping("F", "FrameSelected");
            unityInputProfile.SetMapping("W", "MoveForward");
            
            var godotInputProfile = await inputMappingService.CreateProfileAsync("godot");
            godotInputProfile.SetMapping("Ctrl+N", "NewScene");
            godotInputProfile.SetMapping("Ctrl+S", "SaveScene");
            godotInputProfile.SetMapping("F", "FocusSelected");
            godotInputProfile.SetMapping("G", "MoveGizmo");

            // Demo 4: Switch to Unity-style profile
            Console.WriteLine("\n🔄 Switching to Unity-style profile...");
            await uiProfileService.ActivateProfileAsync("unity-style");
            await inputMappingService.SwitchProfileAsync("unity");
            
            DisplayProfileDetails(uiProfileService.ActiveProfile!);
            await DisplayInputMappings(inputMappingService, "Unity");

            // Demo 5: Switch to Godot-style profile
            Console.WriteLine("\n🔄 Switching to Godot-style profile...");
            await uiProfileService.ActivateProfileAsync("godot-style");
            await inputMappingService.SwitchProfileAsync("godot");
            
            DisplayProfileDetails(uiProfileService.ActiveProfile!);
            await DisplayInputMappings(inputMappingService, "Godot");

            // Demo 6: Create custom profile
            Console.WriteLine("\n✨ Creating custom 'Game Developer' profile...");
            var customProfile = await uiProfileService.CreateProfileAsync(
                "game-dev-custom",
                "Game Developer Custom",
                "A custom profile optimized for game development workflow",
                "unity", // Use unity input profile
                new Dictionary<string, object>
                {
                    { "Resolution", "3440x1440" },
                    { "RefreshRate", 120 },
                    { "MultiMonitor", true }
                },
                new Dictionary<string, object>
                {
                    { "Theme", "VSCode Dark" },
                    { "Layout", "Dual Panel" },
                    { "ShowConsole", true },
                    { "ShowProfiler", true },
                    { "AutoSave", true }
                });

            await uiProfileService.ActivateProfileAsync("game-dev-custom");
            DisplayProfileDetails(customProfile);

            // Demo 7: Update profile settings
            Console.WriteLine("\n🔧 Updating custom profile settings...");
            await uiProfileService.UpdateProfileAsync(
                "game-dev-custom",
                uiSettings: new Dictionary<string, object>
                {
                    { "Theme", "High Contrast" },
                    { "FontSize", 14 },
                    { "ShowMinimap", false }
                });
            
            var updatedProfile = await uiProfileService.GetProfileAsync("game-dev-custom");
            DisplayProfileDetails(updatedProfile!);

            Console.WriteLine("\n✅ Demo completed successfully!");
            Console.WriteLine("\n💡 Key Features Demonstrated:");
            Console.WriteLine("  • Default profile creation (Unity-style, Godot-style)");
            Console.WriteLine("  • Profile activation with event notifications");
            Console.WriteLine("  • Coordination with Input system profiles");
            Console.WriteLine("  • Custom profile creation and modification");
            Console.WriteLine("  • Graphics and UI settings management");
            Console.WriteLine("  • Engine simulation mode switching");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        finally
        {
            // Cleanup
            await uiProfileService.StopAsync();
            await inputMappingService.StopAsync();
            await uiProfileService.DisposeAsync();
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void DisplayProfileDetails(IUIProfile profile)
    {
        Console.WriteLine($"📊 Profile Details:");
        Console.WriteLine($"  Name: {profile.Name}");
        Console.WriteLine($"  Description: {profile.Description}");
        Console.WriteLine($"  Input Profile: {profile.InputProfileName ?? "None"}");
        Console.WriteLine($"  Created: {profile.CreatedAt:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"  Modified: {profile.LastModified:yyyy-MM-dd HH:mm}");
        
        Console.WriteLine("  Graphics Settings:");
        foreach (var setting in profile.GraphicsSettings)
        {
            Console.WriteLine($"    {setting.Key}: {setting.Value}");
        }
        
        Console.WriteLine("  UI Settings:");
        foreach (var setting in profile.UISettings)
        {
            Console.WriteLine($"    {setting.Key}: {setting.Value}");
        }
        Console.WriteLine();
    }

    private static async Task DisplayInputMappings(InputMappingService inputService, string profileName)
    {
        var config = await inputService.GetMappingConfigurationAsync();
        Console.WriteLine($"⌨️  {profileName} Input Mappings:");
        
        var sampleKeys = new[] { "Ctrl+N", "Ctrl+S", "F", "G", "W" };
        foreach (var key in sampleKeys)
        {
            var mapping = config.GetMapping(key);
            if (mapping != null)
            {
                Console.WriteLine($"    {key} → {mapping}");
            }
        }
        Console.WriteLine();
    }
}