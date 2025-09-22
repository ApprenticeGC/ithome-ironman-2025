using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Services;

/// <summary>
/// Core UI service interface for console-based user interface operations.
/// Provides unified interface for TUI (Text-based User Interface) rendering and interaction.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region Rendering Operations
    
    /// <summary>
    /// Begins a new UI render frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BeginRenderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ends the current UI render frame and presents to console.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EndRenderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears the console screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearScreenAsync(CancellationToken cancellationToken = default);
    
    #endregion

    #region Element Management
    
    /// <summary>
    /// Creates a new UI element and adds it to the UI hierarchy.
    /// </summary>
    /// <typeparam name="T">Type of UI element to create.</typeparam>
    /// <param name="element">The UI element to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task CreateElementAsync<T>(T element, CancellationToken cancellationToken = default) where T : Core.UIElement;
    
    /// <summary>
    /// Updates an existing UI element.
    /// </summary>
    /// <param name="elementId">ID of the element to update.</param>
    /// <param name="element">Updated element data.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateElementAsync(string elementId, Core.UIElement element, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a UI element from the hierarchy.
    /// </summary>
    /// <param name="elementId">ID of the element to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveElementAsync(string elementId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a UI element by its ID.
    /// </summary>
    /// <param name="elementId">ID of the element to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The UI element or null if not found.</returns>
    Task<Core.UIElement?> GetElementAsync(string elementId, CancellationToken cancellationToken = default);
    
    #endregion

    #region Focus and Input
    
    /// <summary>
    /// Sets focus to a specific UI element.
    /// </summary>
    /// <param name="elementId">ID of the element to focus.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFocusAsync(string elementId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the currently focused element ID.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The focused element ID or null if no element has focus.</returns>
    Task<string?> GetFocusedElementAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes input for UI interactions.
    /// </summary>
    /// <param name="inputData">Input data to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ProcessInputAsync(UIInputData inputData, CancellationToken cancellationToken = default);
    
    #endregion

    #region Console Properties
    
    /// <summary>
    /// Gets the current console size.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current console dimensions.</returns>
    Task<Core.UISize> GetConsoleSizeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets console colors for rendering.
    /// </summary>
    /// <param name="foreground">Foreground color.</param>
    /// <param name="background">Background color.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetConsoleColorsAsync(Core.UIColor foreground, Core.UIColor background, CancellationToken cancellationToken = default);
    
    #endregion

    #region Events
    
    /// <summary>
    /// Event raised when UI focus changes.
    /// </summary>
    event EventHandler<Core.UIFocusEvent>? FocusChanged;

    /// <summary>
    /// Event raised when a UI element is activated.
    /// </summary>
    event EventHandler<Core.UIActivationEvent>? ElementActivated;

    /// <summary>
    /// Event raised when UI layout changes.
    /// </summary>
    event EventHandler<Core.UILayoutEvent>? LayoutChanged;

    /// <summary>
    /// Event raised when text input occurs.
    /// </summary>
    event EventHandler<Core.UITextInputEvent>? TextInput;

    /// <summary>
    /// Event raised when UI element state changes.
    /// </summary>
    event EventHandler<Core.UIStateChangeEvent>? StateChanged;
    
    #endregion

    #region Capability Access
    
    /// <summary>
    /// Gets the theme manager capability.
    /// </summary>
    IThemeManagerCapability? ThemeManager { get; }
    
    /// <summary>
    /// Gets the layout manager capability.
    /// </summary>
    ILayoutManagerCapability? LayoutManager { get; }
    
    /// <summary>
    /// Gets the animation capability.
    /// </summary>
    IAnimationCapability? AnimationManager { get; }

    #endregion
}

/// <summary>
/// Input data for UI processing.
/// </summary>
public class UIInputData
{
    /// <summary>
    /// Type of input.
    /// </summary>
    public UIInputType InputType { get; set; }

    /// <summary>
    /// Key code for keyboard input.
    /// </summary>
    public ConsoleKey? Key { get; set; }

    /// <summary>
    /// Character for text input.
    /// </summary>
    public char? Character { get; set; }

    /// <summary>
    /// Mouse position for mouse input.
    /// </summary>
    public Core.UIPosition? MousePosition { get; set; }

    /// <summary>
    /// Modifier keys pressed during input.
    /// </summary>
    public UIInputModifiers Modifiers { get; set; }
}

/// <summary>
/// Types of UI input.
/// </summary>
public enum UIInputType
{
    KeyPress,
    KeyRelease,
    Character,
    MouseClick,
    MouseMove
}

/// <summary>
/// UI input modifier flags.
/// </summary>
[Flags]
public enum UIInputModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4
}

/// <summary>
/// Capability interface for theme management operations.
/// </summary>
public interface IThemeManagerCapability : ICapabilityProvider
{
    /// <summary>
    /// Sets the current UI theme.
    /// </summary>
    /// <param name="theme">Theme configuration to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetThemeAsync(UITheme theme, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current UI theme.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current theme configuration.</returns>
    Task<UITheme> GetThemeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available themes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>List of available themes.</returns>
    Task<IEnumerable<UITheme>> GetAvailableThemesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for layout management operations.
/// </summary>
public interface ILayoutManagerCapability : ICapabilityProvider
{
    /// <summary>
    /// Performs automatic layout of UI elements.
    /// </summary>
    /// <param name="containerId">Container element to layout.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PerformLayoutAsync(string containerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculates optimal size for a UI element.
    /// </summary>
    /// <param name="elementId">Element to calculate size for.</param>
    /// <param name="availableSize">Available space for the element.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Optimal size for the element.</returns>
    Task<Core.UISize> CalculateOptimalSizeAsync(string elementId, Core.UISize availableSize, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for UI animations.
/// </summary>
public interface IAnimationCapability : ICapabilityProvider
{
    /// <summary>
    /// Animates a UI element property over time.
    /// </summary>
    /// <param name="elementId">Element to animate.</param>
    /// <param name="propertyName">Property to animate.</param>
    /// <param name="targetValue">Target value for the property.</param>
    /// <param name="duration">Animation duration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AnimatePropertyAsync(string elementId, string propertyName, object targetValue, TimeSpan duration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops all animations for a specific element.
    /// </summary>
    /// <param name="elementId">Element to stop animations for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopAnimationsAsync(string elementId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a UI theme configuration.
/// </summary>
public class UITheme
{
    /// <summary>
    /// Theme name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default foreground color.
    /// </summary>
    public Core.UIColor DefaultForeground { get; set; } = Core.UIColor.White;

    /// <summary>
    /// Default background color.
    /// </summary>
    public Core.UIColor DefaultBackground { get; set; } = Core.UIColor.Black;

    /// <summary>
    /// Focused element colors.
    /// </summary>
    public Core.UIColor FocusedForeground { get; set; } = Core.UIColor.Black;
    public Core.UIColor FocusedBackground { get; set; } = Core.UIColor.White;

    /// <summary>
    /// Disabled element colors.
    /// </summary>
    public Core.UIColor DisabledForeground { get; set; } = Core.UIColor.DarkGray;
    public Core.UIColor DisabledBackground { get; set; } = Core.UIColor.Black;
}