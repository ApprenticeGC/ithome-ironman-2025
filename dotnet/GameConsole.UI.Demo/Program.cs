using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Demonstration program showing the UI Profile Configuration System in action.
/// This shows how to create, manage, and switch between UI profiles.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<UIProfileConfigurationService>();
        
        // Setup configuration
        var configuration = new ConfigurationBuilder().Build();
        
        // Create the service
        var profileService = new UIProfileConfigurationService(logger, configuration);
        
        try
        {
            Console.WriteLine("=== UI Profile Configuration System Demo ===");
            Console.WriteLine();
            
            // Initialize and start the service
            await profileService.InitializeAsync();
            await profileService.StartAsync();
            
            // Show built-in profiles
            Console.WriteLine("1. Built-in profiles loaded:");
            var allProfiles = await profileService.GetAllProfilesAsync();
            foreach (var profile in allProfiles)
            {
                Console.WriteLine($"   - {profile.Id}: {profile.Name} (Mode: {profile.Mode})");
                if (profile.IsBuiltIn)
                    Console.WriteLine($"     Built-in profile");
            }
            Console.WriteLine();
            
            // Show active profile
            var activeProfile = await profileService.GetActiveProfileAsync();
            if (activeProfile != null)
            {
                Console.WriteLine($"2. Current active profile: {activeProfile.Name} ({activeProfile.Mode})");
                Console.WriteLine($"   Theme: {activeProfile.Settings.Theme.ColorScheme}, Font: {activeProfile.Settings.Theme.FontFamily} {activeProfile.Settings.Theme.FontSize}px");
                Console.WriteLine($"   Layout: {activeProfile.Settings.Layout.Width}x{activeProfile.Settings.Layout.Height}");
                Console.WriteLine();
            }
            
            // Create a custom profile
            Console.WriteLine("3. Creating custom gaming profile...");
            var gamingProfile = new UIProfile
            {
                Id = "gaming-optimized",
                Name = "Gaming Optimized",
                Description = "High-performance profile optimized for gaming",
                Mode = "GUI",
                Settings = new UIProfileSettings
                {
                    Theme = new UIThemeSettings
                    {
                        ColorScheme = "Gaming",
                        FontFamily = "Courier New", 
                        FontSize = 16,
                        DarkMode = true
                    },
                    Layout = new UILayoutSettings
                    {
                        Width = "1920px",
                        Height = "1080px",
                        Padding = 4,
                        ShowBorders = false
                    },
                    Input = new UIInputSettings
                    {
                        PreferredInput = "Gamepad",
                        KeyboardShortcuts = true,
                        MouseEnabled = true
                    },
                    Rendering = new UIRenderingSettings
                    {
                        MaxFPS = 120,
                        UseHardwareAcceleration = true,
                        Quality = "High"
                    }
                }
            };
            
            var created = await profileService.CreateProfileAsync(gamingProfile);
            Console.WriteLine($"   Created: {created}");
            Console.WriteLine();
            
            // Show all profiles again
            Console.WriteLine("4. All profiles after creating custom profile:");
            allProfiles = await profileService.GetAllProfilesAsync();
            foreach (var profile in allProfiles)
            {
                var marker = profile.IsActive ? " [ACTIVE]" : "";
                Console.WriteLine($"   - {profile.Id}: {profile.Name} (Mode: {profile.Mode}){marker}");
            }
            Console.WriteLine();
            
            // Switch to the gaming profile
            Console.WriteLine("5. Switching to gaming profile...");
            var activated = await profileService.ActivateProfileAsync("gaming-optimized");
            Console.WriteLine($"   Activated: {activated}");
            
            activeProfile = await profileService.GetActiveProfileAsync();
            if (activeProfile != null)
            {
                Console.WriteLine($"   New active profile: {activeProfile.Name}");
                Console.WriteLine($"   Settings: {activeProfile.Settings.Rendering.MaxFPS}fps, {activeProfile.Settings.Rendering.Quality} quality");
            }
            Console.WriteLine();
            
            // Test validation
            Console.WriteLine("6. Testing profile validation...");
            var invalidProfile = new UIProfile
            {
                Id = "", // Invalid
                Name = "", // Invalid  
                Mode = "INVALID_MODE",
                Settings = new UIProfileSettings
                {
                    Theme = new UIThemeSettings { FontSize = 2 }, // Too small
                    Rendering = new UIRenderingSettings { MaxFPS = 500 } // Too high
                }
            };
            
            var validation = await profileService.ValidateProfileAsync(invalidProfile);
            Console.WriteLine($"   Valid: {validation.IsValid}");
            if (validation.Errors.Any())
            {
                Console.WriteLine("   Errors:");
                foreach (var error in validation.Errors)
                    Console.WriteLine($"     - {error}");
            }
            if (validation.Warnings.Any())
            {
                Console.WriteLine("   Warnings:");
                foreach (var warning in validation.Warnings)
                    Console.WriteLine($"     - {warning}");
            }
            Console.WriteLine();
            
            // Show supported modes
            Console.WriteLine("7. Supported UI modes:");
            var supportedModes = await profileService.GetSupportedModesAsync();
            foreach (var mode in supportedModes)
            {
                Console.WriteLine($"   - {mode}");
            }
            Console.WriteLine();
            
            // Test capability provider interface
            Console.WriteLine("8. Testing capability provider interface...");
            var capabilities = await profileService.GetCapabilitiesAsync();
            Console.WriteLine($"   Capabilities provided: {string.Join(", ", capabilities.Select(c => c.Name))}");
            
            var hasProfileCapability = await profileService.HasCapabilityAsync<IUIProfileCapability>();
            Console.WriteLine($"   Has IUIProfileCapability: {hasProfileCapability}");
            
            var profileCapability = await profileService.GetCapabilityAsync<IUIProfileCapability>();
            Console.WriteLine($"   Retrieved capability: {profileCapability != null}");
            Console.WriteLine();
            
            Console.WriteLine("=== Demo completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during demo: {ex.Message}");
        }
        finally
        {
            await profileService.StopAsync();
            await profileService.DisposeAsync();
        }
    }
}
