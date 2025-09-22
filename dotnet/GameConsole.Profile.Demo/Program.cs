using GameConsole.Profile.Core;
using GameConsole.Profile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameConsole.Profile.Demo;

/// <summary>
/// Demo console application showing the Profile Management System in action.
/// This demonstrates the TUI-like functionality for profile management.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🎮 GameConsole Profile Management System Demo");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        // Set up dependency injection and logging
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IProfileProvider, MemoryProfileProvider>();
                services.AddSingleton<IProfileManager, ProfileManager>();
                services.AddSingleton<IProfileConfiguration, ProfileConfigurationService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        var profileManager = host.Services.GetRequiredService<IProfileManager>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Initialize the profile manager
            await profileManager.InitializeAsync();
            await profileManager.StartAsync();

            // Demo the profile system
            await RunProfileSystemDemo(profileManager, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the demo");
        }
        finally
        {
            await profileManager.StopAsync();
            await profileManager.DisposeAsync();
            await host.StopAsync();
        }

        Console.WriteLine();
        Console.WriteLine("Demo completed. Press any key to exit...");
        Console.ReadKey();
    }

    static async Task RunProfileSystemDemo(IProfileManager profileManager, ILogger logger)
    {
        Console.WriteLine("📋 Available Profiles:");
        Console.WriteLine("=====================");

        // List all available profiles
        var profiles = await profileManager.GetAllProfilesAsync();
        foreach (var profile in profiles)
        {
            var activeIndicator = "";
            try
            {
                var activeProfile = await profileManager.GetActiveProfileAsync();
                if (activeProfile.Id == profile.Id)
                    activeIndicator = " [ACTIVE]";
            }
            catch { }

            Console.WriteLine($"  • {profile.Name} ({profile.Type}){activeIndicator}");
            Console.WriteLine($"    ID: {profile.Id}");
            Console.WriteLine($"    Description: {profile.Description}");
            Console.WriteLine($"    Services: {profile.ServiceConfigurations.Count}");
            Console.WriteLine($"    Read-Only: {profile.IsReadOnly}");
            Console.WriteLine();
        }

        Console.WriteLine("🔄 Profile Switching Demo:");
        Console.WriteLine("==========================");

        // Demo switching between profiles
        var profileTypes = new[] { "unity-style", "godot-style", "minimal", "default" };
        
        foreach (var profileId in profileTypes)
        {
            Console.WriteLine($"Switching to profile: {profileId}");
            
            try
            {
                await profileManager.SetActiveProfileAsync(profileId);
                var currentProfile = await profileManager.GetActiveProfileAsync();
                
                Console.WriteLine($"✅ Active Profile: {currentProfile.Name}");
                Console.WriteLine($"   Type: {currentProfile.Type}");
                Console.WriteLine($"   Services Configured: {currentProfile.ServiceConfigurations.Count}");
                
                // Show some service configurations
                if (currentProfile.ServiceConfigurations.Any())
                {
                    Console.WriteLine("   Service Configurations:");
                    foreach (var config in currentProfile.ServiceConfigurations.Take(2))
                    {
                        Console.WriteLine($"     • {config.Key} -> {config.Value.Implementation}");
                        Console.WriteLine($"       Capabilities: [{string.Join(", ", config.Value.Capabilities)}]");
                        Console.WriteLine($"       Settings: {config.Value.Settings.Count} items");
                    }
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to switch to {profileId}: {ex.Message}");
            }
        }

        Console.WriteLine("🆕 Custom Profile Creation Demo:");
        Console.WriteLine("================================");

        try
        {
            // Create a custom profile
            var customProfile = await profileManager.CreateProfileAsync(
                "My Custom Gaming Setup", 
                ProfileType.Custom, 
                "A custom profile tailored for my specific gaming needs");

            Console.WriteLine($"✅ Created custom profile: {customProfile.Name}");
            Console.WriteLine($"   ID: {customProfile.Id}");
            Console.WriteLine($"   Type: {customProfile.Type}");
            
            // Demonstrate modifying the custom profile
            if (customProfile is GameConsole.Profile.Core.Profile mutableProfile)
            {
                mutableProfile.SetServiceConfiguration("ICustomInputService", new ServiceConfiguration
                {
                    Implementation = "MyCustomInputService",
                    Capabilities = new List<string> { "CustomKeyMapping", "MacroSupport" },
                    Settings = new Dictionary<string, object>
                    {
                        { "macroEnabled", true },
                        { "customSensitivity", 1.5 }
                    },
                    Enabled = true
                });

                await profileManager.UpdateProfileAsync(mutableProfile);
                Console.WriteLine("✅ Added custom service configuration");
            }
            
            // Switch to the custom profile
            await profileManager.SetActiveProfileAsync(customProfile.Id);
            Console.WriteLine("✅ Switched to custom profile");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to create custom profile: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("📊 Profile System Statistics:");
        Console.WriteLine("=============================");
        
        var allProfiles = await profileManager.GetAllProfilesAsync();
        var profilesByType = allProfiles.GroupBy(p => p.Type).ToList();
        
        Console.WriteLine($"Total Profiles: {allProfiles.Count()}");
        foreach (var group in profilesByType)
        {
            Console.WriteLine($"  {group.Key}: {group.Count()}");
        }

        var currentActiveProfile = await profileManager.GetActiveProfileAsync();
        Console.WriteLine($"Currently Active: {currentActiveProfile.Name} ({currentActiveProfile.Type})");
        
        Console.WriteLine();
        Console.WriteLine("🎯 Profile Validation Demo:");
        Console.WriteLine("===========================");
        
        // Test validation
        var validProfile = allProfiles.First();
        var isValid = await profileManager.ValidateProfileAsync(validProfile);
        Console.WriteLine($"Profile '{validProfile.Name}' validation: {(isValid ? "✅ VALID" : "❌ INVALID")}");

        // Test with invalid profile
        var invalidProfile = new GameConsole.Profile.Core.Profile("", "", ProfileType.Custom); // Invalid - empty ID and name
        var isInvalid = await profileManager.ValidateProfileAsync(invalidProfile);
        Console.WriteLine($"Invalid profile validation: {(isInvalid ? "✅ VALID" : "❌ INVALID (as expected)")}");
    }
}
