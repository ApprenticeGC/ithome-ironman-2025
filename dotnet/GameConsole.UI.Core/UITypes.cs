namespace GameConsole.UI.Core;

/// <summary>
/// Defines the type of UI framework being used.
/// </summary>
public enum UIFrameworkType
{
    /// <summary>
    /// Console-based text user interface (TUI).
    /// </summary>
    Console,

    /// <summary>
    /// Web-based user interface using HTML/CSS/JavaScript.
    /// </summary>
    Web,

    /// <summary>
    /// Desktop GUI application (WPF, WinUI, etc.).
    /// </summary>
    Desktop,

    /// <summary>
    /// Mobile application interface.
    /// </summary>
    Mobile,

    /// <summary>
    /// API-only interface (no visual components).
    /// </summary>
    Headless
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
    /// Text input and editing.
    /// </summary>
    TextInput = 1 << 0,

    /// <summary>
    /// File selection dialogs.
    /// </summary>
    FileSelection = 1 << 1,

    /// <summary>
    /// Progress display (progress bars, spinners).
    /// </summary>
    ProgressDisplay = 1 << 2,

    /// <summary>
    /// Interactive navigation (menus, links, tabs).
    /// </summary>
    InteractiveNavigation = 1 << 3,

    /// <summary>
    /// Real-time updates without user action.
    /// </summary>
    RealTimeUpdates = 1 << 4,

    /// <summary>
    /// Keyboard shortcuts and hotkeys.
    /// </summary>
    KeyboardShortcuts = 1 << 5,

    /// <summary>
    /// Mouse interaction (clicking, dragging).
    /// </summary>
    MouseInteraction = 1 << 6,

    /// <summary>
    /// Color display and styling.
    /// </summary>
    ColorDisplay = 1 << 7,

    /// <summary>
    /// Form input elements (checkboxes, radio buttons, etc.).
    /// </summary>
    FormInput = 1 << 8,

    /// <summary>
    /// Table display and sorting.
    /// </summary>
    TableDisplay = 1 << 9,

    /// <summary>
    /// Image and media display.
    /// </summary>
    MediaDisplay = 1 << 10,

    /// <summary>
    /// Audio output capabilities.
    /// </summary>
    AudioOutput = 1 << 11,

    /// <summary>
    /// Drag and drop functionality.
    /// </summary>
    DragAndDrop = 1 << 12,

    /// <summary>
    /// Multi-window or panel support.
    /// </summary>
    MultiWindow = 1 << 13,

    /// <summary>
    /// Touch input for mobile interfaces.
    /// </summary>
    TouchInput = 1 << 14,

    /// <summary>
    /// Accessibility features (screen readers, high contrast, etc.).
    /// </summary>
    Accessibility = 1 << 15
}

/// <summary>
/// Responsive behavior modes for UI components.
/// </summary>
public enum UIResponsiveMode
{
    /// <summary>
    /// Fixed layout that doesn't adapt to screen size.
    /// </summary>
    Fixed,

    /// <summary>
    /// Adaptive layout that adjusts to available space.
    /// </summary>
    Adaptive,

    /// <summary>
    /// Responsive layout that changes based on screen size breakpoints.
    /// </summary>
    Responsive,

    /// <summary>
    /// Fluid layout that scales continuously.
    /// </summary>
    Fluid
}

/// <summary>
/// Component lifecycle states.
/// </summary>
public enum UIComponentState
{
    /// <summary>
    /// Component is not initialized.
    /// </summary>
    Uninitialized,

    /// <summary>
    /// Component is initialized but not rendered.
    /// </summary>
    Initialized,

    /// <summary>
    /// Component is mounted and rendered.
    /// </summary>
    Mounted,

    /// <summary>
    /// Component is being updated.
    /// </summary>
    Updating,

    /// <summary>
    /// Component is unmounted and disposed.
    /// </summary>
    Unmounted
}

/// <summary>
/// Priority levels for UI updates.
/// </summary>
public enum UIUpdatePriority
{
    /// <summary>
    /// Low priority, can be deferred.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority for regular updates.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority for user interactions.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority for immediate updates.
    /// </summary>
    Critical
}