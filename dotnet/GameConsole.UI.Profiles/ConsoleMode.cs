namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines the different operational modes that the GameConsole can operate in.
/// Each mode provides specialized UI experiences and capabilities.
/// </summary>
public enum ConsoleMode
{
    /// <summary>
    /// Game mode - optimized for runtime operations, debugging, and player interaction.
    /// Provides tools and interfaces focused on gameplay execution and real-time monitoring.
    /// </summary>
    Game,

    /// <summary>
    /// Editor mode - optimized for content creation, asset management, and development workflows.
    /// Provides comprehensive authoring tools and asset pipeline management.
    /// </summary>
    Editor
}