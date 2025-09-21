using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using GameConsole.Input.Services;
using Microsoft.Extensions.Logging;

namespace GameConsole.Input.Services;

/// <summary>
/// Base implementation for input services providing common functionality.
/// </summary>
public abstract class BaseInputService : GameConsole.Input.Services.IService
{
    protected readonly ILogger _logger;
    private long _currentFrame = 0;
    private bool _isRunning = false;

    protected BaseInputService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IService Implementation

    public bool IsRunning => _isRunning;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        await OnInitializeAsync(cancellationToken);
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        await OnStartAsync(cancellationToken);
        _isRunning = true;
        _logger.LogInformation("Started {ServiceType}", GetType().Name);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {ServiceType}", GetType().Name);
        _isRunning = false;
        await OnStopAsync(cancellationToken);
        _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        await OnDisposeAsync();
    }

    #endregion

    #region Input Service Implementation

    public event EventHandler<KeyEvent>? KeyEvent;
    public event EventHandler<MouseEvent>? MouseEvent;
    public event EventHandler<GamepadEvent>? GamepadEvent;

    public abstract Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default);
    public abstract Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default);
    public abstract Task<bool> IsGamepadButtonPressedAsync(int gamepadIndex, GamepadButton button, CancellationToken cancellationToken = default);
    public abstract Task<float> GetGamepadAxisAsync(int gamepadIndex, GamepadAxis axis, CancellationToken cancellationToken = default);
    public abstract Task<int> GetConnectedGamepadCountAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> IsGamepadConnectedAsync(int gamepadIndex, CancellationToken cancellationToken = default);
    public abstract Task<string?> GetGamepadNameAsync(int gamepadIndex, CancellationToken cancellationToken = default);

    #endregion

    #region Protected Methods for Derived Classes

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    protected void PublishKeyEvent(KeyEvent keyEvent)
    {
        KeyEvent?.Invoke(this, keyEvent);
    }

    protected void PublishMouseEvent(MouseEvent mouseEvent)
    {
        MouseEvent?.Invoke(this, mouseEvent);
    }

    protected void PublishGamepadEvent(GamepadEvent gamepadEvent)
    {
        GamepadEvent?.Invoke(this, gamepadEvent);
    }

    protected long GetCurrentFrame() => _currentFrame;
    protected void IncrementFrame() => _currentFrame++;

    #endregion
}