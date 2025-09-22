using GameConsole.Input.Core;

namespace GameConsole.UI.Console;

/// <summary>
/// Interface for managing console input interactions in UI components.
/// </summary>
public interface IConsoleInputManager
{
    /// <summary>
    /// Event fired when a key is pressed that should be handled by UI components.
    /// </summary>
    event EventHandler<UIKeyEventArgs>? KeyPressed;
    
    /// <summary>
    /// Waits for a key press and returns the key information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Key information for the pressed key.</returns>
    Task<UIKeyEventArgs> WaitForKeyAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a specific key is currently pressed.
    /// </summary>
    /// <param name="key">Key to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the key is pressed; otherwise, false.</returns>
    Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enables or disables input capturing for UI components.
    /// </summary>
    /// <param name="enabled">True to enable input capturing; false to disable.</param>
    void SetInputCapture(bool enabled);
    
    /// <summary>
    /// Gets a value indicating whether input capturing is currently enabled.
    /// </summary>
    bool IsInputCaptureEnabled { get; }
}

/// <summary>
/// Interface for managing console layout and positioning.
/// </summary>
public interface IConsoleLayoutManager
{
    /// <summary>
    /// Calculates the optimal layout for UI components within the console bounds.
    /// </summary>
    /// <param name="components">Components to layout.</param>
    /// <returns>Layout information for each component.</returns>
    IReadOnlyDictionary<string, Rectangle> CalculateLayout(IEnumerable<IUIComponent> components);
    
    /// <summary>
    /// Gets the available screen area for UI components.
    /// </summary>
    Rectangle ScreenBounds { get; }
    
    /// <summary>
    /// Creates a new layout region within the specified bounds.
    /// </summary>
    /// <param name="bounds">Bounds for the layout region.</param>
    /// <returns>A new layout region.</returns>
    ILayoutRegion CreateRegion(Rectangle bounds);
}

/// <summary>
/// Represents a rectangular area for UI layout.
/// </summary>
public struct Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>
/// Represents a layout region that can contain UI components.
/// </summary>
public interface ILayoutRegion
{
    /// <summary>
    /// Gets the bounds of this layout region.
    /// </summary>
    Rectangle Bounds { get; }
    
    /// <summary>
    /// Subdivides this region into smaller regions.
    /// </summary>
    /// <param name="divisions">Number of divisions.</param>
    /// <param name="direction">Direction of division (horizontal or vertical).</param>
    /// <returns>Array of subdivided regions.</returns>
    ILayoutRegion[] Subdivide(int divisions, LayoutDirection direction);
    
    /// <summary>
    /// Splits this region into two regions at the specified position.
    /// </summary>
    /// <param name="position">Position to split at.</param>
    /// <param name="direction">Direction of split.</param>
    /// <returns>Array containing the two split regions.</returns>
    ILayoutRegion[] Split(int position, LayoutDirection direction);
}

/// <summary>
/// Layout direction for subdividing regions.
/// </summary>
public enum LayoutDirection
{
    Horizontal,
    Vertical
}

/// <summary>
/// Event arguments for UI key events.
/// </summary>
public class UIKeyEventArgs : EventArgs
{
    public KeyCode Key { get; }
    public bool CtrlPressed { get; }
    public bool AltPressed { get; }
    public bool ShiftPressed { get; }
    public char? Character { get; }
    
    public UIKeyEventArgs(KeyCode key, bool ctrlPressed = false, bool altPressed = false, bool shiftPressed = false, char? character = null)
    {
        Key = key;
        CtrlPressed = ctrlPressed;
        AltPressed = altPressed;
        ShiftPressed = shiftPressed;
        Character = character;
    }
}

/// <summary>
/// Base interface for UI components.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Gets the unique identifier for this component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets or sets the desired bounds for this component.
    /// </summary>
    Rectangle DesiredBounds { get; set; }
    
    /// <summary>
    /// Gets the actual bounds assigned to this component after layout.
    /// </summary>
    Rectangle ActualBounds { get; }
    
    /// <summary>
    /// Gets or sets whether this component is visible.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Gets or sets whether this component can receive input focus.
    /// </summary>
    bool CanFocus { get; set; }
    
    /// <summary>
    /// Gets whether this component currently has input focus.
    /// </summary>
    bool HasFocus { get; }
    
    /// <summary>
    /// Renders this component to the console.
    /// </summary>
    /// <param name="framework">Console framework for rendering.</param>
    void Render(IConsoleUIFramework framework);
    
    /// <summary>
    /// Handles input events for this component.
    /// </summary>
    /// <param name="input">Input event to handle.</param>
    /// <returns>True if the input was handled; false otherwise.</returns>
    bool HandleInput(UIKeyEventArgs input);
    
    /// <summary>
    /// Sets the layout bounds for this component.
    /// </summary>
    /// <param name="bounds">New bounds for the component.</param>
    void SetBounds(Rectangle bounds);
}