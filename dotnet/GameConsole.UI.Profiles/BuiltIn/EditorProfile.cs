using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles.BuiltIn;

/// <summary>
/// Editor mode profile optimized for content creation, asset management, and development workflows.
/// Includes authoring tools, asset management, and project management features.
/// </summary>
public class EditorProfile : UIProfile
{
    public EditorProfile(ILogger? logger = null) 
        : base("Editor", ConsoleMode.Editor, logger)
    {
        Metadata = new UIProfileMetadata
        {
            Version = "1.0.0",
            Author = "GameConsole",
            Description = "Content creation and development interface with authoring tools",
            Tags = new[] { "editor", "authoring", "development", "assets" },
            IsBuiltIn = true,
            Priority = 300,
            CompatibleModes = new[] { ConsoleMode.Editor }
        };
    }

    public override CommandSet GetCommandSet()
    {
        var commandSet = CreateDefaultCommandSet();

        // File operations
        commandSet.AddCommand("new", new CommandDefinition
        {
            Category = "File",
            Description = "Create new asset or project",
            Priority = 1000,
            KeyboardShortcuts = new[] { "Ctrl+N" }
        });

        commandSet.AddCommand("open", new CommandDefinition
        {
            Category = "File",
            Description = "Open asset or project",
            Priority = 999,
            KeyboardShortcuts = new[] { "Ctrl+O" }
        });

        commandSet.AddCommand("save", new CommandDefinition
        {
            Category = "File",
            Description = "Save current asset",
            Priority = 998,
            KeyboardShortcuts = new[] { "Ctrl+S" }
        });

        commandSet.AddCommand("save_all", new CommandDefinition
        {
            Category = "File",
            Description = "Save all modified assets",
            Priority = 997,
            KeyboardShortcuts = new[] { "Ctrl+Shift+S" }
        });

        // Asset management
        commandSet.AddCommand("import", new CommandDefinition
        {
            Category = "Assets",
            Description = "Import external assets",
            Priority = 950,
            KeyboardShortcuts = new[] { "Ctrl+I" }
        });

        commandSet.AddCommand("export", new CommandDefinition
        {
            Category = "Assets",
            Description = "Export selected assets",
            Priority = 949,
            KeyboardShortcuts = new[] { "Ctrl+E" }
        });

        commandSet.AddCommand("build", new CommandDefinition
        {
            Category = "Build",
            Description = "Build the project",
            Priority = 900,
            KeyboardShortcuts = new[] { "Ctrl+B", "F7" }
        });

        commandSet.AddCommand("rebuild", new CommandDefinition
        {
            Category = "Build",
            Description = "Rebuild the entire project",
            Priority = 899,
            KeyboardShortcuts = new[] { "Ctrl+Shift+B" }
        });

        // Editor tools
        commandSet.AddCommand("inspector", new CommandDefinition
        {
            Category = "Tools",
            Description = "Open property inspector",
            Priority = 880,
            KeyboardShortcuts = new[] { "F4" }
        });

        commandSet.AddCommand("hierarchy", new CommandDefinition
        {
            Category = "Tools",
            Description = "Show scene hierarchy",
            Priority = 879,
            KeyboardShortcuts = new[] { "Ctrl+H" }
        });

        commandSet.AddCommand("project", new CommandDefinition
        {
            Category = "Tools",
            Description = "Show project browser",
            Priority = 878,
            KeyboardShortcuts = new[] { "Ctrl+P" }
        });

        // Editing operations
        commandSet.AddCommand("undo", new CommandDefinition
        {
            Category = "Edit",
            Description = "Undo last action",
            Priority = 950,
            KeyboardShortcuts = new[] { "Ctrl+Z" }
        });

        commandSet.AddCommand("redo", new CommandDefinition
        {
            Category = "Edit",
            Description = "Redo last undone action",
            Priority = 949,
            KeyboardShortcuts = new[] { "Ctrl+Y", "Ctrl+Shift+Z" }
        });

        commandSet.AddCommand("cut", new CommandDefinition
        {
            Category = "Edit",
            Description = "Cut selection to clipboard",
            Priority = 930,
            KeyboardShortcuts = new[] { "Ctrl+X" }
        });

        commandSet.AddCommand("copy", new CommandDefinition
        {
            Category = "Edit",
            Description = "Copy selection to clipboard",
            Priority = 929,
            KeyboardShortcuts = new[] { "Ctrl+C" }
        });

        commandSet.AddCommand("paste", new CommandDefinition
        {
            Category = "Edit",
            Description = "Paste from clipboard",
            Priority = 928,
            KeyboardShortcuts = new[] { "Ctrl+V" }
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
                    Name = "SceneView",
                    Type = "SceneView",
                    Position = PanelPosition.Center,
                    Size = "50%",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = false
                },
                new PanelConfiguration
                {
                    Name = "Hierarchy",
                    Type = "Hierarchy",
                    Position = PanelPosition.Left,
                    Size = "20%",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = true
                },
                new PanelConfiguration
                {
                    Name = "Inspector",
                    Type = "Inspector",
                    Position = PanelPosition.Right,
                    Size = "25%",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = true
                },
                new PanelConfiguration
                {
                    Name = "Project",
                    Type = "ProjectBrowser",
                    Position = PanelPosition.Bottom,
                    Size = "25%",
                    IsVisible = true,
                    IsResizable = true,
                    IsClosable = true
                },
                new PanelConfiguration
                {
                    Name = "Console",
                    Type = "Console",
                    Position = PanelPosition.Bottom,
                    Size = "25%",
                    IsVisible = false, // Tabbed with Project
                    IsResizable = true,
                    IsClosable = true
                }
            },
            Theme = new ThemeConfiguration
            {
                ColorScheme = "EditorLight",
                FontFamily = "Segoe UI",
                FontSize = 11,
                Scale = 1.0f
            },
            Window = new WindowConfiguration
            {
                Width = 1600,
                Height = 1000,
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
                    ["Ctrl+Shift+N"] = "new_project",
                    ["Ctrl+Alt+O"] = "open_project",
                    ["F5"] = "play_in_editor",
                    ["Shift+F5"] = "stop_play_in_editor"
                }.AsReadOnly()
            }
        };
    }

    public override IReadOnlyDictionary<string, string> GetServiceProviderConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["UI"] = "Editor.UI.Provider",
            ["Input"] = "Editor.Input.Provider",
            ["Graphics"] = "Editor.Graphics.Provider",
            ["Assets"] = "Editor.Assets.Provider",
            ["Build"] = "Editor.Build.Provider",
            ["Version"] = "Editor.Version.Provider"
        }.AsReadOnly();
    }

    public override async Task OnActivatedAsync(IUIProfile? previousProfile, CancellationToken cancellationToken = default)
    {
        await base.OnActivatedAsync(previousProfile, cancellationToken);
        
        _logger?.LogInformation("Editor profile activated - initializing authoring tools");
        
        // Editor-specific activation logic
        // e.g., restore editor state, load last project, etc.
    }

    public override async Task OnDeactivatedAsync(IUIProfile? nextProfile, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Editor profile deactivating - saving editor state");
        
        // Editor-specific deactivation logic
        // e.g., save editor layout, project state, etc.
        
        await base.OnDeactivatedAsync(nextProfile, cancellationToken);
    }
}