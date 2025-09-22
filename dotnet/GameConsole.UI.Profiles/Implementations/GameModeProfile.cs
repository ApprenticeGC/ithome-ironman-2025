namespace GameConsole.UI.Profiles.Implementations;

/// <summary>
/// UI profile optimized for game mode operations, including runtime debugging,
/// player interaction, and performance monitoring.
/// </summary>
public class GameModeProfile : UIProfile
{
    public GameModeProfile()
    {
        Name = "GameModeProfile";
        TargetMode = ConsoleMode.Game;
        Metadata = new UIProfileMetadata
        {
            DisplayName = "Game Mode",
            Description = "Optimized for runtime operations, debugging, and player interaction",
            Version = "1.0.0",
            Author = "GameConsole",
            Tags = new[] { "game", "runtime", "debug", "performance" },
            Priority = 100
        };
    }
    
    public override CommandSet GetCommandSet()
    {
        var commandSet = new CommandSet();
        
        // Runtime commands
        commandSet.Add(new Command
        {
            Id = "game.start",
            Name = "Start Game",
            Description = "Start the game runtime",
            KeyBinding = "F5",
            Category = "Runtime",
            Priority = 100
        });
        
        commandSet.Add(new Command
        {
            Id = "game.pause",
            Name = "Pause Game",
            Description = "Pause the game execution",
            KeyBinding = "F6",
            Category = "Runtime",
            Priority = 90
        });
        
        commandSet.Add(new Command
        {
            Id = "game.stop",
            Name = "Stop Game",
            Description = "Stop the game runtime",
            KeyBinding = "Shift+F5",
            Category = "Runtime",
            Priority = 80
        });
        
        // Debug commands
        commandSet.Add(new Command
        {
            Id = "debug.console",
            Name = "Debug Console",
            Description = "Show/hide debug console",
            KeyBinding = "F12",
            Category = "Debug",
            Priority = 100
        });
        
        commandSet.Add(new Command
        {
            Id = "debug.breakpoint",
            Name = "Toggle Breakpoint",
            Description = "Toggle breakpoint at current line",
            KeyBinding = "F9",
            Category = "Debug",
            Priority = 90
        });
        
        // Performance monitoring
        commandSet.Add(new Command
        {
            Id = "perf.stats",
            Name = "Performance Stats",
            Description = "Show performance statistics",
            KeyBinding = "Ctrl+P",
            Category = "Performance",
            Priority = 80
        });
        
        return commandSet;
    }
    
    public override LayoutConfiguration GetLayoutConfiguration()
    {
        return new LayoutConfiguration
        {
            DefaultPanelWidth = 100,
            DefaultPanelHeight = 30,
            SupportsDynamicResize = true,
            MinimumConsoleWidth = 80,
            MinimumConsoleHeight = 25,
            Panels = new[]
            {
                new PanelConfiguration
                {
                    Id = "game.viewport",
                    Name = "Game Viewport",
                    Position = "center",
                    Width = 80,
                    Height = 20,
                    IsVisible = true,
                    IsResizable = true,
                    ZOrder = 1
                },
                new PanelConfiguration
                {
                    Id = "game.debug",
                    Name = "Debug Panel",
                    Position = "bottom",
                    Width = 80,
                    Height = 8,
                    IsVisible = false,
                    IsResizable = true,
                    ZOrder = 2
                },
                new PanelConfiguration
                {
                    Id = "game.stats",
                    Name = "Performance Stats",
                    Position = "right",
                    Width = 20,
                    Height = 20,
                    IsVisible = false,
                    IsResizable = true,
                    ZOrder = 1
                }
            }
        };
    }
    
    public override KeyBindingSet GetKeyBindings()
    {
        var keyBindings = new KeyBindingSet();
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "game.start",
            Keys = "F5",
            Description = "Start game execution"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "game.pause",
            Keys = "F6",
            Description = "Pause game execution"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "game.stop",
            Keys = "Shift+F5",
            Description = "Stop game execution"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "debug.console",
            Keys = "F12",
            Description = "Toggle debug console"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "debug.breakpoint",
            Keys = "F9",
            Description = "Toggle breakpoint"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "perf.stats",
            Keys = "Ctrl+P",
            Description = "Show performance stats"
        });
        
        return keyBindings;
    }
    
    public override MenuConfiguration GetMenuConfiguration()
    {
        return new MenuConfiguration
        {
            ShowAcceleratorKeys = true,
            MainMenu = new[]
            {
                new MenuItem
                {
                    Id = "game.menu",
                    Text = "&Game",
                    Children = new[]
                    {
                        new MenuItem { Id = "game.start", Text = "&Start Game", CommandId = "game.start", ShortcutKey = "F5" },
                        new MenuItem { Id = "game.pause", Text = "&Pause Game", CommandId = "game.pause", ShortcutKey = "F6" },
                        new MenuItem { Id = "game.stop", Text = "S&top Game", CommandId = "game.stop", ShortcutKey = "Shift+F5" },
                        new MenuItem { Id = "sep1", IsSeparator = true },
                        new MenuItem { Id = "game.exit", Text = "E&xit", CommandId = "app.exit" }
                    }
                },
                new MenuItem
                {
                    Id = "debug.menu",
                    Text = "&Debug",
                    Children = new[]
                    {
                        new MenuItem { Id = "debug.console", Text = "Debug &Console", CommandId = "debug.console", ShortcutKey = "F12" },
                        new MenuItem { Id = "debug.breakpoint", Text = "Toggle &Breakpoint", CommandId = "debug.breakpoint", ShortcutKey = "F9" }
                    }
                },
                new MenuItem
                {
                    Id = "performance.menu",
                    Text = "&Performance",
                    Children = new[]
                    {
                        new MenuItem { Id = "perf.stats", Text = "&Statistics", CommandId = "perf.stats", ShortcutKey = "Ctrl+P" }
                    }
                }
            }
        };
    }
    
    public override StatusBarConfiguration GetStatusBarConfiguration()
    {
        return new StatusBarConfiguration
        {
            IsVisible = true,
            Position = "bottom",
            Height = 1,
            Items = new[]
            {
                new StatusBarItem
                {
                    Id = "game.status",
                    Text = "Game: Stopped",
                    Alignment = "left",
                    Priority = 100
                },
                new StatusBarItem
                {
                    Id = "game.fps",
                    Text = "FPS: 0",
                    Alignment = "right",
                    Priority = 80
                }
            }
        };
    }
    
    public override ToolbarConfiguration GetToolbarConfiguration()
    {
        return new ToolbarConfiguration
        {
            IsVisible = true,
            Position = "top",
            Items = new[]
            {
                new ToolbarItem
                {
                    Id = "game.start",
                    Text = "Start",
                    CommandId = "game.start",
                    Tooltip = "Start game execution",
                    Priority = 100
                },
                new ToolbarItem
                {
                    Id = "game.pause",
                    Text = "Pause",
                    CommandId = "game.pause",
                    Tooltip = "Pause game execution",
                    Priority = 90
                },
                new ToolbarItem
                {
                    Id = "game.stop",
                    Text = "Stop",
                    CommandId = "game.stop",
                    Tooltip = "Stop game execution",
                    Priority = 80
                },
                new ToolbarItem { Id = "sep1", IsSeparator = true },
                new ToolbarItem
                {
                    Id = "debug.console",
                    Text = "Debug",
                    CommandId = "debug.console",
                    Tooltip = "Toggle debug console",
                    Priority = 70
                }
            }
        };
    }
}