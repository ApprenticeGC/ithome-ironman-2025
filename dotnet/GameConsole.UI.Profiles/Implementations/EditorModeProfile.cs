namespace GameConsole.UI.Profiles.Implementations;

/// <summary>
/// UI profile optimized for editor mode operations, including content creation,
/// asset management, and development workflows.
/// </summary>
public class EditorModeProfile : UIProfile
{
    public EditorModeProfile()
    {
        Name = "EditorModeProfile";
        TargetMode = ConsoleMode.Editor;
        Metadata = new UIProfileMetadata
        {
            DisplayName = "Editor Mode",
            Description = "Optimized for content creation, asset management, and development workflows",
            Version = "1.0.0",
            Author = "GameConsole",
            Tags = new[] { "editor", "development", "authoring", "assets" },
            Priority = 100
        };
    }
    
    public override CommandSet GetCommandSet()
    {
        var commandSet = new CommandSet();
        
        // File operations
        commandSet.Add(new Command
        {
            Id = "file.new",
            Name = "New File",
            Description = "Create a new file",
            KeyBinding = "Ctrl+N",
            Category = "File",
            Priority = 100
        });
        
        commandSet.Add(new Command
        {
            Id = "file.open",
            Name = "Open File",
            Description = "Open an existing file",
            KeyBinding = "Ctrl+O",
            Category = "File",
            Priority = 90
        });
        
        commandSet.Add(new Command
        {
            Id = "file.save",
            Name = "Save File",
            Description = "Save the current file",
            KeyBinding = "Ctrl+S",
            Category = "File",
            Priority = 80
        });
        
        // Asset management
        commandSet.Add(new Command
        {
            Id = "asset.import",
            Name = "Import Asset",
            Description = "Import an asset into the project",
            KeyBinding = "Ctrl+I",
            Category = "Assets",
            Priority = 100
        });
        
        commandSet.Add(new Command
        {
            Id = "asset.browser",
            Name = "Asset Browser",
            Description = "Show/hide asset browser",
            KeyBinding = "Ctrl+B",
            Category = "Assets",
            Priority = 90
        });
        
        // Project tools
        commandSet.Add(new Command
        {
            Id = "project.build",
            Name = "Build Project",
            Description = "Build the current project",
            KeyBinding = "Ctrl+F5",
            Category = "Project",
            Priority = 100
        });
        
        commandSet.Add(new Command
        {
            Id = "project.settings",
            Name = "Project Settings",
            Description = "Open project settings",
            KeyBinding = "Ctrl+,",
            Category = "Project",
            Priority = 80
        });
        
        // Creation tools
        commandSet.Add(new Command
        {
            Id = "create.scene",
            Name = "Create Scene",
            Description = "Create a new scene",
            KeyBinding = "Ctrl+Shift+N",
            Category = "Create",
            Priority = 100
        });
        
        commandSet.Add(new Command
        {
            Id = "create.prefab",
            Name = "Create Prefab",
            Description = "Create a prefab from selection",
            KeyBinding = "Ctrl+Shift+P",
            Category = "Create",
            Priority = 90
        });
        
        return commandSet;
    }
    
    public override LayoutConfiguration GetLayoutConfiguration()
    {
        return new LayoutConfiguration
        {
            DefaultPanelWidth = 120,
            DefaultPanelHeight = 40,
            SupportsDynamicResize = true,
            MinimumConsoleWidth = 120,
            MinimumConsoleHeight = 30,
            Panels = new[]
            {
                new PanelConfiguration
                {
                    Id = "editor.hierarchy",
                    Name = "Hierarchy",
                    Position = "left",
                    Width = 25,
                    Height = 40,
                    IsVisible = true,
                    IsResizable = true,
                    ZOrder = 1
                },
                new PanelConfiguration
                {
                    Id = "editor.scene",
                    Name = "Scene View",
                    Position = "center",
                    Width = 70,
                    Height = 30,
                    IsVisible = true,
                    IsResizable = true,
                    ZOrder = 1
                },
                new PanelConfiguration
                {
                    Id = "editor.inspector",
                    Name = "Inspector",
                    Position = "right",
                    Width = 25,
                    Height = 40,
                    IsVisible = true,
                    IsResizable = true,
                    ZOrder = 1
                },
                new PanelConfiguration
                {
                    Id = "editor.project",
                    Name = "Project Browser",
                    Position = "bottom",
                    Width = 120,
                    Height = 10,
                    IsVisible = true,
                    IsResizable = true,
                    ZOrder = 1
                },
                new PanelConfiguration
                {
                    Id = "editor.console",
                    Name = "Console",
                    Position = "bottom",
                    Width = 120,
                    Height = 8,
                    IsVisible = false,
                    IsResizable = true,
                    ZOrder = 2
                }
            }
        };
    }
    
