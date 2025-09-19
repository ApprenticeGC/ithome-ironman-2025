using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Input.Services;

/// <summary>
/// Mouse input service for handling mouse and trackpad input events and state.
/// Provides both polling and event-driven access to mouse input.
/// </summary>
[Service("Mouse Input", "1.0.0", "Handles mouse and trackpad input events and button state polling", 
    Categories = new[] { "Input", "Mouse" }, Lifetime = ServiceLifetime.Singleton)]
public class MouseInputService : BaseInputService
{
    private readonly ConcurrentDictionary<MouseButton, InputState> _buttonStates;
    private readonly ConcurrentDictionary<MouseButton, DateTime> _buttonPressTimestamps;
    private Vector2 _currentPosition;
    private Vector2 _previousPosition;
    private readonly object _eventLock = new object();

    public MouseInputService(ILogger<MouseInputService> logger) : base(logger)
    {
        _buttonStates = new ConcurrentDictionary<MouseButton, InputState>();
        _buttonPressTimestamps = new ConcurrentDictionary<MouseButton, DateTime>();
        _currentPosition = Vector2.Zero;
        _previousPosition = Vector2.Zero;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing mouse input monitoring");
        
        // Initialize all button states to Released
        foreach (MouseButton button in Enum.GetValues<MouseButton>())
        {
            _buttonStates[button] = InputState.Released;
        }

        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting mouse input polling");
        
        // In a real implementation, this would start mouse event monitoring
        // For now, we'll simulate with a background task
        _ = Task.Run(async () => await SimulateMouseInputAsync(cancellationToken), cancellationToken);
        
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping mouse input monitoring");
        await Task.CompletedTask;
    }

