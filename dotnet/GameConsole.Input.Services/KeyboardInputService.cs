using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Input.Services;

/// <summary>
/// Keyboard input service for handling keyboard input events and state.
/// Provides both polling and event-driven access to keyboard input.
/// </summary>
[Service("Keyboard Input", "1.0.0", "Handles keyboard input events and key state polling", 
    Categories = new[] { "Input", "Keyboard" }, Lifetime = ServiceLifetime.Singleton)]
public class KeyboardInputService : BaseInputService
{
    private readonly ConcurrentDictionary<KeyCode, InputState> _keyStates;
    private readonly ConcurrentDictionary<KeyCode, DateTime> _keyPressTimestamps;
    private readonly object _eventLock = new object();

    public KeyboardInputService(ILogger<KeyboardInputService> logger) : base(logger)
    {
        _keyStates = new ConcurrentDictionary<KeyCode, InputState>();
        _keyPressTimestamps = new ConcurrentDictionary<KeyCode, DateTime>();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing keyboard input monitoring");
        
        // Initialize all key states to Released
        foreach (KeyCode key in Enum.GetValues<KeyCode>())
        {
            _keyStates[key] = InputState.Released;
        }

        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting keyboard input polling");
        
        // In a real implementation, this would start keyboard event monitoring
        // For now, we'll simulate with a background task
        _ = Task.Run(async () => await SimulateKeyboardInputAsync(cancellationToken), cancellationToken);
        
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping keyboard input monitoring");
        await Task.CompletedTask;
    }

    public override Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return Task.FromResult(false);

        var state = _keyStates.GetValueOrDefault(key, InputState.Released);
        var isPressed = state == InputState.Pressed || state == InputState.Held;
        
        _logger.LogTrace("Key {Key} state check: {IsPressed}", key, isPressed);
        return Task.FromResult(isPressed);
    }

    public override Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default)
    {
        // Keyboard service doesn't handle mouse - return zero
        return Task.FromResult(Vector2.Zero);
    }

    public override Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default)
    {
        // Keyboard service doesn't handle mouse
        return Task.FromResult(false);
    }

    public override Task<bool> IsGamepadButtonPressedAsync(int gamepadIndex, GamepadButton button, CancellationToken cancellationToken = default)
    {
        // Keyboard service doesn't handle gamepad
        return Task.FromResult(false);
    }

    public override Task<float> GetGamepadAxisAsync(int gamepadIndex, GamepadAxis axis, CancellationToken cancellationToken = default)
    {
        // Keyboard service doesn't handle gamepad
        return Task.FromResult(0.0f);
    }

    public override Task<int> GetConnectedGamepadCountAsync(CancellationToken cancellationToken = default)
    {
        // Keyboard service doesn't handle gamepad
        return Task.FromResult(0);
    }

    public override Task<bool> IsGamepadConnectedAsync(int gamepadIndex, CancellationToken cancellationToken = default)
    {
        // Keyboard service doesn't handle gamepad
        return Task.FromResult(false);
    }

    public override Task<string?> GetGamepadNameAsync(int gamepadIndex, CancellationToken cancellationToken = default)
    {
        // Keyboard service doesn't handle gamepad
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Simulates keyboard input for demonstration purposes.
    /// In a real implementation, this would hook into the OS keyboard events.
    /// </summary>
    private async Task SimulateKeyboardInputAsync(CancellationToken cancellationToken)
    {
        var random = new Random();
        var keys = new[] { KeyCode.A, KeyCode.W, KeyCode.S, KeyCode.D, KeyCode.Space, KeyCode.Enter };
        
        while (!cancellationToken.IsCancellationRequested && IsRunning)
        {
            try
            {
                await Task.Delay(1000, cancellationToken); // Simulate key event every second
                
                if (random.NextDouble() < 0.1) // 10% chance of key event
                {
                    var key = keys[random.Next(keys.Length)];
                    var shouldPress = random.NextDouble() < 0.5;
                    
                    if (shouldPress && _keyStates[key] == InputState.Released)
                    {
                        SimulateKeyPress(key);
                    }
                    else if (!shouldPress && (_keyStates[key] == InputState.Pressed || _keyStates[key] == InputState.Held))
                    {
                        SimulateKeyRelease(key);
                    }
                }
                
                // Update held keys
                UpdateKeyStates();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in keyboard input simulation");
            }
        }
    }

    private void SimulateKeyPress(KeyCode key)
    {
        lock (_eventLock)
        {
            var now = DateTime.UtcNow;
            _keyStates[key] = InputState.Pressed;
            _keyPressTimestamps[key] = now;
            
            var keyEvent = new KeyEvent(key, InputState.Pressed, KeyModifiers.None, now, GetCurrentFrame());
            PublishKeyEvent(keyEvent);
            
            _logger.LogTrace("Simulated key press: {Key}", key);
        }
    }

    private void SimulateKeyRelease(KeyCode key)
    {
        lock (_eventLock)
        {
            var now = DateTime.UtcNow;
            _keyStates[key] = InputState.JustReleased;
            
            var keyEvent = new KeyEvent(key, InputState.JustReleased, KeyModifiers.None, now, GetCurrentFrame());
            PublishKeyEvent(keyEvent);
            
            _logger.LogTrace("Simulated key release: {Key}", key);
        }
    }

    private void UpdateKeyStates()
    {
        lock (_eventLock)
        {
            var keysToUpdate = _keyStates
                .Where(kvp => kvp.Value == InputState.Pressed || kvp.Value == InputState.JustReleased)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToUpdate)
            {
                var currentState = _keyStates[key];
                if (currentState == InputState.Pressed)
                {
                    _keyStates[key] = InputState.Held;
                }
                else if (currentState == InputState.JustReleased)
                {
                    _keyStates[key] = InputState.Released;
                }
            }
            
            IncrementFrame();
        }
    }
}