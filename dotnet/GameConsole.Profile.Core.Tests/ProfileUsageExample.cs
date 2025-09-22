using GameConsole.Profile.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Profile.Core.Tests;

/// <summary>
/// Example demonstrating how to use the Profile Configuration System.
/// </summary>
public class ProfileUsageExample
{
    public static async Task RunExample()
    {
        // Create a service provider and logger
        var serviceProvider = new MockServiceProvider();
        var logger = new MockLogger<ProfileManager>();
        
        // Create the profile manager
        var profileManager = new ProfileManager(serviceProvider, logger);
        
        Console.WriteLine("=== GameConsole Profile Configuration System Demo ===");
        Console.WriteLine();
        
        // Initialize the profile manager (registers built-in profiles)
        Console.WriteLine("1. Initializing Profile Manager...");
        await profileManager.InitializeAsync();
        
        // List available profiles
        var availableProfiles = await profileManager.GetAvailableProfiles();
        Console.WriteLine($"Available Profiles: {availableProfiles.Count()}");
        foreach (var profile in availableProfiles.OrderByDescending(p => p.Priority))
        {
            Console.WriteLine($"  - {profile.DisplayName} (Priority: {profile.Priority})");
        }
        Console.WriteLine();
        
        // Start the manager (auto-activates best profile)
        Console.WriteLine("2. Starting Profile Manager (auto-activating best profile)...");
        await profileManager.StartAsync();
        Console.WriteLine($"Active Profile: {profileManager.ActiveProfile?.DisplayName}");
        Console.WriteLine();
        
        // Show configuration settings
        if (profileManager.ActiveProfile != null)
        {
            Console.WriteLine("3. Current Profile Configuration:");
            var settings = await profileManager.ActiveProfile.GetConfigurationSettings();
            foreach (var setting in settings)
            {
                Console.WriteLine($"  {setting.Key}: {setting.Value}");
            }
            Console.WriteLine();
        }
        
        // Switch to Unity profile
        Console.WriteLine("4. Switching to Unity profile...");
        var activated = await profileManager.ActivateProfile("unity");
        Console.WriteLine($"Unity profile activated: {activated}");
        Console.WriteLine($"New Active Profile: {profileManager.ActiveProfile?.DisplayName}");
        Console.WriteLine();
        
        // Show Unity configuration
        if (profileManager.ActiveProfile != null)
        {
            Console.WriteLine("5. Unity Profile Configuration:");
            var unitySettings = await profileManager.ActiveProfile.GetConfigurationSettings();
            foreach (var setting in unitySettings)
            {
                Console.WriteLine($"  {setting.Key}: {setting.Value}");
            }
            Console.WriteLine();
        }
        
        // List supported profiles
        Console.WriteLine("6. Supported Profiles (ordered by priority):");
        var supportedProfiles = await profileManager.GetSupportedProfiles();
        foreach (var profile in supportedProfiles)
        {
            Console.WriteLine($"  - {profile.DisplayName} (Priority: {profile.Priority})");
        }
        Console.WriteLine();
        
        // Clean shutdown
        Console.WriteLine("7. Shutting down Profile Manager...");
        await profileManager.StopAsync();
        await profileManager.DisposeAsync();
        Console.WriteLine("Profile Manager stopped and disposed.");
    }
}