using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Input.Services;

/// <summary>
/// Gamepad input service for handling controller input events and state.
/// Supports multiple controller types with hot-plugging detection.
/// </summary>
[Service("Gamepad Input", "1.0.0", "Handles gamepad and controller input events and state polling", 
    Categories = new[] { "Input", "Gamepad", "Controller" }, Lifetime = ServiceLifetime.Singleton)]
public class GamepadInputService : BaseInputService
{
    private readonly ConcurrentDictionary<int, GamepadState> _gamepadStates;
    private readonly ConcurrentDictionary<int, string> _gamepadNames;
    private readonly object _eventLock = new object();
    
    private class GamepadState
    {
        public ConcurrentDictionary<GamepadButton, InputState> ButtonStates { get; } = new();
        public ConcurrentDictionary<GamepadAxis, float> AxisValues { get; } = new();
        public bool IsConnected { get; set; } = false;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        public GamepadState()
        {
            // Initialize button states
            foreach (GamepadButton button in Enum.GetValues<GamepadButton>())
            {
                ButtonStates[button] = InputState.Released;
            }
            
            // Initialize axis values
            foreach (GamepadAxis axis in Enum.GetValues<GamepadAxis>())
            {
                AxisValues[axis] = 0.0f;
            }
        }
    }

    public GamepadInputService(ILogger<GamepadInputService> logger) : base(logger)
    {
        _gamepadStates = new ConcurrentDictionary<int, GamepadState>();
        _gamepadNames = new ConcurrentDictionary<int, string>();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing gamepad input monitoring");
        
        // Initialize support for up to 4 gamepads
        for (int i = 0; i < 4; i++)
        {
            _gamepadStates[i] = new GamepadState();
            _gamepadNames[i] = $"Gamepad {i + 1}";
        }

        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting gamepad input polling");
        
        // Simulate some connected gamepads
        _gamepadStates[0].IsConnected = true;
        _gamepadNames[0] = "Xbox Controller";
        
        // In a real implementation, this would start gamepad event monitoring
        _ = Task.Run(async () => await SimulateGamepadInputAsync(cancellationToken), cancellationToken);
        
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping gamepad input monitoring");
        await Task.CompletedTask;
    }

