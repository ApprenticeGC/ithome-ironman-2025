using GameConsole.Core.Abstractions;
using GameConsole.Input.Core;
using GameConsole.Input.Core.Devices;
using GameConsole.Input.Core.Events;
using GameConsole.Input.Core.Types;
using Xunit;

namespace GameConsole.Input.Core.Tests;

public class IInputServiceTests
{
    [Fact]
    public void IInputService_InheritsFromRequiredInterfaces()
    {
        // Assert that IInputService has the correct base interfaces
        Assert.True(typeof(IService).IsAssignableFrom(typeof(IInputService)));
        Assert.True(typeof(ICapabilityProvider).IsAssignableFrom(typeof(IInputService)));
    }

    [Fact]
    public void DeviceConnectionEvent_Constructor_SetsAllProperties()
    {
        // Arrange
        var mockDevice = new MockInputDevice("test-device", "Test Device", InputDeviceType.Keyboard);
        var connected = true;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var connectionEvent = new DeviceConnectionEvent(mockDevice, connected, timestamp);

        // Assert
        Assert.Equal(mockDevice, connectionEvent.Device);
        Assert.Equal(connected, connectionEvent.Connected);
        Assert.Equal(timestamp, connectionEvent.Timestamp);
        Assert.Equal(mockDevice.DeviceId, connectionEvent.DeviceId);
        Assert.True(connectionEvent.IsConnection);
        Assert.False(connectionEvent.IsDisconnection);
    }

    [Fact]
    public void DeviceConnectionEvent_DisconnectionEvent_HasCorrectProperties()
    {
        // Arrange
        var mockDevice = new MockInputDevice("test-device", "Test Device", InputDeviceType.Mouse);
        var connected = false;

        // Act
        var connectionEvent = new DeviceConnectionEvent(mockDevice, connected);

        // Assert
        Assert.False(connectionEvent.Connected);
        Assert.False(connectionEvent.IsConnection);
        Assert.True(connectionEvent.IsDisconnection);
    }

    [Fact]
    public void DeviceConnectionEvent_NullDevice_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DeviceConnectionEvent(null!, true));
    }

    [Fact]
    public void InputStateInfo_Constructor_SetsAllProperties()
    {
        // Arrange
        var isPressed = true;
        var pressedThisFrame = false;
        var releasedThisFrame = true;
        var pressDuration = TimeSpan.FromMilliseconds(500);
        var lastPressTime = DateTimeOffset.UtcNow.AddMilliseconds(-500);
        var lastReleaseTime = DateTimeOffset.UtcNow;

        // Act
        var stateInfo = new InputStateInfo(
            isPressed, 
            pressedThisFrame, 
            releasedThisFrame, 
            pressDuration, 
            lastPressTime, 
            lastReleaseTime);

        // Assert
        Assert.Equal(isPressed, stateInfo.IsPressed);
        Assert.Equal(pressedThisFrame, stateInfo.PressedThisFrame);
        Assert.Equal(releasedThisFrame, stateInfo.ReleasedThisFrame);
        Assert.Equal(pressDuration, stateInfo.PressDuration);
        Assert.Equal(lastPressTime, stateInfo.LastPressTime);
        Assert.Equal(lastReleaseTime, stateInfo.LastReleaseTime);
    }

    [Fact]
    public void InputDeviceType_HasExpectedValues()
    {
        // Verify the enum has the expected values
        Assert.True(Enum.IsDefined(typeof(InputDeviceType), InputDeviceType.Keyboard));
        Assert.True(Enum.IsDefined(typeof(InputDeviceType), InputDeviceType.Mouse));
        Assert.True(Enum.IsDefined(typeof(InputDeviceType), InputDeviceType.Gamepad));
        Assert.True(Enum.IsDefined(typeof(InputDeviceType), InputDeviceType.Touch));
        Assert.True(Enum.IsDefined(typeof(InputDeviceType), InputDeviceType.Unknown));
    }
}

