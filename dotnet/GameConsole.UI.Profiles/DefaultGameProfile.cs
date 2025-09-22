namespace GameConsole.UI.Profiles;

/// <summary>
/// Default built-in profile for Game mode operations.
/// Optimized for runtime operations, debugging, and player interaction.
/// </summary>
public sealed class DefaultGameProfile : UIProfile
{
    private readonly UIProfileMetadata _metadata;

    /// <summary>
    /// Initializes a new instance of the DefaultGameProfile class.
    /// </summary>
    public DefaultGameProfile()
    {
        _metadata = new UIProfileMetadata
        {
            DisplayName = "Default Game Mode",
            Description = "Optimized for runtime operations, debugging, and player interaction",
            Author = "System",
            Version = "1.0.0",
            Tags = new List<string> { "Game", "Runtime", "Debug", "Default" },
            IsSystemProfile = true,
            Priority = 100
        };
    }

    /// <inheritdoc />
    public override string Id => "system.game.default";

    /// <inheritdoc />
    public override string Name => "Default Game Profile";

    /// <inheritdoc />
    public override ConsoleMode TargetMode => ConsoleMode.Game;

    /// <inheritdoc />
    public override UIProfileMetadata Metadata => _metadata;

    /// <inheritdoc />
    public override CommandSet GetCommandSet()
    {
        return new CommandSet
        {
            DefaultCategory = "Game",
            Categories = new Dictionary<string, List<CommandDefinition>>
            {
                ["Game"] = new List<CommandDefinition>
                {
                    new CommandDefinition
                    {
                        Id = "game.play",
                        Name = "Play",
                        Description = "Start the game",
                        KeyBinding = "F5",
                        Priority = 100,
                        Icon = "play"
                    },
                    new CommandDefinition
                    {
                        Id = "game.pause",
                        Name = "Pause",
                        Description = "Pause the game",
                        KeyBinding = "Space",
                        Priority = 90,
                        Icon = "pause"
                    },
                    new CommandDefinition
                    {
                        Id = "game.stop",
                        Name = "Stop",
                        Description = "Stop the game",
                        KeyBinding = "Shift+F5",
                        Priority = 80,
                        Icon = "stop"
                    }
                },
                ["Debug"] = new List<CommandDefinition>
                {
                    new CommandDefinition
                    {
                        Id = "debug.console",
                        Name = "Debug Console",
                        Description = "Open debug console",
                        KeyBinding = "F12",
                        Priority = 100,
                        Icon = "console"
                    },
                    new CommandDefinition
                    {
                        Id = "debug.inspector",
                        Name = "Object Inspector",
                        Description = "Open object inspector",
                        KeyBinding = "Ctrl+I",
                        Priority = 90,
                        Icon = "inspect"
                    },
                    new CommandDefinition
                    {
                        Id = "debug.profiler",
                        Name = "Performance Profiler",
                        Description = "Open performance profiler",
                        KeyBinding = "Ctrl+P",
                        Priority = 80,
                        Icon = "chart"
                    }
                }
            },
            GlobalCommands = new List<CommandDefinition>
            {
                new CommandDefinition
                {
                    Id = "global.help",
                    Name = "Help",
                    Description = "Show help information",
                    KeyBinding = "F1",
                    Priority = 50,
                    Icon = "help"
                },
                new CommandDefinition
                {
                    Id = "global.settings",
                    Name = "Settings",
                    Description = "Open settings",
                    KeyBinding = "Ctrl+Comma",
                    Priority = 40,
                    Icon = "settings"
                }
            }
        };
    }

    /// <inheritdoc />
    public override LayoutConfiguration GetLayoutConfiguration()
    {
        return new LayoutConfiguration
        {
            LayoutTemplate = "Game",
            DefaultFocusPanel = "GameView",
            AllowPanelResize = true,
            AllowPanelReorder = false, // Keep game layout fixed
            MinPanelWidth = 200,
            MinPanelHeight = 150,
            Panels = new List<PanelConfiguration>
            {
                new PanelConfiguration
                {
                    Id = "GameView",
                    Title = "Game",
                    ContentType = "GameRenderer",
                    Width = "70%",
                    Height = "80%",
                    DockPosition = "Center",
                    IsVisible = true,
                    CanClose = false,
                    Priority = 100
                },
                new PanelConfiguration
                {
                    Id = "Console",
                    Title = "Console",
                    ContentType = "LogOutput",
                    Width = "100%",
                    Height = "20%",
                    DockPosition = "Bottom",
                    IsVisible = true,
                    CanClose = true,
                    Priority = 90
                },
                new PanelConfiguration
                {
                    Id = "Inspector",
                    Title = "Inspector",
                    ContentType = "ObjectInspector",
                    Width = "30%",
                    Height = "80%",
                    DockPosition = "Right",
                    IsVisible = false,
                    CanClose = true,
                    Priority = 80
                }
            }
        };
    }