    public override Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default)
    {
        // Gamepad service doesn't handle keyboard
        return Task.FromResult(false);
    }

    public override Task<Vector2> GetMousePositionAsync(CancellationToken cancellationToken = default)
    {
        // Gamepad service doesn't handle mouse
        return Task.FromResult(Vector2.Zero);
    }

    public override Task<bool> IsMouseButtonPressedAsync(MouseButton button, CancellationToken cancellationToken = default)
    {
        // Gamepad service doesn't handle mouse
        return Task.FromResult(false);
    }

    public override Task<bool> IsGamepadButtonPressedAsync(int gamepadIndex, GamepadButton button, CancellationToken cancellationToken = default)
    {
        if (!IsRunning || !_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState) || !gamepadState.IsConnected)
            return Task.FromResult(false);

        var state = gamepadState.ButtonStates.GetValueOrDefault(button, InputState.Released);
        var isPressed = state == InputState.Pressed || state == InputState.Held;
        
        _logger.LogTrace("Gamepad {Index} button {Button} state check: {IsPressed}", gamepadIndex, button, isPressed);
        return Task.FromResult(isPressed);
    }

    public override Task<float> GetGamepadAxisAsync(int gamepadIndex, GamepadAxis axis, CancellationToken cancellationToken = default)
    {
        if (!IsRunning || !_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState) || !gamepadState.IsConnected)
            return Task.FromResult(0.0f);

        var value = gamepadState.AxisValues.GetValueOrDefault(axis, 0.0f);
        
        _logger.LogTrace("Gamepad {Index} axis {Axis} value: {Value}", gamepadIndex, axis, value);
        return Task.FromResult(value);
    }

    public override Task<int> GetConnectedGamepadCountAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            return Task.FromResult(0);

        var count = _gamepadStates.Count(kvp => kvp.Value.IsConnected);
        
        _logger.LogTrace("Connected gamepad count: {Count}", count);
        return Task.FromResult(count);
    }

    public override Task<bool> IsGamepadConnectedAsync(int gamepadIndex, CancellationToken cancellationToken = default)
    {
        if (!IsRunning || !_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState))
            return Task.FromResult(false);

        var isConnected = gamepadState.IsConnected;
        
        _logger.LogTrace("Gamepad {Index} connected check: {IsConnected}", gamepadIndex, isConnected);
        return Task.FromResult(isConnected);
    }

    public override Task<string?> GetGamepadNameAsync(int gamepadIndex, CancellationToken cancellationToken = default)
    {
        if (!IsRunning || !_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState) || !gamepadState.IsConnected)
            return Task.FromResult<string?>(null);

        var name = _gamepadNames.GetValueOrDefault(gamepadIndex);
        
        _logger.LogTrace("Gamepad {Index} name: {Name}", gamepadIndex, name);
        return Task.FromResult<string?>(name);
    }

    /// <summary>
    /// Simulates gamepad input for demonstration purposes.
    /// In a real implementation, this would hook into the OS gamepad events.
    /// </summary>
    private async Task SimulateGamepadInputAsync(CancellationToken cancellationToken)
    {
        var random = new Random();
        var buttons = Enum.GetValues<GamepadButton>();
        var axes = Enum.GetValues<GamepadAxis>();
        
        while (!cancellationToken.IsCancellationRequested && IsRunning)
        {
            try
            {
                await Task.Delay(200, cancellationToken); // Update gamepad state
                
                foreach (var (gamepadIndex, gamepadState) in _gamepadStates.Where(kvp => kvp.Value.IsConnected))
                {
                    // Simulate button events
                    if (random.NextDouble() < 0.1) // 10% chance of button event
                    {
                        var button = buttons[random.Next(buttons.Length)];
                        var shouldPress = random.NextDouble() < 0.5;
                        
                        if (shouldPress && gamepadState.ButtonStates[button] == InputState.Released)
                        {
                            SimulateButtonPress(gamepadIndex, button);
                        }
                        else if (!shouldPress && (gamepadState.ButtonStates[button] == InputState.Pressed || gamepadState.ButtonStates[button] == InputState.Held))
                        {
                            SimulateButtonRelease(gamepadIndex, button);
                        }
                    }
                    
                    // Simulate axis changes
                    if (random.NextDouble() < 0.3) // 30% chance of axis change
                    {
                        var axis = axes[random.Next(axes.Length)];
                        var newValue = (float)(random.NextDouble() * 2.0 - 1.0); // -1.0 to 1.0
                        
                        // Apply deadzone
                        if (Math.Abs(newValue) < 0.1f)
                            newValue = 0.0f;
                            
                        SimulateAxisChange(gamepadIndex, axis, newValue);
                    }
                }
                
                // Simulate hot-plugging occasionally
                if (random.NextDouble() < 0.01) // 1% chance of connection change
                {
                    SimulateConnectionChange();
                }
                
                // Update button states
                UpdateGamepadStates();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in gamepad input simulation");
            }
        }
    }

    private void SimulateButtonPress(int gamepadIndex, GamepadButton button)
    {
        lock (_eventLock)
        {
            if (!_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState))
                return;

            var now = DateTime.UtcNow;
            gamepadState.ButtonStates[button] = InputState.Pressed;
            gamepadState.LastUpdate = now;
            
            var gamepadEvent = new GamepadEvent(gamepadIndex, button, InputState.Pressed, null, null, now, GetCurrentFrame());
            PublishGamepadEvent(gamepadEvent);
            
            _logger.LogTrace("Simulated gamepad {Index} button press: {Button}", gamepadIndex, button);
        }
    }

    private void SimulateButtonRelease(int gamepadIndex, GamepadButton button)
    {
        lock (_eventLock)
        {
            if (!_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState))
                return;

            var now = DateTime.UtcNow;
            gamepadState.ButtonStates[button] = InputState.JustReleased;
            gamepadState.LastUpdate = now;
            
            var gamepadEvent = new GamepadEvent(gamepadIndex, button, InputState.JustReleased, null, null, now, GetCurrentFrame());
            PublishGamepadEvent(gamepadEvent);
            
            _logger.LogTrace("Simulated gamepad {Index} button release: {Button}", gamepadIndex, button);
        }
    }

    private void SimulateAxisChange(int gamepadIndex, GamepadAxis axis, float newValue)
    {
        lock (_eventLock)
        {
            if (!_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState))
                return;

            var oldValue = gamepadState.AxisValues[axis];
            if (Math.Abs(newValue - oldValue) < 0.01f) // Ignore tiny changes
                return;

            var now = DateTime.UtcNow;
            gamepadState.AxisValues[axis] = newValue;
            gamepadState.LastUpdate = now;
            
            var gamepadEvent = new GamepadEvent(gamepadIndex, null, null, axis, newValue, now, GetCurrentFrame());
            PublishGamepadEvent(gamepadEvent);
            
            _logger.LogTrace("Simulated gamepad {Index} axis change: {Axis} = {Value}", gamepadIndex, axis, newValue);
        }
    }

    private void SimulateConnectionChange()
    {
        lock (_eventLock)
        {
            var random = new Random();
            var gamepadIndex = random.Next(4);
            
            if (!_gamepadStates.TryGetValue(gamepadIndex, out var gamepadState))
                return;

            var wasConnected = gamepadState.IsConnected;
            gamepadState.IsConnected = !wasConnected;
            
            if (gamepadState.IsConnected)
            {
                _gamepadNames[gamepadIndex] = $"Controller {gamepadIndex + 1}";
                _logger.LogInformation("Gamepad {Index} connected: {Name}", gamepadIndex, _gamepadNames[gamepadIndex]);
            }
            else
            {
                _logger.LogInformation("Gamepad {Index} disconnected", gamepadIndex);
            }
        }
    }

    private void UpdateGamepadStates()
    {
        lock (_eventLock)
        {
            foreach (var (gamepadIndex, gamepadState) in _gamepadStates.Where(kvp => kvp.Value.IsConnected))
            {
                var buttonsToUpdate = gamepadState.ButtonStates
                    .Where(kvp => kvp.Value == InputState.Pressed || kvp.Value == InputState.JustReleased)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var button in buttonsToUpdate)
                {
                    var currentState = gamepadState.ButtonStates[button];
                    if (currentState == InputState.Pressed)
                    {
                        gamepadState.ButtonStates[button] = InputState.Held;
                    }
                    else if (currentState == InputState.JustReleased)
                    {
                        gamepadState.ButtonStates[button] = InputState.Released;
                    }
                }
            }
            
            IncrementFrame();
        }
    }
}