using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services.Tests;

/// <summary>
/// Example demonstrating UI Profile configuration system usage.
/// Shows how to initialize, query, and switch between different UI profiles.
/// </summary>
public class UIProfileUsageExample
{
    /// <summary>
    /// Demonstrates the complete UI Profile workflow.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public static async Task RunExampleAsync()
    {
        Console.WriteLine("=== UI Profile Configuration System Demo ===\n");

        // Create logger (in real application, this would come from DI)
        using var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<UIProfileService>();

        // Initialize the UI Profile Service
        var profileService = new UIProfileService(logger);
        
        try
        {
            // Initialize and start the service
            await profileService.InitializeAsync();
            await profileService.StartAsync();

            Console.WriteLine("1. Available UI Profiles:");
            var profiles = await profileService.GetProfilesAsync();
            foreach (var profile in profiles)
            {
                var status = profile.IsActive ? " (ACTIVE)" : "";
                Console.WriteLine($"   - {profile.Name} ({profile.Id}){status}");
                Console.WriteLine($"     Description: {profile.Description}");
                Console.WriteLine($"     Rendering Mode: {profile.Settings.RenderingMode}");
                Console.WriteLine($"     TUI Mode: {profile.Settings.TuiMode}");
                Console.WriteLine();
            }

            // Show current active profile
            Console.WriteLine("2. Current Active Profile:");
            var activeProfile = await profileService.GetActiveProfileAsync();
            Console.WriteLine($"   {activeProfile?.Name ?? "None"}\n");

            // Switch to Unity profile
            Console.WriteLine("3. Switching to Unity Style profile...");
            var switchResult = await profileService.ActivateProfileAsync("unity-style");
            if (switchResult)
            {
                var newActive = await profileService.GetActiveProfileAsync();
                Console.WriteLine($"   Successfully switched to: {newActive?.Name}");
                Console.WriteLine($"   Graphics Backend: {newActive?.Settings.GraphicsBackend}");
                Console.WriteLine($"   Input Mode: {newActive?.Settings.InputMode}");
                Console.WriteLine($"   Engine Mode: {newActive?.Settings.CustomProperties.GetValueOrDefault("EngineMode", "None")}");
            }
            else
            {
                Console.WriteLine("   Failed to switch profile");
            }

            Console.WriteLine();

            // Switch to Godot profile
            Console.WriteLine("4. Switching to Godot Style profile...");
            switchResult = await profileService.ActivateProfileAsync("godot-style");
            if (switchResult)
            {
                var newActive = await profileService.GetActiveProfileAsync();
                Console.WriteLine($"   Successfully switched to: {newActive?.Name}");
                Console.WriteLine($"   Graphics Backend: {newActive?.Settings.GraphicsBackend}");
                Console.WriteLine($"   Engine Mode: {newActive?.Settings.CustomProperties.GetValueOrDefault("EngineMode", "None")}");
            }

            Console.WriteLine();

            // Demonstrate capability discovery
            Console.WriteLine("5. Service Capabilities:");
            var capabilities = await profileService.GetCapabilitiesAsync();
            foreach (var capability in capabilities)
            {
                Console.WriteLine($"   - {capability.Name}");
            }

            var hasUIProfileCapability = await profileService.HasCapabilityAsync<IUIProfileProvider>();
            Console.WriteLine($"   Has IUIProfileProvider capability: {hasUIProfileCapability}");

            var uiProfileCapability = await profileService.GetCapabilityAsync<IUIProfileProvider>();
            Console.WriteLine($"   Can retrieve IUIProfileProvider: {uiProfileCapability != null}");

            Console.WriteLine();

            // Demonstrate error handling
            Console.WriteLine("6. Error Handling - Invalid Profile:");
            var invalidSwitch = await profileService.ActivateProfileAsync("invalid-profile");
            Console.WriteLine($"   Attempt to switch to invalid profile: {(invalidSwitch ? "Success" : "Failed (Expected)")}");

        }
        finally
        {
            // Clean up
            await profileService.StopAsync();
            await profileService.DisposeAsync();
        }

        Console.WriteLine("\n=== Demo Complete ===");
    }
}