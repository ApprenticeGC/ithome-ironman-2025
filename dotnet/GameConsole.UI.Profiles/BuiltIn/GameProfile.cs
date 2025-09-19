using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles.BuiltIn;

/// <summary>
/// Game mode profile optimized for runtime operations, debugging, and player interaction.
/// Includes performance monitoring, debug tools, and runtime commands.
/// </summary>
public class GameProfile : UIProfile
{
    public GameProfile(ILogger? logger = null) 
        : base("Game", ConsoleMode.Game, logger)
    {
        Metadata = new UIProfileMetadata
        {
            Version = "1.0.0",
            Author = "GameConsole",
            Description = "Game runtime interface with debugging tools and performance monitoring",
            Tags = new[] { "game", "runtime", "debug", "performance" },
            IsBuiltIn = true,
            Priority = 200,
            CompatibleModes = new[] { ConsoleMode.Game }
        };
    }

    public override CommandSet GetCommandSet()
    {
        var commandSet = CreateDefaultCommandSet();

        // Game runtime commands
        commandSet.AddCommand("play", new CommandDefinition
        {
            Category = "Game",
            Description = "Start the game",
            Priority = 1000,
            KeyboardShortcuts = new[] { "F5", "Ctrl+P" }
        });

        commandSet.AddCommand("pause", new CommandDefinition
        {
            Category = "Game",
            Description = "Pause the game",
            Priority = 999,
            KeyboardShortcuts = new[] { "Space", "F6" }
        });

        commandSet.AddCommand("stop", new CommandDefinition
        {
            Category = "Game",
            Description = "Stop the game",
            Priority = 998,
            KeyboardShortcuts = new[] { "Shift+F5" }
        });

        commandSet.AddCommand("step", new CommandDefinition
        {
            Category = "Debug",
            Description = "Step one frame forward",
            Priority = 950,
            KeyboardShortcuts = new[] { "F10" }
        });

        // Debug commands
        commandSet.AddCommand("debug", new CommandDefinition
        {
            Category = "Debug",
            Description = "Toggle debug mode",
            Priority = 900,
            KeyboardShortcuts = new[] { "F12" }
        });

        commandSet.AddCommand("fps", new CommandDefinition
        {
            Category = "Performance",
            Description = "Show FPS counter",
            Priority = 850,
            KeyboardShortcuts = new[] { "Ctrl+F" }
        });

        commandSet.AddCommand("profiler", new CommandDefinition
        {
            Category = "Performance",
            Description = "Open performance profiler",
            Priority = 849,
            KeyboardShortcuts = new[] { "Ctrl+Shift+P" }
        });

        // Console commands for game mode
        commandSet.AddCommand("console", new CommandDefinition
        {
            Category = "Debug",
            Description = "Toggle debug console",
            Priority = 880,
            KeyboardShortcuts = new[] { "~", "Ctrl+`" } // Changed from F1 to avoid conflict
        });

        commandSet.AddCommand("log", new CommandDefinition
        {
            Category = "Debug",
            Description = "View game logs",
            Priority = 875
        });

        return commandSet;
    }

    public override LayoutConfiguration GetLayoutConfiguration()
    {
        return new LayoutConfiguration
        {
            Panels = new[]
            {
                new PanelConfiguration
                {
                    Name = "GameView",
                    Type = "GameView",
                    Position = PanelPosition.Center,
                    Size = "70%",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = false
                },
                new PanelConfiguration
                {
                    Name = "Console",
                    Type = "Console",
                    Position = PanelPosition.Bottom,
                    Size = "30%",
                    IsVisible = false, // Hidden by default, can be toggled
                    IsResizable = true,
                    IsClosable = true
                },
                new PanelConfiguration
                {
                    Name = "Performance",
                    Type = "Performance",
                    Position = PanelPosition.Right,
                    Size = "250px",
                    IsVisible = false, // Hidden by default
                    IsResizable = true,
                    IsClosable = true
                }
            },
            Theme = new ThemeConfiguration
            {
                ColorScheme = "GameDark",
                FontFamily = "Segoe UI",
                FontSize = 12,
                Scale = 1.0f
            },
            Window = new WindowConfiguration
            {
                Width = 1920,
                Height = 1080,
                IsResizable = true,
                StartMaximized = true
            },
            Navigation = new NavigationConfiguration
            {
                ShowMenuBar = false,
                ShowToolbar = true,
                ShowStatusBar = true,
                Shortcuts = new Dictionary<string, string>
                {
                    ["Esc"] = "pause",
                    ["Alt+Tab"] = "switch_window",
                    ["Alt+F4"] = "exit",
                    ["F11"] = "fullscreen"
                }.AsReadOnly()
            }
        };
    }

    public override IReadOnlyDictionary<string, string> GetServiceProviderConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["UI"] = "Game.UI.Provider",
            ["Input"] = "Game.Input.Provider",
            ["Graphics"] = "Game.Graphics.Provider",
            ["Audio"] = "Game.Audio.Provider",
            ["Physics"] = "Game.Physics.Provider",
            ["AI"] = "Game.AI.Provider"
        }.AsReadOnly();
    }

    public override async Task OnActivatedAsync(IUIProfile? previousProfile, CancellationToken cancellationToken = default)
    {
        await base.OnActivatedAsync(previousProfile, cancellationToken);
        
        _logger?.LogInformation("Game profile activated - initializing runtime systems");
        
        // Game-specific activation logic could go here
        // e.g., initialize game systems, load assets, etc.
    }

    public override async Task OnDeactivatedAsync(IUIProfile? nextProfile, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Game profile deactivating - preserving runtime state");
        
        // Game-specific deactivation logic could go here
        // e.g., save game state, pause systems, etc.
        
        await base.OnDeactivatedAsync(nextProfile, cancellationToken);
    }
}