    /// <inheritdoc />
    public override KeyBindingSet GetKeyBindings()
    {
        return new KeyBindingSet
        {
            AllowUserCustomization = true,
            GlobalBindings = new Dictionary<string, string>
            {
                ["F5"] = "game.play",
                ["Space"] = "game.pause",
                ["Shift+F5"] = "game.stop",
                ["F12"] = "debug.console",
                ["Ctrl+I"] = "debug.inspector",
                ["Ctrl+P"] = "debug.profiler",
                ["F1"] = "global.help",
                ["Ctrl+Comma"] = "global.settings",
                ["Escape"] = "ui.back"
            },
            ContextualBindings = new Dictionary<string, Dictionary<string, string>>
            {
                ["GameView"] = new Dictionary<string, string>
                {
                    ["W"] = "player.move.forward",
                    ["A"] = "player.move.left",
                    ["S"] = "player.move.backward",
                    ["D"] = "player.move.right"
                }
            }
        };
    }

    /// <inheritdoc />
    public override MenuConfiguration GetMenuConfiguration()
    {
        return new MenuConfiguration
        {
            ShowKeyboardShortcuts = true,
            ShowIcons = true,
            MainMenu = new MenuBarConfiguration
            {
                IsVisible = true,
                Position = "Top",
                Items = new List<MenuItemConfiguration>
                {
                    new MenuItemConfiguration
                    {
                        Id = "menu.game",
                        Text = "Game",
                        Priority = 100,
                        SubItems = new List<MenuItemConfiguration>
                        {
                            new MenuItemConfiguration { Id = "game.play", Text = "Play", Command = "game.play", ShortcutText = "F5" },
                            new MenuItemConfiguration { Id = "game.pause", Text = "Pause", Command = "game.pause", ShortcutText = "Space" },
                            new MenuItemConfiguration { Id = "game.stop", Text = "Stop", Command = "game.stop", ShortcutText = "Shift+F5" },
                            new MenuItemConfiguration { IsSeparator = true },
                            new MenuItemConfiguration { Id = "game.exit", Text = "Exit", Command = "application.exit", ShortcutText = "Alt+F4" }
                        }
                    },
                    new MenuItemConfiguration
                    {
                        Id = "menu.debug",
                        Text = "Debug",
                        Priority = 90,
                        SubItems = new List<MenuItemConfiguration>
                        {
                            new MenuItemConfiguration { Id = "debug.console", Text = "Console", Command = "debug.console", ShortcutText = "F12" },
                            new MenuItemConfiguration { Id = "debug.inspector", Text = "Inspector", Command = "debug.inspector", ShortcutText = "Ctrl+I" },
                            new MenuItemConfiguration { Id = "debug.profiler", Text = "Profiler", Command = "debug.profiler", ShortcutText = "Ctrl+P" }
                        }
                    }
                }
            }
        };
    }

    /// <inheritdoc />
    public override StatusBarConfiguration GetStatusBarConfiguration()
    {
        return new StatusBarConfiguration
        {
            IsVisible = true,
            Position = "Bottom",
            DefaultText = "Game Mode - Ready",
            Segments = new List<StatusBarSegment>
            {
                new StatusBarSegment
                {
                    Id = "mode",
                    Type = "Text",
                    Width = "120px",
                    Alignment = "Left",
                    Format = "Game Mode",
                    Priority = 100
                },
                new StatusBarSegment
                {
                    Id = "status",
                    Type = "Text",
                    Width = "*",
                    Alignment = "Left",
                    Format = "{0}",
                    Priority = 90
                },
                new StatusBarSegment
                {
                    Id = "fps",
                    Type = "Performance",
                    Width = "80px",
                    Alignment = "Right",
                    Format = "FPS: {0:F1}",
                    Priority = 80
                },
                new StatusBarSegment
                {
                    Id = "memory",
                    Type = "Performance",
                    Width = "100px",
                    Alignment = "Right",
                    Format = "Mem: {0:F1}MB",
                    Priority = 70
                }
            }
        };
    }

    /// <inheritdoc />
    public override ToolbarConfiguration GetToolbarConfiguration()
    {
        return new ToolbarConfiguration
        {
            AllowUserCustomization = true,
            AllowDocking = true,
            DefaultToolbar = "GameControls",
            Toolbars = new List<ToolbarDefinition>
            {
                new ToolbarDefinition
                {
                    Id = "GameControls",
                    Name = "Game Controls",
                    Position = "Top",
                    IsVisible = true,
                    Priority = 100,
                    Items = new List<ToolbarItemDefinition>
                    {
                        new ToolbarItemDefinition
                        {
                            Id = "play",
                            Type = "Button",
                            Command = "game.play",
                            Icon = "play",
                            Tooltip = "Play (F5)",
                            Priority = 100
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "pause",
                            Type = "Button",
                            Command = "game.pause",
                            Icon = "pause",
                            Tooltip = "Pause (Space)",
                            Priority = 90
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "stop",
                            Type = "Button",
                            Command = "game.stop",
                            Icon = "stop",
                            Tooltip = "Stop (Shift+F5)",
                            Priority = 80
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "separator1",
                            Type = "Separator",
                            Priority = 75
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "console",
                            Type = "Button",
                            Command = "debug.console",
                            Icon = "console",
                            Tooltip = "Debug Console (F12)",
                            Priority = 70
                        }
                    }
                }
            }
        };
    }
}