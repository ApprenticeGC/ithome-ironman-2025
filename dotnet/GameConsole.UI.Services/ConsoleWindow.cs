using GameConsole.UI.Core;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace GameConsole.UI.Services;

/// <summary>
/// Console implementation of a window component.
/// </summary>
public class ConsoleWindow : BaseUIComponent, IWindow
{
    private readonly Subject<IWindow> _closedSubject = new();
    private readonly List<IUIComponent> _components = new();
    private string _title = string.Empty;
    private bool _canMove = true;
    private bool _canResize = true;
    private Rectangle _clientBounds = Rectangle.Empty;

    public ConsoleWindow(string id, string title = "") : base(id)
    {
        _title = title;
        CanFocus = true;
        UpdateClientBounds();
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value ?? string.Empty;
            UpdateClientBounds();
        }
    }

    public new bool HasFocus
    {
        get => base.HasFocus;
        private set => base.HasFocus = value;
    }

    public bool CanMove
    {
        get => _canMove;
        set => _canMove = value;
    }

    public bool CanResize
    {
        get => _canResize;
        set => _canResize = value;
    }

    public Rectangle ClientBounds => _clientBounds;

    public IObservable<IWindow> Closed => _closedSubject.AsObservable();

    public Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        if (component == null) throw new ArgumentNullException(nameof(component));

        if (!_components.Contains(component))
        {
            _components.Add(component);
            component.Parent = this;
        }

        return Task.CompletedTask;
    }

    public Task RemoveComponentAsync(IUIComponent component, CancellationToken cancellationToken = default)
    {
        if (component != null && _components.Remove(component))
        {
            component.Parent = null;
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<IUIComponent> GetComponents()
    {
        return _components.AsReadOnly();
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _closedSubject.OnNext(this);
        return Task.CompletedTask;
    }

    public override async Task RenderAsync(IConsoleRenderer renderer, CancellationToken cancellationToken = default)
    {
        if (!Visible || Bounds.IsEmpty)
            return;

        // Draw window background
        var backgroundColor = HasFocus ? ConsoleColor.DarkBlue : ConsoleColor.DarkGray;
        await renderer.FillRectangleAsync(Bounds, ' ', ConsoleColor.White, backgroundColor, cancellationToken);

        // Draw window border
        var borderStyle = HasFocus ? BorderStyle.Double : BorderStyle.Single;
        var borderColor = HasFocus ? ConsoleColor.Yellow : ConsoleColor.Gray;
        await renderer.DrawBorderAsync(Bounds, borderStyle, borderColor, cancellationToken);

        // Draw title bar if window has height for it
        if (Bounds.Height > 2 && !string.IsNullOrEmpty(_title))
        {
            var titleBarY = Bounds.Top;
            var titleText = GetDisplayTitle(Bounds.Width - 2);
            var titleX = Bounds.Left + 1 + Math.Max(0, (Bounds.Width - 2 - titleText.Length) / 2);
            
            await renderer.WriteTextAtAsync(titleX, titleBarY, titleText, ConsoleColor.White, backgroundColor, cancellationToken);
        }

        // Draw close button if focused
        if (HasFocus && Bounds.Width > 3)
        {
            await renderer.WriteTextAtAsync(Bounds.Right - 1, Bounds.Top, "X", ConsoleColor.Red, backgroundColor, cancellationToken);
        }

        // Render child components within client bounds
        foreach (var component in _components.Where(c => c.Visible))
        {
            // Translate component bounds to be relative to client area
            var originalBounds = component.Bounds;
            var translatedBounds = new Rectangle(
                _clientBounds.Left + originalBounds.Left,
                _clientBounds.Top + originalBounds.Top,
                originalBounds.Width,
                originalBounds.Height);

            // Only render if component is within client area
            if (_clientBounds.Intersects(translatedBounds))
            {
                // Temporarily update bounds for rendering
                component.Bounds = translatedBounds;
                await component.RenderAsync(renderer, cancellationToken);
                component.Bounds = originalBounds; // Restore original bounds
            }
        }
    }

    public override async Task<bool> HandleEventAsync(UIEvent uiEvent, CancellationToken cancellationToken = default)
    {
        if (!Visible)
            return false;

        // Handle close button click
        if (uiEvent is ClickEvent clickEvent)
        {
            var closeButtonBounds = new Rectangle(Bounds.Right - 1, Bounds.Top, 1, 1);
            if (closeButtonBounds.Contains(clickEvent.Position))
            {
                await CloseAsync(cancellationToken);
                return true;
            }
        }

        // Forward events to focused child components
        var focusedChild = _components.FirstOrDefault(c => c.HasFocus);
        if (focusedChild != null)
        {
            return await focusedChild.HandleEventAsync(uiEvent, cancellationToken);
        }

        // Handle window-specific keyboard shortcuts
        if (HasFocus && uiEvent is KeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case ConsoleKey.Escape:
                    await CloseAsync(cancellationToken);
                    return true;
                case ConsoleKey.Tab:
                    // Focus next component
                    FocusNextComponent(keyEvent.Modifiers.HasFlag(ConsoleModifiers.Shift));
                    return true;
            }
        }

        return false;
    }

    protected override void OnBoundsChanged(Rectangle newBounds)
    {
        UpdateClientBounds();
        base.OnBoundsChanged(newBounds);
    }

    private void UpdateClientBounds()
    {
        if (Bounds.IsEmpty)
        {
            _clientBounds = Rectangle.Empty;
            return;
        }

        // Client area excludes border and title bar
        var hasTitle = !string.IsNullOrEmpty(_title);
        var topOffset = hasTitle ? 1 : 0;
        
        _clientBounds = new Rectangle(
            Bounds.Left + 1,
            Bounds.Top + 1 + topOffset,
            Math.Max(0, Bounds.Width - 2),
            Math.Max(0, Bounds.Height - 2 - topOffset));
    }

    private string GetDisplayTitle(int maxWidth)
    {
        if (string.IsNullOrEmpty(_title) || maxWidth <= 0)
            return string.Empty;

        return _title.Length <= maxWidth ? _title : _title.Substring(0, maxWidth);
    }

    private void FocusNextComponent(bool reverse = false)
    {
        var focusableComponents = _components.Where(c => c.CanFocus && c.Visible).ToList();
        if (focusableComponents.Count == 0)
            return;

        var currentFocused = focusableComponents.FirstOrDefault(c => c.HasFocus);
        int currentIndex = currentFocused != null ? focusableComponents.IndexOf(currentFocused) : -1;

        int nextIndex;
        if (reverse)
        {
            nextIndex = currentIndex <= 0 ? focusableComponents.Count - 1 : currentIndex - 1;
        }
        else
        {
            nextIndex = currentIndex >= focusableComponents.Count - 1 ? 0 : currentIndex + 1;
        }

        // Remove focus from current component
        if (currentFocused != null && currentFocused is BaseUIComponent currentBase)
        {
            currentBase.SetFocus(false);
        }

        // Set focus to next component
        if (focusableComponents[nextIndex] is BaseUIComponent nextBase)
        {
            nextBase.SetFocus(true);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closedSubject.OnCompleted();
            _closedSubject.Dispose();
            
            // Dispose child components
            foreach (var component in _components.OfType<IDisposable>())
            {
                component.Dispose();
            }
            _components.Clear();
        }
        base.Dispose(disposing);
    }
}