using System;
using System.Collections.Generic;
using System.Linq;

namespace GameConsole.Console.UI.Core;

/// <summary>
/// Base abstract class for UI elements that can contain other elements.
/// </summary>
public abstract class UIContainer : UIElement, IUIContainer
{
    private readonly List<IUIElement> _children = new();
    private IUIElement? _focusedChild;
    
    /// <summary>
    /// Gets the collection of child elements in this container.
    /// </summary>
    public IReadOnlyList<IUIElement> Children => _children.AsReadOnly();
    
    /// <summary>
    /// Gets the currently focused child element, if any.
    /// </summary>
    public IUIElement? FocusedChild => _focusedChild;
    
    /// <summary>
    /// Initializes a new instance of the UIContainer class.
    /// </summary>
    /// <param name="id">The unique identifier for this container.</param>
    /// <param name="position">The initial position.</param>
    /// <param name="size">The initial size.</param>
    protected UIContainer(string id, ConsolePosition position, ConsoleSize size) 
        : base(id, position, size)
    {
    }
    
    /// <summary>
    /// Adds a child element to this container.
    /// </summary>
    /// <param name="element">The element to add.</param>
    public virtual void AddChild(IUIElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
            
        if (_children.Contains(element))
            return;
            
        _children.Add(element);
        
        // Set parent reference
        if (element is UIElement uiElement)
        {
            uiElement.Parent = this;
        }
        
        OnChildAdded(element);
    }
    
    /// <summary>
    /// Removes a child element from this container.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    /// <returns>True if the element was removed, false otherwise.</returns>
    public virtual bool RemoveChild(IUIElement element)
    {
        if (element == null)
            return false;
            
        bool removed = _children.Remove(element);
        if (removed)
        {
            // Clear parent reference
            if (element is UIElement uiElement)
            {
                uiElement.Parent = null;
            }
            
            // Clear focus if this child was focused
            if (_focusedChild == element)
            {
                _focusedChild = null;
            }
            
            OnChildRemoved(element);
        }
        
        return removed;
    }
    
    /// <summary>
    /// Removes all child elements from this container.
    /// </summary>
    public virtual void ClearChildren()
    {
        foreach (var child in _children.ToList())
        {
            RemoveChild(child);
        }
    }
    
    /// <summary>
    /// Finds a child element by its ID.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The element if found, null otherwise.</returns>
    public virtual IUIElement? FindChildById(string id)
    {
        foreach (var child in _children)
        {
            if (child.Id == id)
                return child;
                
            // Recursively search containers
            if (child is IUIContainer container)
            {
                var result = container.FindChildById(id);
                if (result != null)
                    return result;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Renders this container and all visible children.
    /// </summary>
    /// <param name="buffer">The console buffer to render to.</param>
    public override void Render(IConsoleBuffer buffer)
    {
        if (!IsVisible)
            return;
            
        // Render the container itself
        OnRender(buffer);
        
        // Render all visible children
        foreach (var child in _children)
        {
            if (child.IsVisible)
            {
                child.Render(buffer);
            }
        }
    }
    
    /// <summary>
    /// Handles input events, passing them to focused children first.
    /// </summary>
    /// <param name="inputEvent">The input event to handle.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    public override bool HandleInput(UIInputEvent inputEvent)
    {
        if (!IsEnabled || !IsVisible)
            return false;
            
        // Let the focused child handle input first
        if (_focusedChild != null && _focusedChild.HandleInput(inputEvent))
        {
            return true;
        }
        
        // Try other children (in reverse order for proper hit testing)
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];
            if (child != _focusedChild && child.IsEnabled && child.IsVisible)
            {
                if (child.HandleInput(inputEvent))
                {
                    return true;
                }
            }
        }
        
        // Handle input for this container
        return base.HandleInput(inputEvent);
    }
    
    /// <summary>
    /// Sets focus on a child element.
    /// </summary>
    /// <param name="child">The child to focus, or null to clear focus.</param>
    /// <returns>True if focus was successfully set, false otherwise.</returns>
    public virtual bool SetFocusedChild(IUIElement? child)
    {
        if (child != null && !_children.Contains(child))
            return false;
            
        // Remove focus from current child
        if (_focusedChild != null)
        {
            _focusedChild.Blur();
        }
        
        _focusedChild = child;
        
        // Set focus on new child
        if (_focusedChild != null)
        {
            return _focusedChild.Focus();
        }
        
        return true;
    }
    
    /// <summary>
    /// Moves focus to the next focusable child.
    /// </summary>
    /// <returns>True if focus was moved, false otherwise.</returns>
    public virtual bool FocusNext()
    {
        var focusableChildren = _children.Where(c => c.CanFocus && c.IsEnabled && c.IsVisible).ToList();
        if (!focusableChildren.Any())
            return false;
            
        int currentIndex = _focusedChild != null ? focusableChildren.IndexOf(_focusedChild) : -1;
        int nextIndex = (currentIndex + 1) % focusableChildren.Count;
        
        return SetFocusedChild(focusableChildren[nextIndex]);
    }
    
    /// <summary>
    /// Moves focus to the previous focusable child.
    /// </summary>
    /// <returns>True if focus was moved, false otherwise.</returns>
    public virtual bool FocusPrevious()
    {
        var focusableChildren = _children.Where(c => c.CanFocus && c.IsEnabled && c.IsVisible).ToList();
        if (!focusableChildren.Any())
            return false;
            
        int currentIndex = _focusedChild != null ? focusableChildren.IndexOf(_focusedChild) : 0;
        int previousIndex = (currentIndex - 1 + focusableChildren.Count) % focusableChildren.Count;
        
        return SetFocusedChild(focusableChildren[previousIndex]);
    }
    
    /// <summary>
    /// Called when a child element is added to this container.
    /// </summary>
    /// <param name="child">The child that was added.</param>
    protected virtual void OnChildAdded(IUIElement child) { }
    
    /// <summary>
    /// Called when a child element is removed from this container.
    /// </summary>
    /// <param name="child">The child that was removed.</param>
    protected virtual void OnChildRemoved(IUIElement child) { }
}