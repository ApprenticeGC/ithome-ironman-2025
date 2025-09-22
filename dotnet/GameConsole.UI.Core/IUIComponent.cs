using System;
using System.Collections.Generic;
using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Base interface for all UI components.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Unique identifier for this component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Position of the component in screen space.
    /// </summary>
    Position Position { get; set; }
    
    /// <summary>
    /// Size of the component.
    /// </summary>
    Size Size { get; set; }
    
    /// <summary>
    /// Visibility state of the component.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Whether the component can receive focus.
    /// </summary>
    bool CanFocus { get; }
    
    /// <summary>
    /// Whether the component currently has focus.
    /// </summary>
    bool HasFocus { get; set; }
    
    /// <summary>
    /// Current UI state of the component.
    /// </summary>
    UIState State { get; }
    
    /// <summary>
    /// Parent container if this component is contained within another.
    /// </summary>
    ILayoutContainer? Parent { get; set; }
    
    /// <summary>
    /// Checks if a point is within the component's bounds.
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns>True if point is within bounds.</returns>
    bool ContainsPoint(Position point);
    
    /// <summary>
    /// Handle UI event.
    /// </summary>
    /// <param name="uiEvent">Event to handle.</param>
    /// <returns>True if event was handled.</returns>
    bool HandleEvent(UIEvent uiEvent);
    
    /// <summary>
    /// Update component state.
    /// </summary>
    /// <param name="deltaTime">Time since last update in seconds.</param>
    void Update(float deltaTime);
}

/// <summary>
/// Interface for components that can contain child components.
/// </summary>
public interface ILayoutContainer : IUIComponent
{
    /// <summary>
    /// Child components in this container.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }
    
    /// <summary>
    /// Add a child component.
    /// </summary>
    /// <param name="child">Component to add.</param>
    void AddChild(IUIComponent child);
    
    /// <summary>
    /// Remove a child component.
    /// </summary>
    /// <param name="child">Component to remove.</param>
    /// <returns>True if child was removed.</returns>
    bool RemoveChild(IUIComponent child);
    
    /// <summary>
    /// Remove child component by ID.
    /// </summary>
    /// <param name="childId">ID of component to remove.</param>
    /// <returns>True if child was removed.</returns>
    bool RemoveChild(string childId);
    
    /// <summary>
    /// Find child component by ID.
    /// </summary>
    /// <param name="childId">ID of child to find.</param>
    /// <returns>Child component or null if not found.</returns>
    IUIComponent? FindChild(string childId);
    
    /// <summary>
    /// Clear all child components.
    /// </summary>
    void ClearChildren();
}

/// <summary>
/// Interface for clickable UI components.
/// </summary>
public interface IClickable
{
    /// <summary>
    /// Event fired when component is clicked.
    /// </summary>
    event EventHandler<ClickEvent>? Clicked;
    
    /// <summary>
    /// Whether the component can be clicked in its current state.
    /// </summary>
    bool CanClick { get; }
}

/// <summary>
/// Interface for UI components that can display text.
/// </summary>
public interface ITextDisplay
{
    /// <summary>
    /// Text content to display.
    /// </summary>
    string Text { get; set; }
    
    /// <summary>
    /// Text color.
    /// </summary>
    UIColor TextColor { get; set; }
    
    /// <summary>
    /// Text alignment.
    /// </summary>
    TextAlignment TextAlignment { get; set; }
    
    /// <summary>
    /// Font size for text rendering.
    /// </summary>
    int FontSize { get; set; }
}

/// <summary>
/// Interface for UI components that accept text input.
/// </summary>
public interface ITextInput : ITextDisplay
{
    /// <summary>
    /// Whether the component is in text editing mode.
    /// </summary>
    bool IsEditing { get; }
    
    /// <summary>
    /// Placeholder text when no content is entered.
    /// </summary>
    string PlaceholderText { get; set; }
    
    /// <summary>
    /// Maximum length of text input.
    /// </summary>
    int MaxLength { get; set; }
    
    /// <summary>
    /// Event fired when text content changes.
    /// </summary>
    event EventHandler<TextChangedEventArgs>? TextChanged;
    
    /// <summary>
    /// Begin text editing mode.
    /// </summary>
    void BeginEdit();
    
    /// <summary>
    /// End text editing mode and commit changes.
    /// </summary>
    void EndEdit();
    
    /// <summary>
    /// Cancel text editing and revert changes.
    /// </summary>
    void CancelEdit();
}

/// <summary>
/// Event arguments for text changed events.
/// </summary>
public class TextChangedEventArgs : EventArgs
{
    public string OldText { get; init; }
    public string NewText { get; init; }
    
    public TextChangedEventArgs(string oldText, string newText)
    {
        OldText = oldText;
        NewText = newText;
    }
}