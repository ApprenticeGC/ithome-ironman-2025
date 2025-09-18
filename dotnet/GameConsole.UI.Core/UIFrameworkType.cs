namespace GameConsole.UI.Core;

/// <summary>
/// Represents the framework types supported by the UI abstraction layer.
/// </summary>
public enum UIFrameworkType
{
    /// <summary>
    /// Console-based text user interface.
    /// </summary>
    Console,
    
    /// <summary>
    /// Web-based user interface using HTML/CSS/JavaScript.
    /// </summary>
    Web,
    
    /// <summary>
    /// Desktop GUI user interface using native windowing systems.
    /// </summary>
    Desktop
}

/// <summary>
/// Capabilities that a UI framework can support.
/// </summary>
[Flags]
public enum UICapabilities
{
    /// <summary>
    /// No capabilities.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Text input capabilities.
    /// </summary>
    TextInput = 1 << 0,
    
    /// <summary>
    /// File selection capabilities.
    /// </summary>
    FileSelection = 1 << 1,
    
    /// <summary>
    /// Progress display capabilities.
    /// </summary>
    ProgressDisplay = 1 << 2,
    
    /// <summary>
    /// Interactive navigation capabilities.
    /// </summary>
    InteractiveNavigation = 1 << 3,
    
    /// <summary>
    /// Real-time updates capabilities.
    /// </summary>
    RealTimeUpdates = 1 << 4,
    
    /// <summary>
    /// Keyboard shortcuts capabilities.
    /// </summary>
    KeyboardShortcuts = 1 << 5,
    
    /// <summary>
    /// Mouse interaction capabilities.
    /// </summary>
    MouseInteraction = 1 << 6,
    
    /// <summary>
    /// Color display capabilities.
    /// </summary>
    ColorDisplay = 1 << 7,
    
    /// <summary>
    /// Form input capabilities.
    /// </summary>
    FormInput = 1 << 8,
    
    /// <summary>
    /// Table display capabilities.
    /// </summary>
    TableDisplay = 1 << 9,
    
    /// <summary>
    /// Responsive design capabilities.
    /// </summary>
    ResponsiveDesign = 1 << 10,
    
    /// <summary>
    /// Accessibility features capabilities.
    /// </summary>
    Accessibility = 1 << 11
}