    public override KeyBindingSet GetKeyBindings()
    {
        var keyBindings = new KeyBindingSet();
        
        // File operations
        keyBindings.Add(new KeyBinding
        {
            CommandId = "file.new",
            Keys = "Ctrl+N",
            Description = "Create new file"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "file.open",
            Keys = "Ctrl+O",
            Description = "Open file"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "file.save",
            Keys = "Ctrl+S",
            Description = "Save file"
        });
        
        // Asset management
        keyBindings.Add(new KeyBinding
        {
            CommandId = "asset.import",
            Keys = "Ctrl+I",
            Description = "Import asset"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "asset.browser",
            Keys = "Ctrl+B",
            Description = "Toggle asset browser"
        });
        
        // Project operations
        keyBindings.Add(new KeyBinding
        {
            CommandId = "project.build",
            Keys = "Ctrl+F5",
            Description = "Build project"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "project.settings",
            Keys = "Ctrl+,",
            Description = "Open project settings"
        });
        
        // Creation tools
        keyBindings.Add(new KeyBinding
        {
            CommandId = "create.scene",
            Keys = "Ctrl+Shift+N",
            Description = "Create new scene"
        });
        
        keyBindings.Add(new KeyBinding
        {
            CommandId = "create.prefab",
            Keys = "Ctrl+Shift+P",
            Description = "Create prefab"
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
                    Id = "file.menu",
                    Text = "&File",
                    Children = new[]
                    {
                        new MenuItem { Id = "file.new", Text = "&New", CommandId = "file.new", ShortcutKey = "Ctrl+N" },
                        new MenuItem { Id = "file.open", Text = "&Open", CommandId = "file.open", ShortcutKey = "Ctrl+O" },
                        new MenuItem { Id = "sep1", IsSeparator = true },
                        new MenuItem { Id = "file.save", Text = "&Save", CommandId = "file.save", ShortcutKey = "Ctrl+S" },
                        new MenuItem { Id = "file.saveAs", Text = "Save &As", CommandId = "file.saveAs", ShortcutKey = "Ctrl+Shift+S" },
                        new MenuItem { Id = "sep2", IsSeparator = true },
                        new MenuItem { Id = "file.exit", Text = "E&xit", CommandId = "app.exit" }
                    }
                },
                new MenuItem
                {
                    Id = "edit.menu",
                    Text = "&Edit",
                    Children = new[]
                    {
                        new MenuItem { Id = "edit.undo", Text = "&Undo", CommandId = "edit.undo", ShortcutKey = "Ctrl+Z" },
                        new MenuItem { Id = "edit.redo", Text = "&Redo", CommandId = "edit.redo", ShortcutKey = "Ctrl+Y" },
                        new MenuItem { Id = "sep3", IsSeparator = true },
                        new MenuItem { Id = "edit.cut", Text = "Cu&t", CommandId = "edit.cut", ShortcutKey = "Ctrl+X" },
                        new MenuItem { Id = "edit.copy", Text = "&Copy", CommandId = "edit.copy", ShortcutKey = "Ctrl+C" },
                        new MenuItem { Id = "edit.paste", Text = "&Paste", CommandId = "edit.paste", ShortcutKey = "Ctrl+V" }
                    }
                },
                new MenuItem
                {
                    Id = "assets.menu",
                    Text = "&Assets",
                    Children = new[]
                    {
                        new MenuItem { Id = "asset.import", Text = "&Import Asset", CommandId = "asset.import", ShortcutKey = "Ctrl+I" },
                        new MenuItem { Id = "asset.browser", Text = "Asset &Browser", CommandId = "asset.browser", ShortcutKey = "Ctrl+B" }
                    }
                },
                new MenuItem
                {
                    Id = "project.menu",
                    Text = "&Project",
                    Children = new[]
                    {
                        new MenuItem { Id = "project.build", Text = "&Build", CommandId = "project.build", ShortcutKey = "Ctrl+F5" },
                        new MenuItem { Id = "sep4", IsSeparator = true },
                        new MenuItem { Id = "project.settings", Text = "&Settings", CommandId = "project.settings", ShortcutKey = "Ctrl+," }
                    }
                },
                new MenuItem
                {
                    Id = "window.menu",
                    Text = "&Window",
                    Children = new[]
                    {
                        new MenuItem { Id = "window.hierarchy", Text = "&Hierarchy", CommandId = "window.hierarchy" },
                        new MenuItem { Id = "window.scene", Text = "&Scene View", CommandId = "window.scene" },
                        new MenuItem { Id = "window.inspector", Text = "&Inspector", CommandId = "window.inspector" },
                        new MenuItem { Id = "window.project", Text = "&Project", CommandId = "window.project" },
                        new MenuItem { Id = "window.console", Text = "&Console", CommandId = "window.console" }
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
                    Id = "editor.status",
                    Text = "Ready",
                    Alignment = "left",
                    Priority = 100
                },
                new StatusBarItem
                {
                    Id = "editor.selection",
                    Text = "No Selection",
                    Alignment = "center",
                    Priority = 80
                },
                new StatusBarItem
                {
                    Id = "editor.mode",
                    Text = "Editor Mode",
                    Alignment = "right",
                    Priority = 90
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
                    Id = "file.new",
                    Text = "New",
                    CommandId = "file.new",
                    Tooltip = "Create new file",
                    Priority = 100
                },
                new ToolbarItem
                {
                    Id = "file.open",
                    Text = "Open",
                    CommandId = "file.open",
                    Tooltip = "Open file",
                    Priority = 90
                },
                new ToolbarItem
                {
                    Id = "file.save",
                    Text = "Save",
                    CommandId = "file.save",
                    Tooltip = "Save file",
                    Priority = 80
                },
                new ToolbarItem { Id = "sep1", IsSeparator = true },
                new ToolbarItem
                {
                    Id = "project.build",
                    Text = "Build",
                    CommandId = "project.build",
                    Tooltip = "Build project",
                    Priority = 70
                },
                new ToolbarItem { Id = "sep2", IsSeparator = true },
                new ToolbarItem
                {
                    Id = "asset.browser",
                    Text = "Assets",
                    CommandId = "asset.browser",
                    Tooltip = "Toggle asset browser",
                    Priority = 60
                }
            }
        };
    }
}