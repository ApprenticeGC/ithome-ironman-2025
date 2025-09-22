using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Base interface for all UI components in the console UI system.
/// Defines the fundamental operations and properties that all UI elements must support.
/// </summary>
public interface IUIComponent : ICapabilityProvider
{
    /// <summary>
    /// Unique identifier for the UI component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Position of the component within its parent container.
    /// </summary>
    Position Position { get; set; }
    
    /// <summary>
    /// Size of the component.
    /// </summary>
    Size Size { get; set; }
    
    /// <summary>
    /// Visibility state of the component.
    /// </summary>
    Visibility Visibility { get; set; }
    
    /// <summary>
    /// Gets the computed bounds of the component.
    /// </summary>
    UIBounds Bounds { get; }
    
    /// <summary>
    /// Parent component, if any.
    /// </summary>
    IUIComponent? Parent { get; }
    
    /// <summary>
    /// Child components of this component.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }
    
    /// <summary>
    /// Adds a child component.
    /// </summary>
    /// <param name="child">The child component to add.</param>
    void AddChild(IUIComponent child);
    
    /// <summary>
    /// Removes a child component.
    /// </summary>
    /// <param name="child">The child component to remove.</param>
    /// <returns>True if the child was found and removed.</returns>
    bool RemoveChild(IUIComponent child);
    
    /// <summary>
    /// Event raised when the component needs to be redrawn.
    /// </summary>
    event EventHandler? Invalidated;
    
    /// <summary>
    /// Invalidates the component, marking it for redraw.
    /// </summary>
    void Invalidate();
}

/// <summary>
/// Interface for UI components that can display text.
/// </summary>
public interface ITextComponent : IUIComponent
{
    /// <summary>
    /// Text content to display.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Text alignment within the component bounds.
    /// </summary>
    TextAlignment TextAlignment { get; set; }
    
    /// <summary>
    /// Vertical alignment of text within the component bounds.
    /// </summary>
    VerticalAlignment VerticalAlignment { get; set; }
    
    /// <summary>
    /// Text and background colors.
    /// </summary>
    ConsoleColor Colors { get; set; }
}

/// <summary>
/// Interface for UI components that can handle user interaction.
/// </summary>
public interface IInteractiveComponent : IUIComponent
{
    /// <summary>
    /// Whether the component can receive input focus.
    /// </summary>
    bool CanFocus { get; }
    
    /// <summary>
    /// Whether the component currently has input focus.
    /// </summary>
    bool HasFocus { get; }
    
    /// <summary>
    /// Event raised when a key is pressed while the component has focus.
    /// </summary>
    event EventHandler<ConsoleKeyInfo>? KeyPressed;
    
    /// <summary>
    /// Attempts to set focus to this component.
    /// </summary>
    /// <returns>True if focus was successfully set.</returns>
    bool Focus();
    
    /// <summary>
    /// Removes focus from this component.
    /// </summary>
    void Blur();
}