using GameConsole.UI.Configuration;
using System.Text.Json;

// Example demonstrating the UI Profile Configuration System

Console.WriteLine("=== UI Profile Configuration System Demo ===\n");

// 1. Create a base profile
var baseProfile = ProfileConfigurationBuilder.Create()
    .WithId("base-ui-profile")
    .WithName("Base UI Profile")
    .WithDescription("Base configuration for all UI profiles")
    .WithVersion(new Version(1, 0, 0))
    .ForEnvironment("Development")
    .AddJsonString("""
    {
        "UI": {
            "Theme": "Light",
            "FontSize": 12,
            "ShowToolbar": true
        },
        "Commands": {
            "DefaultTimeout": "00:00:30",
            "MaxRetries": 3
        },
        "Layout": {
            "DefaultView": "Console",
            "ShowStatusBar": true
        }
    }
    """)
    .WithMetadata("Author", "System")
    .WithMetadata("Category", "Base")
    .Build();

Console.WriteLine($"Created base profile: {baseProfile.Name} (v{baseProfile.Version})");
Console.WriteLine($"Base profile has {baseProfile.Configuration.GetChildren().Count()} top-level sections\n");

// 2. Create a derived profile that inherits from the base
var gameProfile = ProfileConfigurationBuilder.Create()
    .WithId("game-mode-profile")
    .WithName("Game Mode UI Profile")
    .WithDescription("Specialized UI configuration for game mode")
    .WithVersion(new Version(1, 1, 0))
    .ForEnvironment("Production")
    .InheritsFrom("base-ui-profile")
    .AddJsonString("""
    {
        "UI": {
            "Theme": "Dark",
            "ShowDebugPanel": false
        },
        "Commands": {
            "DefaultTimeout": "00:01:00"
        },
        "GameMode": {
            "EnableRuntimeDebugging": false,
            "PerformanceMonitoring": true
        }
    }
    """)
    .WithMetadata("Author", "Game Team")
    .WithMetadata("Category", "Game")
    .Build();

Console.WriteLine($"Created game profile: {gameProfile.Name} (v{gameProfile.Version})");
Console.WriteLine($"Parent: {gameProfile.ParentProfileId}\n");

// 3. Validate the configurations
var validator = new ConfigurationValidator();

var baseValidation = await validator.ValidateAsync(baseProfile);
Console.WriteLine($"Base profile validation: {(baseValidation.IsValid ? "✓ Valid" : "✗ Invalid")}");
if (baseValidation.Warnings.Any())
{
    Console.WriteLine($"Warnings: {string.Join(", ", baseValidation.Warnings)}");
}

var gameValidation = await validator.ValidateAsync(gameProfile);
Console.WriteLine($"Game profile validation: {(gameValidation.IsValid ? "✓ Valid" : "✗ Invalid")}");
if (gameValidation.Warnings.Any())
{
    Console.WriteLine($"Warnings: {string.Join(", ", gameValidation.Warnings)}");
}

// 4. Test compatibility between profiles
var compatibility = await validator.ValidateCompatibilityAsync(gameProfile, baseProfile);
Console.WriteLine($"Compatibility check: {(compatibility.IsValid ? "✓ Compatible" : "✗ Incompatible")}");
if (compatibility.Warnings.Any())
{
    Console.WriteLine($"Compatibility warnings: {string.Join(", ", compatibility.Warnings)}");
}

Console.WriteLine();

// 5. Demonstrate inheritance resolution
var inheritanceManager = new ProfileInheritanceManager();
inheritanceManager.RegisterProfile(baseProfile);
inheritanceManager.RegisterProfile(gameProfile);

Console.WriteLine("=== Inheritance Chain Resolution ===");
var chain = await inheritanceManager.GetInheritanceChainAsync("game-mode-profile");
Console.WriteLine($"Inheritance chain for {gameProfile.Name}:");
foreach (var profile in chain)
{
    Console.WriteLine($"  - {profile.Name} (ID: {profile.Id})");
}

// 6. Resolve the effective configuration
var resolvedConfig = await inheritanceManager.ResolveConfigurationAsync(gameProfile);
Console.WriteLine("\n=== Resolved Configuration ===");
Console.WriteLine($"Resolved profile: {resolvedConfig.Name}");

// Show some key configuration values
Console.WriteLine("Effective configuration values:");
Console.WriteLine($"  UI Theme: {resolvedConfig.Configuration["UI:Theme"]} (overridden by child)");
Console.WriteLine($"  UI FontSize: {resolvedConfig.Configuration["UI:FontSize"]} (inherited from parent)");
Console.WriteLine($"  UI ShowToolbar: {resolvedConfig.Configuration["UI:ShowToolbar"]} (inherited from parent)");
Console.WriteLine($"  Commands DefaultTimeout: {resolvedConfig.Configuration["Commands:DefaultTimeout"]} (overridden by child)");
Console.WriteLine($"  GameMode PerformanceMonitoring: {resolvedConfig.Configuration["GameMode:PerformanceMonitoring"]} (child-specific)");

// 7. Demonstrate type-safe configuration access
Console.WriteLine("\n=== Type-Safe Configuration Access ===");

var uiSettings = resolvedConfig.GetSection<UISettings>("UI");
Console.WriteLine($"UI Settings: Theme={uiSettings.Theme}, FontSize={uiSettings.FontSize}, ShowToolbar={uiSettings.ShowToolbar}");

var commandSettings = resolvedConfig.GetSection<CommandSettings>("Commands");
Console.WriteLine($"Command Settings: Timeout={commandSettings.DefaultTimeout}, Retries={commandSettings.MaxRetries}");

Console.WriteLine("\n=== Demo Complete ===");

// Example configuration classes for type-safe access
public class UISettings
{
    public string Theme { get; set; } = "Light";
    public int FontSize { get; set; } = 12;
    public bool ShowToolbar { get; set; } = true;
    public bool ShowDebugPanel { get; set; } = true;
}

public class CommandSettings
{
    public string DefaultTimeout { get; set; } = "00:00:30";
    public int MaxRetries { get; set; } = 3;
}

public class GameModeSettings
{
    public bool EnableRuntimeDebugging { get; set; } = true;
    public bool PerformanceMonitoring { get; set; } = false;
}