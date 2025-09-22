namespace GameConsole.UI.Core;

/// <summary>
/// Defines the types of UI frameworks supported by the abstraction layer.
/// </summary>
public enum FrameworkType
{
    /// <summary>
    /// Console-based text user interface.
    /// </summary>
    Console,
    
    /// <summary>
    /// Web browser-based user interface.
    /// </summary>
    Web,
    
    /// <summary>
    /// Desktop application user interface.
    /// </summary>
    Desktop
}

/// <summary>
/// Defines the types of UI components available across frameworks.
/// </summary>
public enum ComponentType
{
    /// <summary>
    /// A clickable button component.
    /// </summary>
    Button,
    
    /// <summary>
    /// A text input field component.
    /// </summary>
    TextInput,
    
    /// <summary>
    /// A text label component.
    /// </summary>
    Label,
    
    /// <summary>
    /// A container panel component.
    /// </summary>
    Panel,
    
    /// <summary>
    /// A menu component with options.
    /// </summary>
    Menu,
    
    /// <summary>
    /// A list component for displaying items.
    /// </summary>
    List,
    
    /// <summary>
    /// A table component for tabular data.
    /// </summary>
    Table,
    
    /// <summary>
    /// A progress indicator component.
    /// </summary>
    ProgressBar,
    
    /// <summary>
    /// A checkbox component for boolean selections.
    /// </summary>
    Checkbox,
    
    /// <summary>
    /// A radio button component for exclusive selections.
    /// </summary>
    RadioButton,
    
    /// <summary>
    /// A dropdown selection component.
    /// </summary>
    Dropdown,
    
    /// <summary>
    /// A modal dialog component.
    /// </summary>
    Dialog
}

/// <summary>
/// Defines capabilities that UI frameworks may support.
/// </summary>
[Flags]
public enum UICapabilities
{
    /// <summary>
    /// No special capabilities.
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
    /// Keyboard shortcuts support.
    /// </summary>
    KeyboardShortcuts = 1 << 5,
    
    /// <summary>
    /// Mouse interaction support.
    /// </summary>
    MouseInteraction = 1 << 6,
    
    /// <summary>
    /// Color display support.
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
    /// Responsive design support.
    /// </summary>
    ResponsiveDesign = 1 << 10,
    
    /// <summary>
    /// Accessibility features support.
    /// </summary>
    Accessibility = 1 << 11,
    
    /// <summary>
    /// Styling and theming support.
    /// </summary>
    Styling = 1 << 12
}

/// <summary>
/// Defines the current mode of the UI context.
/// </summary>
public enum UIMode
{
    /// <summary>
    /// Command-line interface mode.
    /// </summary>
    CLI,
    
    /// <summary>
    /// Terminal user interface mode.
    /// </summary>
    TUI,
    
    /// <summary>
    /// Web interface mode.
    /// </summary>
    Web,
    
    /// <summary>
    /// Desktop GUI mode.
    /// </summary>
    Desktop
}