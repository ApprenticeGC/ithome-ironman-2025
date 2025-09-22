namespace GameConsole.UI.Console;

/// <summary>
/// Console buffer interface for virtual console rendering.
/// </summary>
public interface IConsoleBuffer
{
    /// <summary>
    /// Gets the buffer width.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the buffer height.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Writes text to the buffer at the specified position.
    /// </summary>
    /// <param name="x">X coordinate (column).</param>
    /// <param name="y">Y coordinate (row).</param>
    /// <param name="text">Text to write.</param>
    /// <param name="style">Text style.</param>
    void WriteAt(int x, int y, string text, ConsoleTextStyle style = default);

    /// <summary>
    /// Writes a single character to the buffer at the specified position.
    /// </summary>
    /// <param name="x">X coordinate (column).</param>
    /// <param name="y">Y coordinate (row).</param>
    /// <param name="character">Character to write.</param>
    /// <param name="style">Text style.</param>
    void WriteCharAt(int x, int y, char character, ConsoleTextStyle style = default);

    /// <summary>
    /// Fills a rectangular area with the specified character and style.
    /// </summary>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Starting Y coordinate.</param>
    /// <param name="width">Width of the area.</param>
    /// <param name="height">Height of the area.</param>
    /// <param name="character">Character to fill with.</param>
    /// <param name="style">Text style.</param>
    void FillArea(int x, int y, int width, int height, char character = ' ', ConsoleTextStyle style = default);

    /// <summary>
    /// Draws a border around the specified area.
    /// </summary>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Starting Y coordinate.</param>
    /// <param name="width">Width of the border area.</param>
    /// <param name="height">Height of the border area.</param>
    /// <param name="borderStyle">Border style.</param>
    /// <param name="style">Text style for the border.</param>
    void DrawBorder(int x, int y, int width, int height, ConsoleBorderStyle borderStyle = ConsoleBorderStyle.Single, ConsoleTextStyle style = default);

    /// <summary>
    /// Clears the entire buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Clears a specific area of the buffer.
    /// </summary>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Starting Y coordinate.</param>
    /// <param name="width">Width of the area to clear.</param>
    /// <param name="height">Height of the area to clear.</param>
    void ClearArea(int x, int y, int width, int height);

    /// <summary>
    /// Gets the character at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>Character at the position.</returns>
    char GetCharAt(int x, int y);

    /// <summary>
    /// Gets the style at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>Style at the position.</returns>
    ConsoleTextStyle GetStyleAt(int x, int y);

    /// <summary>
    /// Copies the buffer contents to the physical console.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes the buffer.
    /// </summary>
    /// <param name="width">New width.</param>
    /// <param name="height">New height.</param>
    void Resize(int width, int height);
}

/// <summary>
/// Console input manager interface for handling keyboard and mouse input.
/// </summary>
public interface IConsoleInputManager
{
    /// <summary>
    /// Gets whether the input manager is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Starts input monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops input monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next input event.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The next input event, or null if no input is available.</returns>
    Task<ConsoleInputEvent?> GetNextInputAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific key is currently pressed.
    /// </summary>
    /// <param name="key">Key to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    Task<bool> IsKeyPressedAsync(Input.Core.KeyCode key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current mouse position in console coordinates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Mouse position in console coordinates.</returns>
    Task<ConsolePosition> GetMousePositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when input is received.
    /// </summary>
    event EventHandler<ConsoleInputEvent>? InputReceived;

    /// <summary>
    /// Event raised when a key is pressed.
    /// </summary>
    event EventHandler<ConsoleInputEvent>? KeyPressed;

    /// <summary>
    /// Event raised when text is input.
    /// </summary>
    event EventHandler<ConsoleInputEvent>? TextInput;

    /// <summary>
    /// Event raised when mouse input is received.
    /// </summary>
    event EventHandler<ConsoleInputEvent>? MouseInput;
}

/// <summary>
/// Console layout manager interface for organizing UI components.
/// </summary>
public interface IConsoleLayoutManager
{
    /// <summary>
    /// Adds a component to the layout.
    /// </summary>
    /// <param name="component">Component to add.</param>
    void AddComponent(IConsoleComponent component);

    /// <summary>
    /// Removes a component from the layout.
    /// </summary>
    /// <param name="component">Component to remove.</param>
    /// <returns>True if component was removed, false otherwise.</returns>
    bool RemoveComponent(IConsoleComponent component);

    /// <summary>
    /// Gets all components in the layout.
    /// </summary>
    /// <returns>Collection of components.</returns>
    IEnumerable<IConsoleComponent> GetComponents();

    /// <summary>
    /// Arranges components within the specified bounds.
    /// </summary>
    /// <param name="bounds">Available space for layout.</param>
    void ArrangeComponents(ConsoleSize bounds);

    /// <summary>
    /// Gets the component at the specified position.
    /// </summary>
    /// <param name="position">Position to check.</param>
    /// <returns>Component at position, or null if none found.</returns>
    IConsoleComponent? GetComponentAt(ConsolePosition position);

    /// <summary>
    /// Sets focus to the specified component.
    /// </summary>
    /// <param name="component">Component to focus, or null to clear focus.</param>
    void SetFocus(IConsoleComponent? component);

    /// <summary>
    /// Gets the currently focused component.
    /// </summary>
    /// <returns>Focused component, or null if none has focus.</returns>
    IConsoleComponent? GetFocusedComponent();

    /// <summary>
    /// Moves focus to the next focusable component.
    /// </summary>
    void FocusNext();

    /// <summary>
    /// Moves focus to the previous focusable component.
    /// </summary>
    void FocusPrevious();

    /// <summary>
    /// Event raised when component focus changes.
    /// </summary>
    event EventHandler<ConsoleFocusChangedEventArgs>? FocusChanged;
}

/// <summary>
/// Event arguments for focus change events.
/// </summary>
public class ConsoleFocusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previously focused component.
    /// </summary>
    public IConsoleComponent? PreviousComponent { get; }

    /// <summary>
    /// Currently focused component.
    /// </summary>
    public IConsoleComponent? CurrentComponent { get; }

    /// <summary>
    /// Initializes event arguments for focus change.
    /// </summary>
    public ConsoleFocusChangedEventArgs(IConsoleComponent? previousComponent, IConsoleComponent? currentComponent)
    {
        PreviousComponent = previousComponent;
        CurrentComponent = currentComponent;
    }
}