    public override Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default)
    {
        // Mouse service doesn't handle keyboard
        return Task.FromResult(false);
    }

    public override Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return Task.FromResult(Vector2.Zero);

        lock (_eventLock)
        {
            _logger.LogTrace("Mouse position: {Position}", _currentPosition);
            return Task.FromResult(_currentPosition);
        }
    }

    public override Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return Task.FromResult(false);

        var state = _buttonStates.GetValueOrDefault(button, InputState.Released);
        var isPressed = state == InputState.Pressed || state == InputState.Held;
        
        _logger.LogTrace("Mouse button {Button} state check: {IsPressed}", button, isPressed);
        return Task.FromResult(isPressed);
    }

    public override Task<bool> IsGamepadButtonPressedAsync(int gamepadIndex, GamepadButton button, CancellationToken cancellationToken = default)
    {
        // Mouse service doesn't handle gamepad
        return Task.FromResult(false);
    }

    public override Task<float> GetGamepadAxisAsync(int gamepadIndex, GamepadAxis axis, CancellationToken cancellationToken = default)
    {
        // Mouse service doesn't handle gamepad
        return Task.FromResult(0.0f);
    }

    public override Task<int> GetConnectedGamepadCountAsync(CancellationToken cancellationToken = default)
    {
        // Mouse service doesn't handle gamepad
        return Task.FromResult(0);
    }

    public override Task<bool> IsGamepadConnectedAsync(int gamepadIndex, CancellationToken cancellationToken = default)
    {
        // Mouse service doesn't handle gamepad
        return Task.FromResult(false);
    }

    public override Task<string?> GetGamepadNameAsync(int gamepadIndex, CancellationToken cancellationToken = default)
    {
        // Mouse service doesn't handle gamepad
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Simulates mouse input for demonstration purposes.
    /// In a real implementation, this would hook into the OS mouse events.
    /// </summary>
    private async Task SimulateMouseInputAsync(CancellationToken cancellationToken)
    {
        var random = new Random();
        var buttons = new[] { MouseButton.Left, MouseButton.Right, MouseButton.Middle };
        
        while (!cancellationToken.IsCancellationRequested && IsRunning)
        {
            try
            {
                await Task.Delay(100, cancellationToken); // Update more frequently for smoother movement
                
                // Simulate mouse movement
                if (random.NextDouble() < 0.3) // 30% chance of movement
                {
                    SimulateMouseMovement();
                }
                
                // Simulate button events
                if (random.NextDouble() < 0.05) // 5% chance of button event
                {
                    var button = buttons[random.Next(buttons.Length)];
                    var shouldPress = random.NextDouble() < 0.5;
                    
                    if (shouldPress && _buttonStates[button] == InputState.Released)
                    {
                        SimulateButtonPress(button);
                    }
                    else if (!shouldPress && (_buttonStates[button] == InputState.Pressed || _buttonStates[button] == InputState.Held))
                    {
                        SimulateButtonRelease(button);
                    }
                }

                // Simulate scroll events occasionally
                if (random.NextDouble() < 0.02) // 2% chance of scroll
                {
                    SimulateScrollEvent();
                }
                
                // Update button states
                UpdateButtonStates();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mouse input simulation");
            }
        }
    }

    private void SimulateMouseMovement()
    {
        lock (_eventLock)
        {
            var random = new Random();
            _previousPosition = _currentPosition;
            
            // Simulate small random movements
            var deltaX = (float)(random.NextDouble() - 0.5) * 10.0f;
            var deltaY = (float)(random.NextDouble() - 0.5) * 10.0f;
            
            _currentPosition = new Vector2(
                Math.Clamp(_currentPosition.X + deltaX, 0, 1920),
                Math.Clamp(_currentPosition.Y + deltaY, 0, 1080));
            
            var delta = new Vector2(_currentPosition.X - _previousPosition.X, _currentPosition.Y - _previousPosition.Y);
            var now = DateTime.UtcNow;
            
            var mouseEvent = new MouseEvent(_currentPosition, delta, Vector2.Zero, null, null, now, GetCurrentFrame());
            PublishMouseEvent(mouseEvent);
            
            _logger.LogTrace("Simulated mouse movement to {Position}, delta: {Delta}", _currentPosition, delta);
        }
    }

    private void SimulateButtonPress(MouseButton button)
    {
        lock (_eventLock)
        {
            var now = DateTime.UtcNow;
            _buttonStates[button] = InputState.Pressed;
            _buttonPressTimestamps[button] = now;
            
            var mouseEvent = new MouseEvent(_currentPosition, Vector2.Zero, Vector2.Zero, button, InputState.Pressed, now, GetCurrentFrame());
            PublishMouseEvent(mouseEvent);
            
            _logger.LogTrace("Simulated mouse button press: {Button}", button);
        }
    }

    private void SimulateButtonRelease(MouseButton button)
    {
        lock (_eventLock)
        {
            var now = DateTime.UtcNow;
            _buttonStates[button] = InputState.JustReleased;
            
            var mouseEvent = new MouseEvent(_currentPosition, Vector2.Zero, Vector2.Zero, button, InputState.JustReleased, now, GetCurrentFrame());
            PublishMouseEvent(mouseEvent);
            
            _logger.LogTrace("Simulated mouse button release: {Button}", button);
        }
    }

    private void SimulateScrollEvent()
    {
        lock (_eventLock)
        {
            var random = new Random();
            var scrollDelta = new Vector2(0, (float)(random.NextDouble() - 0.5) * 3.0f);
            var now = DateTime.UtcNow;
            
            var mouseEvent = new MouseEvent(_currentPosition, Vector2.Zero, scrollDelta, null, null, now, GetCurrentFrame());
            PublishMouseEvent(mouseEvent);
            
            _logger.LogTrace("Simulated mouse scroll: {ScrollDelta}", scrollDelta);
        }
    }

    private void UpdateButtonStates()
    {
        lock (_eventLock)
        {
            var buttonsToUpdate = _buttonStates
                .Where(kvp => kvp.Value == InputState.Pressed || kvp.Value == InputState.JustReleased)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var button in buttonsToUpdate)
            {
                var currentState = _buttonStates[button];
                if (currentState == InputState.Pressed)
                {
                    _buttonStates[button] = InputState.Held;
                }
                else if (currentState == InputState.JustReleased)
                {
                    _buttonStates[button] = InputState.Released;
                }
            }
            
            IncrementFrame();
        }
    }
}