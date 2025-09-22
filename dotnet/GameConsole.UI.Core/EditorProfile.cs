using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameConsole.UI.Core;

/// <summary>
/// Editor mode UI profile optimized for content creation and asset management
/// </summary>
public class EditorProfile : UIProfile
{
    public EditorProfile()
    {
        Name = "Editor";
        TargetMode = ConsoleMode.Editor;
        SupportedCapabilities = UICapabilities.TextDisplay | 
                               UICapabilities.MenuNavigation |
                               UICapabilities.FormInput |
                               UICapabilities.TableDisplay |
                               UICapabilities.TreeView |
                               UICapabilities.KeyboardShortcuts | 
                               UICapabilities.MouseInteraction |
                               UICapabilities.ColorDisplay |
                               UICapabilities.ProgressIndicators |
                               UICapabilities.StatusBar;
        Metadata = new UIProfileMetadata(
            Description: "Editor mode profile for content creation and development",
            Version: "1.0.0",
            Author: "GameConsole",
            IsBuiltIn: true);
    }

    public override CommandSet GetCommandSet()
    {
        var commands = new List<UICommand>
        {
            new UICommand("create", "Create new asset or project", HandleCreateCommand),
            new UICommand("edit", "Edit existing asset", HandleEditCommand),
            new UICommand("build", "Build project or assets", HandleBuildCommand),
            new UICommand("test", "Run tests", HandleTestCommand),
            new UICommand("deploy", "Deploy project", HandleDeployCommand),
            new UICommand("import", "Import external assets", HandleImportCommand),
            new UICommand("export", "Export assets", HandleExportCommand),
            new UICommand("validate", "Validate project integrity", HandleValidateCommand),
            new UICommand("help", "Show available commands", HandleHelpCommand),
            new UICommand("exit", "Exit editor mode", HandleExitCommand)
        };

        var aliases = new Dictionary<string, string>
        {
            { "new", "create" },
            { "c", "create" },
            { "e", "edit" },
            { "b", "build" },
            { "t", "test" },
            { "d", "deploy" },
            { "i", "import" },
            { "x", "export" },
            { "v", "validate" },
            { "h", "help" },
            { "q", "exit" }
        };

        return new CommandSet(commands, aliases, "help");
    }

    public override LayoutConfiguration GetLayoutConfiguration()
    {
        return new LayoutConfiguration(
            Layout: LayoutType.TwoColumn,
            Columns: 2,
            ShowStatusBar: true,
            ShowMenuBar: true,
            StatusFormat: "EDITOR | {time} | {project} | {mode}");
    }

    public override async Task<UICommandResult> HandleCommandAsync(string command, UIContext context)
    {
        var commandSet = GetCommandSet();
        var cmd = commandSet.Commands.FirstOrDefault(c => c.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
        
        if (cmd != null)
        {
            return await cmd.Handler(context);
        }

        // Check aliases
        if (commandSet.Aliases.TryGetValue(command.ToLowerInvariant(), out var aliasTarget))
        {
            return await HandleCommandAsync(aliasTarget, context);
        }

        return new UICommandResult(false, $"Unknown command: {command}");
    }

    public override void Configure(UIConfiguration config)
    {
        // Editor profile specific configuration
    }

    private static Task<UICommandResult> HandleCreateCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Asset creation wizard started"));
    }

    private static Task<UICommandResult> HandleEditCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Asset editor opened"));
    }

    private static Task<UICommandResult> HandleBuildCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Build process started"));
    }

    private static Task<UICommandResult> HandleTestCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Test suite executed"));
    }

    private static Task<UICommandResult> HandleDeployCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Deployment initiated"));
    }

    private static Task<UICommandResult> HandleImportCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Import wizard opened"));
    }

    private static Task<UICommandResult> HandleExportCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Export process started"));
    }

    private static Task<UICommandResult> HandleValidateCommand(UIContext context)
    {
        var data = new Dictionary<string, object>
        {
            { "errors", 0 },
            { "warnings", 2 },
            { "assets_checked", 45 }
        };
        return Task.FromResult(new UICommandResult(true, "Project validation completed", data));
    }

    private Task<UICommandResult> HandleHelpCommand(UIContext context)
    {
        var commands = GetCommandSet().Commands;
        var helpText = string.Join("\n", commands.Select(c => $"{c.Name} - {c.Description}"));
        return Task.FromResult(new UICommandResult(true, helpText));
    }

    private static Task<UICommandResult> HandleExitCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Exiting editor mode..."));
    }
}