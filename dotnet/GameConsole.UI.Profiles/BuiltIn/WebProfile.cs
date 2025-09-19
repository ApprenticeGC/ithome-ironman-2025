using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles.BuiltIn;

/// <summary>
/// Web profile optimized for browser-based interfaces and remote access.
/// Lightweight UI suitable for web deployment and remote management.
/// </summary>
public class WebProfile : UIProfile
{
    public WebProfile(ILogger? logger = null) 
        : base("Web", ConsoleMode.Web, logger)
    {
        Metadata = new UIProfileMetadata
        {
            Version = "1.0.0",
            Author = "GameConsole",
            Description = "Web-optimized interface for browser deployment and remote access",
            Tags = new[] { "web", "browser", "remote", "lightweight" },
            IsBuiltIn = true,
            Priority = 150,
            CompatibleModes = new[] { ConsoleMode.Web }
        };
    }

    public override CommandSet GetCommandSet()
    {
        var commandSet = CreateDefaultCommandSet();

        // Web-optimized commands
        commandSet.AddCommand("refresh", new CommandDefinition
        {
            Category = "Web",
            Description = "Refresh the interface",
            Priority = 950,
            KeyboardShortcuts = new[] { "F5", "Ctrl+R" }
        });

        commandSet.AddCommand("fullscreen", new CommandDefinition
        {
            Category = "Web",
            Description = "Toggle fullscreen mode",
            Priority = 900,
            KeyboardShortcuts = new[] { "F11" }
        });

        commandSet.AddCommand("share", new CommandDefinition
        {
            Category = "Web",
            Description = "Share current session",
            Priority = 850
        });

        commandSet.AddCommand("download", new CommandDefinition
        {
            Category = "File",
            Description = "Download file or resource",
            Priority = 800,
            KeyboardShortcuts = new[] { "Ctrl+D" }
        });

        // Remote management commands
        commandSet.AddCommand("connect", new CommandDefinition
        {
            Category = "Remote",
            Description = "Connect to remote instance",
            Priority = 780
        });

        commandSet.AddCommand("disconnect", new CommandDefinition
        {
            Category = "Remote",
            Description = "Disconnect from remote instance",
            Priority = 779
        });

        commandSet.AddCommand("status", new CommandDefinition
        {
            Category = "Remote",
            Description = "Show connection status",
            Priority = 778
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
                    Name = "MainView",
                    Type = "WebView",
                    Position = PanelPosition.Center,
                    Size = "100%",
                    IsVisible = true,
                    IsResizable = false,
                    IsClosable = false
                }
            },
            Theme = new ThemeConfiguration
            {
                ColorScheme = "WebLight",
                FontFamily = "Arial",
                FontSize = 14,
                Scale = 1.0f
            },
            Window = new WindowConfiguration
            {
                Width = 1024,
                Height = 768,
                IsResizable = true,
                StartMaximized = false
            },
            Navigation = new NavigationConfiguration
            {
                ShowMenuBar = false,
                ShowToolbar = true,
                ShowStatusBar = false,
                Shortcuts = new Dictionary<string, string>
                {
                    ["Ctrl+Shift+I"] = "dev_tools",
                    ["Ctrl+U"] = "view_source",
                    ["Ctrl+Shift+R"] = "hard_refresh"
                }.AsReadOnly()
            }
        };
    }

    public override IReadOnlyDictionary<string, string> GetServiceProviderConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["UI"] = "Web.UI.Provider",
            ["Input"] = "Web.Input.Provider",
            ["Graphics"] = "Web.Graphics.Provider",
            ["Network"] = "Web.Network.Provider",
            ["Storage"] = "Web.Storage.Provider"
        }.AsReadOnly();
    }
}