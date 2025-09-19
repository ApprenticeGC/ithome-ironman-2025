using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles.BuiltIn;

/// <summary>
/// Desktop profile optimized for full desktop application with windowed UI.
/// Rich interface with multiple windows, docking, and advanced desktop features.
/// </summary>
public class DesktopProfile : UIProfile
{
    public DesktopProfile(ILogger? logger = null) 
        : base("Desktop", ConsoleMode.Desktop, logger)
    {
        Metadata = new UIProfileMetadata
        {
            Version = "1.0.0",
            Author = "GameConsole",
            Description = "Full desktop application interface with windowing and docking",
            Tags = new[] { "desktop", "windowed", "docking", "rich" },
            IsBuiltIn = true,
            Priority = 250,
            CompatibleModes = new[] { ConsoleMode.Desktop }
        };
    }

    public override CommandSet GetCommandSet()
    {
        var commandSet = CreateDefaultCommandSet();

        // Window management
        commandSet.AddCommand("new_window", new CommandDefinition
        {
            Category = "Window",
            Description = "Create new window",
            Priority = 950,
            KeyboardShortcuts = new[] { "Ctrl+Shift+N" }
        });

        commandSet.AddCommand("close_window", new CommandDefinition
        {
            Category = "Window",
            Description = "Close current window",
            Priority = 949,
            KeyboardShortcuts = new[] { "Ctrl+W" }
        });

        commandSet.AddCommand("minimize", new CommandDefinition
        {
            Category = "Window",
            Description = "Minimize window",
            Priority = 930,
            KeyboardShortcuts = new[] { "Alt+F9" }
        });

        commandSet.AddCommand("maximize", new CommandDefinition
        {
            Category = "Window",
            Description = "Maximize window",
            Priority = 929,
            KeyboardShortcuts = new[] { "Alt+F10" }
        });

        commandSet.AddCommand("restore", new CommandDefinition
        {
            Category = "Window",
            Description = "Restore window",
            Priority = 928,
            KeyboardShortcuts = new[] { "Alt+F5" }
        });

        // Docking and layout
        commandSet.AddCommand("dock", new CommandDefinition
        {
            Category = "Layout",
            Description = "Dock current panel",
            Priority = 900
        });

        commandSet.AddCommand("undock", new CommandDefinition
        {
            Category = "Layout",
            Description = "Undock current panel",
            Priority = 899
        });

        commandSet.AddCommand("reset_layout", new CommandDefinition
        {
            Category = "Layout",
            Description = "Reset to default layout",
            Priority = 880,
            KeyboardShortcuts = new[] { "Ctrl+Shift+R" }
        });

        commandSet.AddCommand("save_layout", new CommandDefinition
        {
            Category = "Layout",
            Description = "Save current layout",
            Priority = 879,
            KeyboardShortcuts = new[] { "Ctrl+Shift+S" }
        });

        // View management
        commandSet.AddCommand("zoom_in", new CommandDefinition
        {
            Category = "View",
            Description = "Zoom in",
            Priority = 860,
            KeyboardShortcuts = new[] { "Ctrl+Plus", "Ctrl+=" }
        });

        commandSet.AddCommand("zoom_out", new CommandDefinition
        {
            Category = "View",
            Description = "Zoom out",
            Priority = 859,
            KeyboardShortcuts = new[] { "Ctrl+Minus" }
        });

        commandSet.AddCommand("zoom_reset", new CommandDefinition
        {
            Category = "View",
            Description = "Reset zoom to 100%",
            Priority = 858,
            KeyboardShortcuts = new[] { "Ctrl+0" }
        });

        // Desktop integration
        commandSet.AddCommand("notification", new CommandDefinition
        {
            Category = "System",
            Description = "Show system notification",
            Priority = 800
        });

        commandSet.AddCommand("tray", new CommandDefinition
        {
            Category = "System",
            Description = "Minimize to system tray",
            Priority = 799
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
                    Name = "MainWorkspace",
                    Type = "Workspace",
                    Position = PanelPosition.Center,
                    Size = "60%",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = false
                },
                new PanelConfiguration
                {
                    Name = "ToolBox",
                    Type = "ToolBox",
                    Position = PanelPosition.Left,
                    Size = "200px",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = true
                },
                new PanelConfiguration
                {
                    Name = "Properties",
                    Type = "Properties",
                    Position = PanelPosition.Right,
                    Size = "250px",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = true
                },
                new PanelConfiguration
                {
                    Name = "Output",
                    Type = "Output",
                    Position = PanelPosition.Bottom,
                    Size = "200px",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = true
                },
                new PanelConfiguration
                {
                    Name = "Explorer",
                    Type = "FileExplorer",
                    Position = PanelPosition.Left,
                    Size = "200px",
                    IsVisible = false, // Tabbed with ToolBox
                    IsResizable = true,
                    IsClosable = true
                }
            },
            Theme = new ThemeConfiguration
            {
                ColorScheme = "DesktopClassic",
                FontFamily = "Segoe UI",
                FontSize = 12,
                Scale = 1.0f
            },
            Window = new WindowConfiguration
            {
                Width = 1440,
                Height = 900,
                IsResizable = true,
                StartMaximized = false
            },
            Navigation = new NavigationConfiguration
            {
                ShowMenuBar = true,
                ShowToolbar = true,
                ShowStatusBar = true,
                Shortcuts = new Dictionary<string, string>
                {
                    ["Alt"] = "show_mnemonics",
                    ["F10"] = "focus_menu",
                    ["Shift+F10"] = "context_menu",
                    ["Ctrl+Tab"] = "next_tab",
                    ["Ctrl+Shift+Tab"] = "prev_tab"
                }.AsReadOnly()
            }
        };
    }

    public override IReadOnlyDictionary<string, string> GetServiceProviderConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["UI"] = "Desktop.UI.Provider",
            ["Input"] = "Desktop.Input.Provider",
            ["Graphics"] = "Desktop.Graphics.Provider",
            ["Window"] = "Desktop.Window.Provider",
            ["Dock"] = "Desktop.Dock.Provider",
            ["Themes"] = "Desktop.Themes.Provider"
        }.AsReadOnly();
    }

    public override async Task OnActivatedAsync(IUIProfile? previousProfile, CancellationToken cancellationToken = default)
    {
        await base.OnActivatedAsync(previousProfile, cancellationToken);
        
        _logger?.LogInformation("Desktop profile activated - initializing windowing system");
        
        // Desktop-specific activation
        // e.g., restore window positions, load saved layouts, etc.
    }

    public override async Task OnDeactivatedAsync(IUIProfile? nextProfile, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Desktop profile deactivating - saving window state");
        
        // Desktop-specific deactivation
        // e.g., save window positions, layouts, etc.
        
        await base.OnDeactivatedAsync(nextProfile, cancellationToken);
    }
}