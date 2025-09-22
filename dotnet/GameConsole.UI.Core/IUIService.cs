using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Main UI service interface for the GameConsole UI system.
/// Provides access to UI components and rendering capabilities.
/// </summary>
public interface IUIService : IService
{
    /// <summary>
    /// The console rendering service for drawing UI components.
    /// </summary>
    IConsoleRenderer? ConsoleRenderer { get; }
    
    /// <summary>
    /// Window management capabilities.
    /// </summary>
    IWindowManager? WindowManager { get; }
    
    /// <summary>
    /// Layout management capabilities.
    /// </summary>
    ILayoutManager? LayoutManager { get; }
}