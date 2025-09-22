namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines the operational modes for the GameConsole UI system.
/// Each mode provides specialized interface configurations optimized for specific workflows.
/// </summary>
public enum ConsoleMode
{
    /// <summary>
    /// Game mode optimized for runtime operations, debugging, and player interaction.
    /// Provides streamlined access to runtime commands and performance monitoring tools.
    /// </summary>
    Game = 0,

    /// <summary>
    /// Editor mode optimized for content creation, asset management, and development workflows.
    /// Provides comprehensive creation tools and development-focused interface elements.
    /// </summary>
    Editor = 1
}