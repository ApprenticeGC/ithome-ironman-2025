using GameConsole.UI.Core;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace GameConsole.UI.Services;

/// <summary>
/// Base class for all UI components providing common functionality.
/// </summary>
public abstract class BaseUIComponent : IUIComponent, IDisposable
{
    private readonly Subject<Rectangle> _boundsChangedSubject = new();
    private readonly Subject<bool> _visibilityChangedSubject = new();
    private Rectangle _bounds = Rectangle.Empty;
    private bool _visible = true;
    private bool _canFocus = false;
    private bool _hasFocus = false;
    private IUIComponent? _parent;
    private bool _disposed = false;

    protected BaseUIComponent(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public string Id { get; }

    public Rectangle Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds != value)
            {
                _bounds = value;
                _boundsChangedSubject.OnNext(_bounds);
                OnBoundsChanged(_bounds);
            }
        }
    }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible != value)
            {
                _visible = value;
                _visibilityChangedSubject.OnNext(_visible);
                OnVisibilityChanged(_visible);
            }
        }
    }

    public bool CanFocus
    {
        get => _canFocus;
        set => _canFocus = value;
    }

    public bool HasFocus
    {
        get => _hasFocus;
        protected set
        {
            if (_hasFocus != value)
            {
                _hasFocus = value;
                OnFocusChanged(_hasFocus);
            }
        }
    }

    public IUIComponent? Parent
    {
        get => _parent;
        set => _parent = value;
    }

    public IObservable<Rectangle> BoundsChanged => _boundsChangedSubject.AsObservable();
    public IObservable<bool> VisibilityChanged => _visibilityChangedSubject.AsObservable();

    public abstract Task RenderAsync(IConsoleRenderer renderer, CancellationToken cancellationToken = default);

    public virtual Task<bool> HandleEventAsync(UIEvent uiEvent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false); // Base implementation doesn't handle events
    }

    protected virtual void OnBoundsChanged(Rectangle newBounds) { }
    protected virtual void OnVisibilityChanged(bool visible) { }
    protected virtual void OnFocusChanged(bool hasFocus) { }

    /// <summary>
    /// Set focus state for this component. Protected method for use by parent components.
    /// </summary>
    internal void SetFocus(bool focus) => HasFocus = focus;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _boundsChangedSubject.OnCompleted();
            _boundsChangedSubject.Dispose();
            _visibilityChangedSubject.OnCompleted();
            _visibilityChangedSubject.Dispose();
            _disposed = true;
        }
    }
}