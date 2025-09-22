namespace GameConsole.UI.Profiles;

/// <summary>
/// Default built-in profile for Editor mode operations.
/// Optimized for content creation, asset management, and development workflows.
/// </summary>
public sealed class DefaultEditorProfile : UIProfile
{
    private readonly UIProfileMetadata _metadata;

    /// <summary>
    /// Initializes a new instance of the DefaultEditorProfile class.
    /// </summary>
    public DefaultEditorProfile()
    {
        _metadata = new UIProfileMetadata
        {
            DisplayName = "Default Editor Mode",
            Description = "Optimized for content creation, asset management, and development workflows",
            Author = "System",
            Version = "1.0.0",
            Tags = new List<string> { "Editor", "Development", "Creation", "Default" },
            IsSystemProfile = true,
            Priority = 100
        };
    }

    /// <inheritdoc />
    public override string Id => "system.editor.default";

    /// <inheritdoc />
    public override string Name => "Default Editor Profile";

    /// <inheritdoc />
    public override ConsoleMode TargetMode => ConsoleMode.Editor;

    /// <inheritdoc />
    public override UIProfileMetadata Metadata => _metadata;

    /// <inheritdoc />
    public override CommandSet GetCommandSet()
    {
        return new CommandSet
        {
            DefaultCategory = "Edit",
            Categories = new Dictionary<string, List<CommandDefinition>>
            {
                ["File"] = new List<CommandDefinition>
                {
                    new CommandDefinition
                    {
                        Id = "file.new",
                        Name = "New",
                        Description = "Create a new file",
                        KeyBinding = "Ctrl+N",
                        Priority = 100,
                        Icon = "new_file"
                    },
                    new CommandDefinition
                    {
                        Id = "file.open",
                        Name = "Open",
                        Description = "Open an existing file",
                        KeyBinding = "Ctrl+O",
                        Priority = 90,
                        Icon = "open_file"
                    },
                    new CommandDefinition
                    {
                        Id = "file.save",
                        Name = "Save",
                        Description = "Save the current file",
                        KeyBinding = "Ctrl+S",
                        Priority = 80,
                        Icon = "save"
                    },
                    new CommandDefinition
                    {
                        Id = "file.save_as",
                        Name = "Save As...",
                        Description = "Save the current file with a new name",
                        KeyBinding = "Ctrl+Shift+S",
                        Priority = 70,
                        Icon = "save_as"
                    }
                },
                ["Edit"] = new List<CommandDefinition>
                {
                    new CommandDefinition
                    {
                        Id = "edit.undo",
                        Name = "Undo",
                        Description = "Undo the last action",
                        KeyBinding = "Ctrl+Z",
                        Priority = 100,
                        Icon = "undo"
                    },
                    new CommandDefinition
                    {
                        Id = "edit.redo",
                        Name = "Redo",
                        Description = "Redo the last undone action",
                        KeyBinding = "Ctrl+Y",
                        Priority = 90,
                        Icon = "redo"
                    },
                    new CommandDefinition
                    {
                        Id = "edit.cut",
                        Name = "Cut",
                        Description = "Cut selected content",
                        KeyBinding = "Ctrl+X",
                        Priority = 80,
                        Icon = "cut"
                    },
                    new CommandDefinition
                    {
                        Id = "edit.copy",
                        Name = "Copy",
                        Description = "Copy selected content",
                        KeyBinding = "Ctrl+C",
                        Priority = 70,
                        Icon = "copy"
                    },
                    new CommandDefinition
                    {
                        Id = "edit.paste",
                        Name = "Paste",
                        Description = "Paste clipboard content",
                        KeyBinding = "Ctrl+V",
                        Priority = 60,
                        Icon = "paste"
                    }
                },
                ["Assets"] = new List<CommandDefinition>
                {
                    new CommandDefinition
                    {
                        Id = "assets.import",
                        Name = "Import Asset",
                        Description = "Import an external asset",
                        KeyBinding = "Ctrl+I",
                        Priority = 100,
                        Icon = "import"
                    },
                    new CommandDefinition
                    {
                        Id = "assets.export",
                        Name = "Export Asset",
                        Description = "Export selected asset",
                        KeyBinding = "Ctrl+E",
                        Priority = 90,
                        Icon = "export"
                    },
                    new CommandDefinition
                    {
                        Id = "assets.refresh",
                        Name = "Refresh Assets",
                        Description = "Refresh asset database",
                        KeyBinding = "F5",
                        Priority = 80,
                        Icon = "refresh"
                    }
                },
                ["Build"] = new List<CommandDefinition>
                {
                    new CommandDefinition
                    {
                        Id = "build.build",
                        Name = "Build",
                        Description = "Build the project",
                        KeyBinding = "Ctrl+B",
                        Priority = 100,
                        Icon = "build"
                    },
                    new CommandDefinition
                    {
                        Id = "build.rebuild",
                        Name = "Rebuild",
                        Description = "Clean and build the project",
                        KeyBinding = "Ctrl+Shift+B",
                        Priority = 90,
                        Icon = "rebuild"
                    },
                    new CommandDefinition
                    {
                        Id = "build.clean",
                        Name = "Clean",
                        Description = "Clean build artifacts",
                        KeyBinding = "Ctrl+Alt+C",
                        Priority = 80,
                        Icon = "clean"
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
            LayoutTemplate = "Editor",
            DefaultFocusPanel = "SceneView",
            AllowPanelResize = true,
            AllowPanelReorder = true,
            MinPanelWidth = 200,
            MinPanelHeight = 150,
            Panels = new List<PanelConfiguration>
            {
                new PanelConfiguration
                {
                    Id = "SceneView",
                    Title = "Scene",
                    ContentType = "SceneEditor",
                    Width = "50%",
                    Height = "70%",
                    DockPosition = "Center",
                    IsVisible = true,
                    CanClose = false,
                    Priority = 100
                },
                new PanelConfiguration
                {
                    Id = "Hierarchy",
                    Title = "Hierarchy",
                    ContentType = "SceneHierarchy",
                    Width = "25%",
                    Height = "70%",
                    DockPosition = "Left",
                    IsVisible = true,
                    CanClose = true,
                    Priority = 90
                },
                new PanelConfiguration
                {
                    Id = "Inspector",
                    Title = "Inspector",
                    ContentType = "PropertyEditor",
                    Width = "25%",
                    Height = "70%",
                    DockPosition = "Right",
                    IsVisible = true,
                    CanClose = true,
                    Priority = 80
                },
                new PanelConfiguration
                {
                    Id = "AssetBrowser",
                    Title = "Assets",
                    ContentType = "AssetBrowser",
                    Width = "50%",
                    Height = "30%",
                    DockPosition = "Bottom",
                    IsVisible = true,
                    CanClose = true,
                    Priority = 70
                },
                new PanelConfiguration
                {
                    Id = "Console",
                    Title = "Console",
                    ContentType = "LogOutput",
                    Width = "50%",
                    Height = "30%",
                    DockPosition = "Bottom",
                    IsVisible = true,
                    CanClose = true,
                    Priority = 60
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
                ["Ctrl+N"] = "file.new",
                ["Ctrl+O"] = "file.open",
                ["Ctrl+S"] = "file.save",
                ["Ctrl+Shift+S"] = "file.save_as",
                ["Ctrl+Z"] = "edit.undo",
                ["Ctrl+Y"] = "edit.redo",
                ["Ctrl+X"] = "edit.cut",
                ["Ctrl+C"] = "edit.copy",
                ["Ctrl+V"] = "edit.paste",
                ["Ctrl+I"] = "assets.import",
                ["Ctrl+E"] = "assets.export",
                ["F5"] = "assets.refresh",
                ["Ctrl+B"] = "build.build",
                ["Ctrl+Shift+B"] = "build.rebuild",
                ["Ctrl+Alt+C"] = "build.clean",
                ["F1"] = "global.help",
                ["Ctrl+Comma"] = "global.settings",
                ["Delete"] = "edit.delete",
                ["F2"] = "edit.rename"
            },
            ContextualBindings = new Dictionary<string, Dictionary<string, string>>
            {
                ["SceneView"] = new Dictionary<string, string>
                {
                    ["W"] = "view.pan.up",
                    ["A"] = "view.pan.left",
                    ["S"] = "view.pan.down",
                    ["D"] = "view.pan.right",
                    ["Q"] = "tool.select",
                    ["E"] = "tool.rotate",
                    ["R"] = "tool.scale"
                },
                ["AssetBrowser"] = new Dictionary<string, string>
                {
                    ["Enter"] = "assets.open",
                    ["F2"] = "assets.rename",
                    ["Delete"] = "assets.delete"
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
                        Id = "menu.file",
                        Text = "File",
                        Priority = 100,
                        SubItems = new List<MenuItemConfiguration>
                        {
                            new MenuItemConfiguration { Id = "file.new", Text = "New", Command = "file.new", ShortcutText = "Ctrl+N" },
                            new MenuItemConfiguration { Id = "file.open", Text = "Open...", Command = "file.open", ShortcutText = "Ctrl+O" },
                            new MenuItemConfiguration { IsSeparator = true },
                            new MenuItemConfiguration { Id = "file.save", Text = "Save", Command = "file.save", ShortcutText = "Ctrl+S" },
                            new MenuItemConfiguration { Id = "file.save_as", Text = "Save As...", Command = "file.save_as", ShortcutText = "Ctrl+Shift+S" },
                            new MenuItemConfiguration { IsSeparator = true },
                            new MenuItemConfiguration { Id = "file.exit", Text = "Exit", Command = "application.exit", ShortcutText = "Alt+F4" }
                        }
                    },
                    new MenuItemConfiguration
                    {
                        Id = "menu.edit",
                        Text = "Edit",
                        Priority = 90,
                        SubItems = new List<MenuItemConfiguration>
                        {
                            new MenuItemConfiguration { Id = "edit.undo", Text = "Undo", Command = "edit.undo", ShortcutText = "Ctrl+Z" },
                            new MenuItemConfiguration { Id = "edit.redo", Text = "Redo", Command = "edit.redo", ShortcutText = "Ctrl+Y" },
                            new MenuItemConfiguration { IsSeparator = true },
                            new MenuItemConfiguration { Id = "edit.cut", Text = "Cut", Command = "edit.cut", ShortcutText = "Ctrl+X" },
                            new MenuItemConfiguration { Id = "edit.copy", Text = "Copy", Command = "edit.copy", ShortcutText = "Ctrl+C" },
                            new MenuItemConfiguration { Id = "edit.paste", Text = "Paste", Command = "edit.paste", ShortcutText = "Ctrl+V" }
                        }
                    },
                    new MenuItemConfiguration
                    {
                        Id = "menu.assets",
                        Text = "Assets",
                        Priority = 80,
                        SubItems = new List<MenuItemConfiguration>
                        {
                            new MenuItemConfiguration { Id = "assets.import", Text = "Import...", Command = "assets.import", ShortcutText = "Ctrl+I" },
                            new MenuItemConfiguration { Id = "assets.export", Text = "Export...", Command = "assets.export", ShortcutText = "Ctrl+E" },
                            new MenuItemConfiguration { IsSeparator = true },
                            new MenuItemConfiguration { Id = "assets.refresh", Text = "Refresh", Command = "assets.refresh", ShortcutText = "F5" }
                        }
                    },
                    new MenuItemConfiguration
                    {
                        Id = "menu.build",
                        Text = "Build",
                        Priority = 70,
                        SubItems = new List<MenuItemConfiguration>
                        {
                            new MenuItemConfiguration { Id = "build.build", Text = "Build", Command = "build.build", ShortcutText = "Ctrl+B" },
                            new MenuItemConfiguration { Id = "build.rebuild", Text = "Rebuild", Command = "build.rebuild", ShortcutText = "Ctrl+Shift+B" },
                            new MenuItemConfiguration { Id = "build.clean", Text = "Clean", Command = "build.clean", ShortcutText = "Ctrl+Alt+C" }
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
            DefaultText = "Editor Mode - Ready",
            Segments = new List<StatusBarSegment>
            {
                new StatusBarSegment
                {
                    Id = "mode",
                    Type = "Text",
                    Width = "120px",
                    Alignment = "Left",
                    Format = "Editor Mode",
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
                    Id = "selection",
                    Type = "SelectionInfo",
                    Width = "150px",
                    Alignment = "Right",
                    Format = "Selected: {0}",
                    Priority = 80
                },
                new StatusBarSegment
                {
                    Id = "tool",
                    Type = "ToolInfo",
                    Width = "100px",
                    Alignment = "Right",
                    Format = "Tool: {0}",
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
            DefaultToolbar = "MainTools",
            Toolbars = new List<ToolbarDefinition>
            {
                new ToolbarDefinition
                {
                    Id = "MainTools",
                    Name = "Main Tools",
                    Position = "Top",
                    IsVisible = true,
                    Priority = 100,
                    Items = new List<ToolbarItemDefinition>
                    {
                        new ToolbarItemDefinition
                        {
                            Id = "new",
                            Type = "Button",
                            Command = "file.new",
                            Icon = "new_file",
                            Tooltip = "New (Ctrl+N)",
                            Priority = 100
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "open",
                            Type = "Button",
                            Command = "file.open",
                            Icon = "open_file",
                            Tooltip = "Open (Ctrl+O)",
                            Priority = 90
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "save",
                            Type = "Button",
                            Command = "file.save",
                            Icon = "save",
                            Tooltip = "Save (Ctrl+S)",
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
                            Id = "undo",
                            Type = "Button",
                            Command = "edit.undo",
                            Icon = "undo",
                            Tooltip = "Undo (Ctrl+Z)",
                            Priority = 70
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "redo",
                            Type = "Button",
                            Command = "edit.redo",
                            Icon = "redo",
                            Tooltip = "Redo (Ctrl+Y)",
                            Priority = 60
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "separator2",
                            Type = "Separator",
                            Priority = 55
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "build",
                            Type = "Button",
                            Command = "build.build",
                            Icon = "build",
                            Tooltip = "Build (Ctrl+B)",
                            Priority = 50
                        }
                    }
                },
                new ToolbarDefinition
                {
                    Id = "SceneTools",
                    Name = "Scene Tools",
                    Position = "Left",
                    IsVisible = true,
                    Priority = 90,
                    Items = new List<ToolbarItemDefinition>
                    {
                        new ToolbarItemDefinition
                        {
                            Id = "select",
                            Type = "Button",
                            Command = "tool.select",
                            Icon = "select",
                            Tooltip = "Select Tool (Q)",
                            Priority = 100
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "move",
                            Type = "Button",
                            Command = "tool.move",
                            Icon = "move",
                            Tooltip = "Move Tool (W)",
                            Priority = 90
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "rotate",
                            Type = "Button",
                            Command = "tool.rotate",
                            Icon = "rotate",
                            Tooltip = "Rotate Tool (E)",
                            Priority = 80
                        },
                        new ToolbarItemDefinition
                        {
                            Id = "scale",
                            Type = "Button",
                            Command = "tool.scale",
                            Icon = "scale",
                            Tooltip = "Scale Tool (R)",
                            Priority = 70
                        }
                    }
                }
            }
        };
    }
}