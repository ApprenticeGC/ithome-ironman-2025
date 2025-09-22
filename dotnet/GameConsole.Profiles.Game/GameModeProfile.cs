using GameConsole.Core.Abstractions;
using GameConsole.UI.Profiles;
using GameConsole.Profiles.Game.Commands;

namespace GameConsole.Profiles.Game;

/// <summary>
/// UI profile optimized for game mode operations.
/// </summary>
[Service("Game Mode Profile", "1.0.0", "UI profile optimized for runtime game operations and debugging")]
public class GameModeProfile : UIProfile
{
    public GameModeProfile()
    {
        Name = "GameMode";
        TargetMode = ConsoleMode.Game;
        Metadata = new UIProfileMetadata
        {
            Description = "Optimized for runtime game operations and debugging",
            Version = "1.0.0",
            Properties = new Dictionary<string, object>
            {
                ["Priority"] = 100,
                ["Category"] = "Game"
            }
        };
    }

    public override CommandSet GetCommandSet()
    {
        var commandSet = new CommandSet();
        
        // Game-specific commands
        commandSet.RegisterCommand("play", new PlayGameCommand());
        commandSet.RegisterCommand("pause", new PauseGameCommand());
        commandSet.RegisterCommand("stop", new StopGameCommand());
        commandSet.RegisterCommand("debug", new DebugCommand());
        commandSet.RegisterCommand("perf", new PerformanceCommand());
        
        // Aliases for convenience
        commandSet.RegisterAlias("p", "play");
        commandSet.RegisterAlias("d", "debug");
        commandSet.RegisterAlias("performance", "perf");
        
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
                    Name = "Console",
                    Type = PanelType.Console,
                    Position = new Position(0, 0),
                    Size = new Size(80, 25),
                    IsVisible = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["AutoScroll"] = true,
                        ["ShowTimestamps"] = true
                    }
                },
                new PanelConfiguration
                {
                    Name = "Performance",
                    Type = PanelType.Performance,
                    Position = new Position(80, 0),
                    Size = new Size(40, 12),
                    IsVisible = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["RefreshRate"] = 1000,
                        ["ShowGraphs"] = true
                    }
                },
                new PanelConfiguration
                {
                    Name = "Debug",
                    Type = PanelType.Debug,
                    Position = new Position(80, 12),
                    Size = new Size(40, 13),
                    IsVisible = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["ShowCallStack"] = true,
                        ["ShowVariables"] = true
                    }
                }
            },
            KeyBindings = CreateGameModeKeyBindings(),
            Theme = new ThemeConfiguration
            {
                PrimaryColor = "Green",
                BackgroundColor = "Black",
                TextColor = "White",
                Properties = new Dictionary<string, object>
                {
                    ["HighlightColor"] = "Yellow",
                    ["ErrorColor"] = "Red"
                }
            },
            CustomProperties = new Dictionary<string, object>
            {
                ["Mode"] = "Game",
                ["ShowFPS"] = true,
                ["EnableHotkeys"] = true
            }
        };
    }

    private static KeyBindingSet CreateGameModeKeyBindings()
    {
        var keyBindings = new KeyBindingSet();
        
        // Game control hotkeys
        keyBindings.Add("Ctrl+P", "play");
        keyBindings.Add("Ctrl+Shift+P", "pause");
        keyBindings.Add("Ctrl+S", "stop");
        keyBindings.Add("F5", "debug");
        keyBindings.Add("F11", "perf");
        
        // Quick access keys
        keyBindings.Add("Space", "pause");
        keyBindings.Add("Escape", "stop");
        
        return keyBindings;
    }

    public override Task<bool> CanActivateAsync(CancellationToken cancellationToken = default)
    {
        // Game profile can always be activated
        return Task.FromResult(true);
    }

    public override Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Game Mode Profile activated");
        Console.WriteLine("Available commands: play, pause, stop, debug, perf");
        Console.WriteLine("Use Ctrl+P to play, Space to pause, Esc to stop");
        
        // Here you would initialize game-specific services
        // e.g., performance monitoring, debug overlay, etc.
        
        return Task.CompletedTask;
    }

    public override Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Game Mode Profile deactivated");
        
        // Here you would cleanup game-specific resources
        // e.g., stop performance monitoring, hide debug overlay, etc.
        
        return Task.CompletedTask;
    }
}