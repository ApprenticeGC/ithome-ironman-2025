using System;
using System.Collections.Generic;

namespace GameConsole.Console.UI.Core;

/// <summary>
/// Base interface for all UI elements in the console UI system.
/// </summary>
public interface IUIElement
{
    /// <summary>
    /// Gets the unique identifier for this UI element.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets or sets the position of this element.
    /// </summary>
    ConsolePosition Position { get; set; }
    
    /// <summary>
    /// Gets or sets the size of this element.
    /// </summary>
    ConsoleSize Size { get; set; }
    
    /// <summary>
    /// Gets the bounds of this element (position + size).
    /// </summary>
    ConsoleBounds Bounds { get; }
    
    /// <summary>
    /// Gets or sets whether this element is visible.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Gets or sets whether this element is enabled for interaction.
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets whether this element can receive focus.
    /// </summary>
    bool CanFocus { get; set; }
    
    /// <summary>
    /// Gets whether this element currently has focus.
    /// </summary>
    bool HasFocus { get; }
    
    /// <summary>
    /// Gets the parent container of this element, if any.
    /// </summary>
    IUIContainer? Parent { get; }
    
    /// <summary>
    /// Renders this element to the provided console buffer.
    /// </summary>
    /// <param name="buffer">The console buffer to render to.</param>
    void Render(IConsoleBuffer buffer);
    
    /// <summary>
    /// Handles input events for this element.
    /// </summary>
    /// <param name="inputEvent">The input event to handle.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    bool HandleInput(UIInputEvent inputEvent);
    
    /// <summary>
    /// Sets focus on this element.
    /// </summary>
    /// <returns>True if focus was successfully set, false otherwise.</returns>
    bool Focus();
    
    /// <summary>
    /// Removes focus from this element.
    /// </summary>
    void Blur();
    
    /// <summary>
    /// Event raised when this element gains focus.
    /// </summary>
    event EventHandler? GotFocus;
    
    /// <summary>
    /// Event raised when this element loses focus.
    /// </summary>
    event EventHandler? LostFocus;
    
    /// <summary>
    /// Event raised when this element is clicked.
    /// </summary>
    event EventHandler<UIClickEventArgs>? Click;
}

/// <summary>
/// Interface for UI elements that can contain other elements.
/// </summary>
public interface IUIContainer : IUIElement
{
    /// <summary>
    /// Gets the collection of child elements in this container.
    /// </summary>
    IReadOnlyList<IUIElement> Children { get; }
    
    /// <summary>
    /// Adds a child element to this container.
    /// </summary>
    /// <param name="element">The element to add.</param>
    void AddChild(IUIElement element);
    
    /// <summary>
    /// Removes a child element from this container.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    /// <returns>True if the element was removed, false otherwise.</returns>
    bool RemoveChild(IUIElement element);
    
    /// <summary>
    /// Removes all child elements from this container.
    /// </summary>
    void ClearChildren();
    
    /// <summary>
    /// Finds a child element by its ID.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The element if found, null otherwise.</returns>
    IUIElement? FindChildById(string id);
    
    /// <summary>
    /// Gets the currently focused child element, if any.
    /// </summary>
    IUIElement? FocusedChild { get; }
}

/// <summary>
/// Interface for console buffer operations.
/// </summary>
public interface IConsoleBuffer
{
    /// <summary>
    /// Gets the width of the console buffer.
    /// </summary>
    int Width { get; }
    
    /// <summary>
    /// Gets the height of the console buffer.
    /// </summary>
    int Height { get; }
    
    /// <summary>
    /// Sets a character at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="styledChar">The styled character to set.</param>
    void SetChar(int x, int y, StyledChar styledChar);
    
    /// <summary>
    /// Gets a character at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>The styled character at the position.</returns>
    StyledChar GetChar(int x, int y);
    
    /// <summary>
    /// Draws text at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="style">The style to apply.</param>
    void DrawText(int x, int y, string text, ConsoleStyle style);
    
    /// <summary>
    /// Fills a rectangular region with a character and style.
    /// </summary>
    /// <param name="bounds">The region to fill.</param>
    /// <param name="character">The character to fill with.</param>
    /// <param name="style">The style to apply.</param>
    void FillRegion(ConsoleBounds bounds, char character, ConsoleStyle style);
    
    /// <summary>
    /// Clears the entire buffer.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Clears a specific region of the buffer.
    /// </summary>
    /// <param name="bounds">The region to clear.</param>
    void Clear(ConsoleBounds bounds);
}