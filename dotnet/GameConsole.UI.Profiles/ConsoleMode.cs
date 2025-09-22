namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents the different console operation modes that determine UI behavior and available features.
/// Each mode provides a distinct user experience optimized for specific workflows.
/// </summary>
public enum ConsoleMode
{
    /// <summary>
    /// Game runtime mode optimized for gameplay, debugging, and player interaction.
    /// Provides runtime commands, debug tools, player interface, and performance monitoring UI.
    /// </summary>
    Game,

    /// <summary>
    /// Editor mode optimized for content creation, asset management, and development workflows.
    /// Provides creation tools, asset management, project tools, and workflow helpers.
    /// </summary>
    Editor,

    /// <summary>
    /// Web-based interface mode for remote access and browser-based interactions.
    /// Provides web-optimized UI components and remote access capabilities.
    /// </summary>
    Web,

    /// <summary>
    /// Desktop application mode with native desktop UI components and integrations.
    /// Provides native desktop experience with system integration features.
    /// </summary>
    Desktop
}