/// <summary>
/// Mock implementation of IInputDevice for testing purposes.
/// </summary>
public class MockInputDevice : IInputDevice
{
    public MockInputDevice(string deviceId, string deviceName, InputDeviceType deviceType)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceType = deviceType;
        IsConnected = true;
        LastActivity = DateTimeOffset.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    public string DeviceId { get; }
    public string DeviceName { get; }
    public InputDeviceType DeviceType { get; }
    public bool IsConnected { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Mock implementation of IKeyboard for testing purposes.
/// </summary>
public class MockKeyboard : MockInputDevice, IKeyboard
{
    private readonly Dictionary<KeyCode, InputStateInfo> _keyStates = new();

    public MockKeyboard(string deviceId) 
        : base(deviceId, "Mock Keyboard", InputDeviceType.Keyboard)
    {
    }

    public IReadOnlyDictionary<KeyCode, InputStateInfo> KeyStates => _keyStates;

    public bool IsKeyPressed(KeyCode keyCode)
    {
        return _keyStates.TryGetValue(keyCode, out var state) && state.IsPressed;
    }

    public bool IsKeyDown(KeyCode keyCode)
    {
        return _keyStates.TryGetValue(keyCode, out var state) && state.PressedThisFrame;
    }

    public bool IsKeyUp(KeyCode keyCode)
    {
        return _keyStates.TryGetValue(keyCode, out var state) && state.ReleasedThisFrame;
    }

    public KeyModifiers GetModifiers()
    {
        var modifiers = KeyModifiers.None;
        
        if (IsKeyPressed(KeyCode.LeftShift) || IsKeyPressed(KeyCode.RightShift))
            modifiers |= KeyModifiers.Shift;
        
        if (IsKeyPressed(KeyCode.LeftControl) || IsKeyPressed(KeyCode.RightControl))
            modifiers |= KeyModifiers.Control;
        
        if (IsKeyPressed(KeyCode.LeftAlt) || IsKeyPressed(KeyCode.RightAlt))
            modifiers |= KeyModifiers.Alt;
        
        if (IsKeyPressed(KeyCode.LeftCommand) || IsKeyPressed(KeyCode.RightCommand))
            modifiers |= KeyModifiers.Command;

        return modifiers;
    }

    public void SetKeyState(KeyCode keyCode, bool pressed)
    {
        var now = DateTimeOffset.UtcNow;
        var currentState = _keyStates.TryGetValue(keyCode, out var existing) ? existing : new InputStateInfo();
        
        _keyStates[keyCode] = new InputStateInfo(
            pressed,
            pressed && !currentState.IsPressed, // pressed this frame
            !pressed && currentState.IsPressed, // released this frame
            pressed ? (currentState.IsPressed ? currentState.PressDuration.Add(TimeSpan.FromMilliseconds(16)) : TimeSpan.Zero) : TimeSpan.Zero,
            pressed && !currentState.IsPressed ? now : currentState.LastPressTime,
            !pressed && currentState.IsPressed ? now : currentState.LastReleaseTime
        );
    }
}

/// <summary>
/// Mock implementation of IMouse for testing purposes.
/// </summary>
public class MockMouse : MockInputDevice, IMouse
{
    private readonly Dictionary<MouseButton, InputStateInfo> _buttonStates = new();

    public MockMouse(string deviceId) 
        : base(deviceId, "Mock Mouse", InputDeviceType.Mouse)
    {
        Position = Vector2.Zero;
        Delta = Vector2.Zero;
        ScrollDelta = Vector2.Zero;
    }

    public Vector2 Position { get; set; }
    public Vector2 Delta { get; set; }
    public Vector2 ScrollDelta { get; set; }
    public IReadOnlyDictionary<MouseButton, InputStateInfo> ButtonStates => _buttonStates;

    public bool IsButtonPressed(MouseButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) && state.IsPressed;
    }

    public bool IsButtonDown(MouseButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) && state.PressedThisFrame;
    }

    public bool IsButtonUp(MouseButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) && state.ReleasedThisFrame;
    }

    public void SetButtonState(MouseButton button, bool pressed)
    {
        var now = DateTimeOffset.UtcNow;
        var currentState = _buttonStates.TryGetValue(button, out var existing) ? existing : new InputStateInfo();
        
        _buttonStates[button] = new InputStateInfo(
            pressed,
            pressed && !currentState.IsPressed,
            !pressed && currentState.IsPressed,
            pressed ? (currentState.IsPressed ? currentState.PressDuration.Add(TimeSpan.FromMilliseconds(16)) : TimeSpan.Zero) : TimeSpan.Zero,
            pressed && !currentState.IsPressed ? now : currentState.LastPressTime,
            !pressed && currentState.IsPressed ? now : currentState.LastReleaseTime
        );
    }
}

/// <summary>
/// Mock implementation of IGamepad for testing purposes.
/// </summary>
public class MockGamepad : MockInputDevice, IGamepad
{
    private readonly Dictionary<GamepadButton, InputStateInfo> _buttonStates = new();
    private readonly Dictionary<GamepadAxis, float> _axisStates = new();

    public MockGamepad(string deviceId, int playerIndex) 
        : base(deviceId, "Mock Gamepad", InputDeviceType.Gamepad)
    {
        PlayerIndex = playerIndex;
    }

    public int PlayerIndex { get; }
    public IReadOnlyDictionary<GamepadButton, InputStateInfo> ButtonStates => _buttonStates;
    public IReadOnlyDictionary<GamepadAxis, float> AxisStates => _axisStates;

    public bool IsButtonPressed(GamepadButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) && state.IsPressed;
    }

    public bool IsButtonDown(GamepadButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) && state.PressedThisFrame;
    }

    public bool IsButtonUp(GamepadButton button)
    {
        return _buttonStates.TryGetValue(button, out var state) && state.ReleasedThisFrame;
    }

    public float GetAxisValue(GamepadAxis axis)
    {
        return _axisStates.TryGetValue(axis, out var value) ? value : 0f;
    }

    public Vector2 LeftStick => new(GetAxisValue(GamepadAxis.LeftStickX), GetAxisValue(GamepadAxis.LeftStickY));
    public Vector2 RightStick => new(GetAxisValue(GamepadAxis.RightStickX), GetAxisValue(GamepadAxis.RightStickY));
    public float LeftTrigger => GetAxisValue(GamepadAxis.LeftTrigger);
    public float RightTrigger => GetAxisValue(GamepadAxis.RightTrigger);

    public void SetButtonState(GamepadButton button, bool pressed)
    {
        var now = DateTimeOffset.UtcNow;
        var currentState = _buttonStates.TryGetValue(button, out var existing) ? existing : new InputStateInfo();
        
        _buttonStates[button] = new InputStateInfo(
            pressed,
            pressed && !currentState.IsPressed,
            !pressed && currentState.IsPressed,
            pressed ? (currentState.IsPressed ? currentState.PressDuration.Add(TimeSpan.FromMilliseconds(16)) : TimeSpan.Zero) : TimeSpan.Zero,
            pressed && !currentState.IsPressed ? now : currentState.LastPressTime,
            !pressed && currentState.IsPressed ? now : currentState.LastReleaseTime
        );
    }

    public void SetAxisValue(GamepadAxis axis, float value)
    {
        _axisStates[axis] = Math.Clamp(value, -1f, 1f);
    }
}