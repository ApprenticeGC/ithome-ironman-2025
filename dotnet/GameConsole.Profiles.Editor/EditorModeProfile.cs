using GameConsole.Core.Abstractions;
using GameConsole.UI.Profiles;
using GameConsole.Profiles.Editor.Commands;

namespace GameConsole.Profiles.Editor;

/// <summary>
/// UI profile optimized for editor mode operations.
/// </summary>
[Service("Editor Mode Profile", "1.0.0", "UI profile optimized for content creation and development workflows")]
public class EditorModeProfile : UIProfile
{
    public EditorModeProfile()
    {
        Name = "EditorMode";
        TargetMode = ConsoleMode.Editor;
        Metadata = new UIProfileMetadata
        {
            Description = "Optimized for content creation, asset management, and development workflows",
            Version = "1.0.0",
            Properties = new Dictionary<string, object>
            {
                ["Priority"] = 200,
                ["Category"] = "Editor"
            }
        };
    }

    public override CommandSet GetCommandSet()
    {
        var commandSet = new CommandSet();
        
        // Editor-specific commands
        commandSet.RegisterCommand("create", new CreateAssetCommand());
        commandSet.RegisterCommand("import", new ImportAssetCommand());
        commandSet.RegisterCommand("export", new ExportAssetCommand());
        commandSet.RegisterCommand("build", new BuildCommand());
        commandSet.RegisterCommand("deploy", new DeployCommand());
        
        // Aliases for convenience
        commandSet.RegisterAlias("c", "create");
        commandSet.RegisterAlias("i", "import");
        commandSet.RegisterAlias("e", "export");
        commandSet.RegisterAlias("b", "build");
        commandSet.RegisterAlias("d", "deploy");
        
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
                    Size = new Size(60, 30),
                    IsVisible = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["ShowLineNumbers"] = true,
                        ["EnableSyntaxHighlighting"] = true
                    }
                },
                new PanelConfiguration
                {
                    Name = "AssetBrowser",
                    Type = PanelType.AssetBrowser,
                    Position = new Position(60, 0),
                    Size = new Size(40, 15),
                    IsVisible = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["ShowPreview"] = true,
                        ["EnableFiltering"] = true,
                        ["SortBy"] = "Name"
                    }
                },
                new PanelConfiguration
                {
                    Name = "Inspector",
                    Type = PanelType.Inspector,
                    Position = new Position(60, 15),
                    Size = new Size(40, 15),
                    IsVisible = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["ShowMetadata"] = true,
                        ["EditableProperties"] = true
                    }
                }
            },
            KeyBindings = CreateEditorModeKeyBindings(),
            Theme = new ThemeConfiguration
            {
                PrimaryColor = "Blue",
                BackgroundColor = "DarkBlue",
                TextColor = "LightGray",
                Properties = new Dictionary<string, object>
                {
                    ["HighlightColor"] = "Cyan",
                    ["ErrorColor"] = "Red",
                    ["SuccessColor"] = "Green"
                }
            },
            CustomProperties = new Dictionary<string, object>
            {
                ["Mode"] = "Editor",
                ["EnableAutosave"] = true,
                ["ShowGrid"] = true,
                ["EnableSnapping"] = true
            }
        };
    }

    private static KeyBindingSet CreateEditorModeKeyBindings()
    {
        var keyBindings = new KeyBindingSet();
        
        // Editor workflow hotkeys
        keyBindings.Add("Ctrl+N", "create");
        keyBindings.Add("Ctrl+O", "import");
        keyBindings.Add("Ctrl+E", "export");
        keyBindings.Add("Ctrl+B", "build");
        keyBindings.Add("Ctrl+D", "deploy");
        
        // Quick access keys
        keyBindings.Add("F7", "build");
        keyBindings.Add("Ctrl+F7", "deploy");
        
        return keyBindings;
    }

    public override Task<bool> CanActivateAsync(CancellationToken cancellationToken = default)
    {
        // Editor profile can always be activated
        return Task.FromResult(true);
    }

    public override Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Editor Mode Profile activated");
        Console.WriteLine("Available commands: create, import, export, build, deploy");
        Console.WriteLine("Use Ctrl+N to create, Ctrl+B to build, F7 for quick build");
        
        // Here you would initialize editor-specific services
        // e.g., asset browser, inspector, project management, etc.
        
        return Task.CompletedTask;
    }

    public override Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Editor Mode Profile deactivated");
        
        // Here you would cleanup editor-specific resources
        // e.g., save project state, close asset browsers, etc.
        
        return Task.CompletedTask;
    }
}