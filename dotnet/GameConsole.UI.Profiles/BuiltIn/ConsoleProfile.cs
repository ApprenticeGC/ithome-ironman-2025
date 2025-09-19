using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles.BuiltIn;

/// <summary>
/// Basic console profile optimized for minimal terminal interface.
/// Provides essential commands and a simple text-based layout.
/// </summary>
public class ConsoleProfile : UIProfile
{
    public ConsoleProfile(ILogger? logger = null) 
        : base("Console", ConsoleMode.Console, logger)
    {
        Metadata = new UIProfileMetadata
        {
            Version = "1.0.0",
            Author = "GameConsole",
            Description = "Basic terminal console interface with minimal UI elements",
            Tags = new[] { "console", "terminal", "minimal", "text" },
            IsBuiltIn = true,
            Priority = 100,
            CompatibleModes = new[] { ConsoleMode.Console }
        };
    }

    public override CommandSet GetCommandSet()
    {
        var commandSet = CreateDefaultCommandSet();

        // Console-specific commands
        commandSet.AddCommand("clear", new CommandDefinition
        {
            Category = "Console",
            Description = "Clear the console screen",
            Priority = 900,
            KeyboardShortcuts = new[] { "Ctrl+L" }
        });

        commandSet.AddCommand("history", new CommandDefinition
        {
            Category = "Console",
            Description = "Show command history",
            Priority = 850,
            KeyboardShortcuts = new[] { "Ctrl+H" }
        });

        commandSet.AddCommand("echo", new CommandDefinition
        {
            Category = "Console",
            Description = "Print text to console",
            Priority = 800
        });

        commandSet.AddCommand("ls", new CommandDefinition
        {
            Category = "File",
            Description = "List directory contents",
            Priority = 750
        });

        commandSet.AddCommand("cd", new CommandDefinition
        {
            Category = "File",
            Description = "Change current directory",
            Priority = 749
        });

        commandSet.AddCommand("pwd", new CommandDefinition
        {
            Category = "File",
            Description = "Print current working directory",
            Priority = 748
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
                    Name = "Console",
                    Type = "Console",
                    Position = PanelPosition.Center,
                    Size = "100%",
                    IsVisible = true,
                    IsResizable = false,
                    IsClosable = false
                }
            },
            Theme = new ThemeConfiguration
            {
                ColorScheme = "Terminal",
                FontFamily = "Consolas",
                FontSize = 14
            },
            Window = new WindowConfiguration
            {
                Width = 800,
                Height = 600,
                IsResizable = true,
                StartMaximized = false
            },
            Navigation = new NavigationConfiguration
            {
                ShowMenuBar = false,
                ShowToolbar = false,
                ShowStatusBar = true,
                Shortcuts = new Dictionary<string, string>
                {
                    ["Ctrl+C"] = "interrupt",
                    ["Tab"] = "autocomplete",
                    ["Up"] = "history_prev",
                    ["Down"] = "history_next"
                }.AsReadOnly()
            }
        };
    }

    public override IReadOnlyDictionary<string, string> GetServiceProviderConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["UI"] = "Terminal.UI.Provider",
            ["Input"] = "Console.Input.Provider",
            ["Graphics"] = "Text.Graphics.Provider"
        }.AsReadOnly();
    }
}