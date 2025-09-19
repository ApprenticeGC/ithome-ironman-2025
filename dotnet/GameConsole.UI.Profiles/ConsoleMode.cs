namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents the different operational modes that GameConsole can operate in.
/// Each mode provides different UI configurations, commands, and behaviors.
/// </summary>
public enum ConsoleMode
{
    /// <summary>
    /// Game mode - optimized for runtime operations, debugging, and player interaction.
    /// </summary>
    Game = 0,

    /// <summary>
    /// Editor mode - optimized for content creation, asset management, and development workflows.
    /// </summary>
    Editor = 1,

    /// <summary>
    /// Console mode - basic terminal interface with minimal UI elements.
    /// </summary>
    Console = 2,

    /// <summary>
    /// Web mode - optimized for web-based interfaces and remote access.
    /// </summary>
    Web = 3,

    /// <summary>
    /// Desktop mode - full desktop application interface with windowed UI.
    /// </summary>
    Desktop = 4
}