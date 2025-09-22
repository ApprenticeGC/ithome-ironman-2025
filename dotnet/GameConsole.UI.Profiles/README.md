# GameConsole.UI.Profiles Usage Example

This example demonstrates how to use the Mode-Based UI Profile System implemented in RFC-011-01.

## Basic Usage

```csharp
using GameConsole.UI.Profiles;

// Create the profile management system
var validator = new ProfileValidator();
var manager = new UIProfileManager(validator);
var switcher = new ProfileSwitcher(manager);

// Create profiles for different modes
var gameProfile = new GameModeProfile();
var editorProfile = new EditorModeProfile();
var webProfile = new WebModeProfile();

// Register profiles with the manager
manager.RegisterProfile(gameProfile);
manager.RegisterProfile(editorProfile);
manager.RegisterProfile(webProfile);

// Initialize and start the manager
await manager.InitializeAsync();
await manager.StartAsync();

// Create UI context for different modes
var gameContext = new UIContext(ConsoleMode.Game, serviceProvider);
var webContext = new UIContext(ConsoleMode.Web, serviceProvider);

// Switch between profiles
var result = await switcher.SwitchToProfileAsync("GameProfile", gameContext, preserveState: true);
if (result.Success)
{
    Console.WriteLine($"Successfully switched to {manager.ActiveProfile?.Name}");
}

// Switch to web mode
result = await switcher.SwitchToProfileAsync("WebProfile", webContext, preserveState: true);
if (result.Success)
{
    Console.WriteLine($"Successfully switched to {manager.ActiveProfile?.Name}");
}
```

## Creating Custom Profiles

```csharp
public class GameModeProfile : IUIProfile
{
    public string Name => "GameProfile";
    public ConsoleMode TargetMode => ConsoleMode.Game;
    public UIProfileMetadata Metadata { get; }

    public GameModeProfile()
    {
        Metadata = new UIProfileMetadata
        {
            DisplayName = "Game Runtime Profile",
            Description = "Optimized for gameplay, debugging, and player interaction",
            Author = "GameConsole Team",
            Version = "1.0.0",
            SupportsHotReload = true
        };
        
        Metadata.Tags.Add("runtime");
        Metadata.Tags.Add("gameplay");
        Metadata.FeatureFlags["DebugTools"] = true;
        Metadata.FeatureFlags["PerformanceMonitoring"] = true;
    }

    public CommandSet GetCommandSet()
    {
        var commands = new CommandSet();
        commands.AddCommand("start-game", new StartGameCommand());
        commands.AddCommand("pause-game", new PauseGameCommand());
        commands.AddCommand("debug-info", new DebugInfoCommand());
        return commands;
    }

    public LayoutConfiguration GetLayoutConfiguration()
    {
        return new LayoutConfiguration
        {
            MainWindow = new WindowConfiguration
            {
                Title = "GameConsole - Game Mode",
                Width = 1280,
                Height = 720,
                Theme = "Dark"
            },
            StatusBar = new StatusBarConfiguration
            {
                Visible = true,
                Height = 24
            }
        };
    }

    public KeyBindingSet GetKeyBindings()
    {
        var bindings = new KeyBindingSet();
        bindings.AddBinding("F5", "start-game");
        bindings.AddBinding("F6", "pause-game");
        bindings.AddBinding("F12", "debug-info");
        return bindings;
    }

    public async Task<bool> CanActivateAsync(IUIContext context, CancellationToken cancellationToken = default)
    {
        return context.CurrentMode == ConsoleMode.Game;
    }

    public async Task ActivateAsync(IUIContext context, CancellationToken cancellationToken = default)
    {
        // Set up game-specific UI elements
        Console.WriteLine("Activating Game Mode Profile...");
        await Task.CompletedTask;
    }

    public async Task DeactivateAsync(IUIContext context, CancellationToken cancellationToken = default)
    {
        // Clean up game-specific resources
        Console.WriteLine("Deactivating Game Mode Profile...");
        await Task.CompletedTask;
    }

    public async Task SaveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        // Save profile configuration to persistent storage
        await Task.CompletedTask;
    }

    public async Task ReloadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        // Reload configuration from storage (hot-reload support)
        await Task.CompletedTask;
    }
}
```

## Profile Validation

```csharp
// Validate individual profile
var validationResult = await validator.ValidateProfileAsync(gameProfile, gameContext);
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}

// Validate collection of profiles for conflicts
var allProfiles = new[] { gameProfile, editorProfile, webProfile };
var collectionResult = await validator.ValidateProfileCollectionAsync(allProfiles);
if (!collectionResult.IsValid)
{
    Console.WriteLine("Profile collection has validation issues");
}
```

## Event Handling

```csharp
// Subscribe to profile switch events
switcher.SwitchStarted += (sender, args) =>
{
    Console.WriteLine($"Switching from {args.FromProfile} to {args.ToProfile}");
};

switcher.SwitchCompleted += (sender, args) =>
{
    Console.WriteLine($"Successfully switched to {args.ToProfile}");
};

switcher.SwitchFailed += (sender, args) =>
{
    Console.WriteLine($"Failed to switch: {args.ErrorMessage}");
};
```

## Profile Inheritance

```csharp
public class EditorModeProfile : IUIProfile
{
    public UIProfileMetadata Metadata { get; }

    public EditorModeProfile()
    {
        Metadata = new UIProfileMetadata();
        
        // Set up inheritance from game profile
        Metadata.InheritsFrom.Add("GameProfile");
        
        // Add editor-specific features
        Metadata.FeatureFlags["AssetBrowser"] = true;
        Metadata.FeatureFlags["SceneEditor"] = true;
        Metadata.CompatibilityRequirements["MinEditorVersion"] = "2.0.0";
    }
    
    // ... rest of implementation
}
```

## Key Features Demonstrated

1. **Dynamic Profile Switching**: Seamlessly switch between different UI modes
2. **State Preservation**: Maintain user state during profile transitions
3. **Profile Validation**: Ensure profiles are consistent and compatible
4. **Event System**: React to profile switch events for custom logic
5. **Configuration Management**: Save and reload profile configurations
6. **Hot-Reloading**: Support for development-time profile updates
7. **Profile Inheritance**: Build profiles that extend base functionality
8. **Mode Organization**: Profiles are automatically organized by target mode

This system provides a solid foundation for building mode-specific UI experiences in GameConsole while maintaining consistency and allowing for future extensibility.