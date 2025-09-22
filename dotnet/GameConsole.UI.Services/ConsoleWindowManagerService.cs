using GameConsole.Core.Abstractions;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace GameConsole.UI.Services;

/// <summary>
/// Console-based window manager service for managing UI windows.
/// </summary>
[Service("Window Manager", "1.0.0", "Manages UI windows and focus in console mode",
    Categories = new[] { "UI", "Console", "Windows" }, Lifetime = ServiceLifetime.Singleton)]
public class ConsoleWindowManagerService : BaseUIService, IWindowManager
{
    private readonly Subject<IWindow?> _focusChangedSubject = new();
    private readonly List<IWindow> _windows = new();
    private IWindow? _focusedWindow;

    public ConsoleWindowManagerService(ILogger<ConsoleWindowManagerService> logger) : base(logger) { }

    public IObservable<IWindow?> FocusChanged => _focusChangedSubject.AsObservable();

    public async Task<IWindow> CreateWindowAsync(string title, Rectangle bounds, CancellationToken cancellationToken = default)
    {
        var windowId = Guid.NewGuid().ToString();
        var window = new ConsoleWindow(windowId, title)
        {
            Bounds = bounds
        };

        _windows.Add(window);

        // Subscribe to window closed event
        window.Closed.Subscribe(closedWindow => 
        {
            _windows.Remove(closedWindow);
            if (_focusedWindow == closedWindow)
            {
                // Focus the next available window
                _focusedWindow = _windows.LastOrDefault();
                UpdateFocus();
            }
        });

        // If this is the first window, focus it
        if (_focusedWindow == null)
        {
            await SetFocusAsync(window, cancellationToken);
        }

        Logger.LogDebug("Created window '{Title}' with bounds {Bounds}", title, bounds);
        return window;
    }

    public IReadOnlyList<IWindow> GetWindows()
    {
        return _windows.AsReadOnly();
    }

    public Task CloseWindowAsync(IWindow window, CancellationToken cancellationToken = default)
    {
        if (window == null) throw new ArgumentNullException(nameof(window));

        return window.CloseAsync(cancellationToken);
    }

    public Task SetFocusAsync(IWindow window, CancellationToken cancellationToken = default)
    {
        if (window == null) throw new ArgumentNullException(nameof(window));
        
        if (!_windows.Contains(window))
        {
            Logger.LogWarning("Cannot focus window that is not managed by this window manager");
            return Task.CompletedTask;
        }

        if (_focusedWindow != window)
        {
            _focusedWindow = window;
            UpdateFocus();
            Logger.LogDebug("Focused window '{Title}'", window.Title);
        }

        return Task.CompletedTask;
    }

    public IWindow? GetFocusedWindow()
    {
        return _focusedWindow;
    }

    /// <summary>
    /// Handle global UI events and route them to appropriate windows.
    /// </summary>
    public async Task HandleGlobalEventAsync(UIEvent uiEvent, CancellationToken cancellationToken = default)
    {
        // Route events to the focused window first
        if (_focusedWindow != null)
        {
            var handled = await _focusedWindow.HandleEventAsync(uiEvent, cancellationToken);
            if (handled) return;
        }

        // Handle global window management keys
        if (uiEvent is KeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case ConsoleKey.Tab when keyEvent.Modifiers.HasFlag(ConsoleModifiers.Alt):
                    // Alt+Tab: Switch between windows
                    FocusNextWindow(keyEvent.Modifiers.HasFlag(ConsoleModifiers.Shift));
                    break;

                case ConsoleKey.F4 when keyEvent.Modifiers.HasFlag(ConsoleModifiers.Alt):
                    // Alt+F4: Close focused window
                    if (_focusedWindow != null)
                    {
                        await _focusedWindow.CloseAsync(cancellationToken);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Render all windows in the correct order (focused window on top).
    /// </summary>
    public async Task RenderAllWindowsAsync(IConsoleRenderer renderer, CancellationToken cancellationToken = default)
    {
        // Render non-focused windows first (bottom to top)
        foreach (var window in _windows.Where(w => w != _focusedWindow))
        {
            await window.RenderAsync(renderer, cancellationToken);
        }

        // Render focused window last (on top)
        if (_focusedWindow != null)
        {
            await _focusedWindow.RenderAsync(renderer, cancellationToken);
        }
    }

    private void UpdateFocus()
    {
        // Update focus state for all windows
        foreach (var window in _windows)
        {
            if (window is BaseUIComponent baseWindow)
            {
                baseWindow.SetFocus(window == _focusedWindow);
            }
        }

        _focusChangedSubject.OnNext(_focusedWindow);
    }

    private void FocusNextWindow(bool reverse = false)
    {
        if (_windows.Count <= 1) return;

        var currentIndex = _focusedWindow != null ? _windows.IndexOf(_focusedWindow) : -1;

        int nextIndex;
        if (reverse)
        {
            nextIndex = currentIndex <= 0 ? _windows.Count - 1 : currentIndex - 1;
        }
        else
        {
            nextIndex = currentIndex >= _windows.Count - 1 ? 0 : currentIndex + 1;
        }

        _ = SetFocusAsync(_windows[nextIndex]);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _focusChangedSubject.OnCompleted();
        _focusChangedSubject.Dispose();

        // Close all windows
        var windowsCopy = _windows.ToList();
        _windows.Clear();
        
        foreach (var window in windowsCopy)
        {
            await window.CloseAsync();
            if (window is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        await base.OnDisposeAsync();
    }
}