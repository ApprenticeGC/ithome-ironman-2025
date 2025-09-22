namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines the different console modes supported by the UI Profile system.
/// Each mode provides specialized UI configurations and command sets.
/// </summary>
public enum ConsoleMode
{
    /// <summary>
    /// Game mode - optimized for runtime operations, debugging, and player interaction.
    /// </summary>
    Game,
    
    /// <summary>
    /// Editor mode - optimized for content creation, asset management, and development workflows.
    /// </summary>
    Editor
}