using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameConsole.UI.Core;

/// <summary>
/// Game mode UI profile optimized for runtime gameplay operations
/// </summary>
public class GameProfile : UIProfile
{
    public GameProfile()
    {
        Name = "Game";
        TargetMode = ConsoleMode.Game;
        SupportedCapabilities = UICapabilities.TextDisplay | 
                               UICapabilities.KeyboardShortcuts | 
                               UICapabilities.StatusBar |
                               UICapabilities.ColorDisplay;
        Metadata = new UIProfileMetadata(
            Description: "Game mode profile for runtime operations",
            Version: "1.0.0",
            Author: "GameConsole",
            IsBuiltIn: true);
    }

    public override CommandSet GetCommandSet()
    {
        var commands = new List<UICommand>
        {
            new UICommand("play", "Start or resume gameplay", HandlePlayCommand),
            new UICommand("pause", "Pause current game", HandlePauseCommand),
            new UICommand("debug", "Toggle debug mode", HandleDebugCommand),
            new UICommand("stats", "Show game statistics", HandleStatsCommand),
            new UICommand("quit", "Exit the game", HandleQuitCommand, RequiresConfirmation: true),
            new UICommand("help", "Show available commands", HandleHelpCommand)
        };

        var aliases = new Dictionary<string, string>
        {
            { "p", "play" },
            { "d", "debug" },
            { "s", "stats" },
            { "q", "quit" },
            { "h", "help" }
        };

        return new CommandSet(commands, aliases, "help");
    }

    public override LayoutConfiguration GetLayoutConfiguration()
    {
        return new LayoutConfiguration(
            Layout: LayoutType.SingleColumn,
            Columns: 1,
            ShowStatusBar: true,
            ShowMenuBar: false,
            StatusFormat: "GAME | {time} | {status}");
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
        // Game profile specific configuration
    }

    private static Task<UICommandResult> HandlePlayCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Game started"));
    }

    private static Task<UICommandResult> HandlePauseCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Game paused"));
    }

    private static Task<UICommandResult> HandleDebugCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Debug mode toggled"));
    }

    private static Task<UICommandResult> HandleStatsCommand(UIContext context)
    {
        var data = new Dictionary<string, object>
        {
            { "fps", 60 },
            { "memory", "256MB" },
            { "uptime", TimeSpan.FromMinutes(15) }
        };
        return Task.FromResult(new UICommandResult(true, "Game statistics retrieved", data));
    }

    private static Task<UICommandResult> HandleQuitCommand(UIContext context)
    {
        return Task.FromResult(new UICommandResult(true, "Exiting game..."));
    }

    private Task<UICommandResult> HandleHelpCommand(UIContext context)
    {
        var commands = GetCommandSet().Commands;
        var helpText = string.Join("\n", commands.Select(c => $"{c.Name} - {c.Description}"));
        return Task.FromResult(new UICommandResult(true, helpText));
    }
}