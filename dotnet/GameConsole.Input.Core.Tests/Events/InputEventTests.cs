using GameConsole.Input.Core.Events;
using GameConsole.Input.Core.Types;
using Xunit;

namespace GameConsole.Input.Core.Tests.Events;

public class KeyEventTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var deviceId = "keyboard-1";
        var keyCode = KeyCode.A;
        var state = InputState.Pressed;
        var modifiers = KeyModifiers.Shift | KeyModifiers.Control;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var keyEvent = new KeyEvent(deviceId, keyCode, state, modifiers, timestamp);

        // Assert
        Assert.Equal(deviceId, keyEvent.DeviceId);
        Assert.Equal(keyCode, keyEvent.KeyCode);
        Assert.Equal(state, keyEvent.State);
        Assert.Equal(modifiers, keyEvent.Modifiers);
        Assert.Equal(timestamp, keyEvent.Timestamp);
        Assert.False(keyEvent.IsHandled);
    }

    [Fact]
    public void Constructor_WithoutModifiers_DefaultsToNone()
    {
        // Arrange
        var deviceId = "keyboard-1";
        var keyCode = KeyCode.Space;
        var state = InputState.Released;

        // Act
        var keyEvent = new KeyEvent(deviceId, keyCode, state);

        // Assert
        Assert.Equal(KeyModifiers.None, keyEvent.Modifiers);
    }

    [Fact]
    public void Constructor_WithoutTimestamp_SetsCurrentTime()
    {
        // Arrange
        var deviceId = "keyboard-1";
        var keyCode = KeyCode.Enter;
        var state = InputState.Held;
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var keyEvent = new KeyEvent(deviceId, keyCode, state);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(keyEvent.Timestamp >= beforeCreation);
        Assert.True(keyEvent.Timestamp <= afterCreation);
    }

    [Theory]
    [InlineData(InputState.Pressed, true, false, false)]
    [InlineData(InputState.Released, false, true, false)]
    [InlineData(InputState.Held, false, false, true)]
    public void StateProperties_ReflectCorrectState(InputState state, bool isPressed, bool isReleased, bool isHeld)
    {
        // Arrange
        var keyEvent = new KeyEvent("keyboard-1", KeyCode.A, state);

        // Act & Assert
        Assert.Equal(isPressed, keyEvent.IsPressed);
        Assert.Equal(isReleased, keyEvent.IsReleased);
        Assert.Equal(isHeld, keyEvent.IsHeld);
    }

    [Fact]
    public void IsHandled_CanBeSetAndGet()
    {
        // Arrange
        var keyEvent = new KeyEvent("keyboard-1", KeyCode.A, InputState.Pressed);
        Assert.False(keyEvent.IsHandled);

        // Act
        keyEvent.IsHandled = true;

        // Assert
        Assert.True(keyEvent.IsHandled);
    }

    [Fact]
    public void Constructor_NullDeviceId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new KeyEvent(null!, KeyCode.A, InputState.Pressed));
    }

    [Theory]
    [InlineData(KeyModifiers.None)]
    [InlineData(KeyModifiers.Shift)]
    [InlineData(KeyModifiers.Control)]
    [InlineData(KeyModifiers.Alt)]
    [InlineData(KeyModifiers.Command)]
    [InlineData(KeyModifiers.Shift | KeyModifiers.Control)]
    [InlineData(KeyModifiers.Alt | KeyModifiers.Command)]
    public void Modifiers_CanBeSetToAnyValidCombination(KeyModifiers modifiers)
    {
        // Act
        var keyEvent = new KeyEvent("keyboard-1", KeyCode.A, InputState.Pressed, modifiers);

        // Assert
        Assert.Equal(modifiers, keyEvent.Modifiers);
    }
}

public class MouseEventTests
{
    [Fact]
    public void Constructor_ButtonEvent_SetsAllProperties()
    {
        // Arrange
        var deviceId = "mouse-1";
        var eventType = MouseEventType.ButtonPress;
        var position = new Vector2(100f, 200f);
        var button = MouseButton.Left;
        var state = InputState.Pressed;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var mouseEvent = new MouseEvent(deviceId, eventType, position, button, state, timestamp: timestamp);

        // Assert
        Assert.Equal(deviceId, mouseEvent.DeviceId);
        Assert.Equal(eventType, mouseEvent.EventType);
        Assert.Equal(position, mouseEvent.Position);
        Assert.Equal(button, mouseEvent.Button);
        Assert.Equal(state, mouseEvent.State);
        Assert.Equal(Vector2.Zero, mouseEvent.ScrollDelta);
        Assert.Equal(timestamp, mouseEvent.Timestamp);
    }

    [Fact]
    public void Constructor_ScrollEvent_SetsScrollDelta()
    {
        // Arrange
        var deviceId = "mouse-1";
        var eventType = MouseEventType.Scroll;
        var position = new Vector2(150f, 250f);
        var scrollDelta = new Vector2(0f, 120f);

        // Act
        var mouseEvent = new MouseEvent(deviceId, eventType, position, scrollDelta: scrollDelta);

        // Assert
        Assert.Equal(scrollDelta, mouseEvent.ScrollDelta);
        Assert.Null(mouseEvent.Button);
        Assert.Null(mouseEvent.State);
    }

    [Fact]
    public void Constructor_MoveEvent_NoButtonOrState()
    {
        // Arrange
        var deviceId = "mouse-1";
        var eventType = MouseEventType.Move;
        var position = new Vector2(300f, 400f);

        // Act
        var mouseEvent = new MouseEvent(deviceId, eventType, position);

        // Assert
        Assert.Equal(eventType, mouseEvent.EventType);
        Assert.Equal(position, mouseEvent.Position);
        Assert.Null(mouseEvent.Button);
        Assert.Null(mouseEvent.State);
        Assert.Equal(Vector2.Zero, mouseEvent.ScrollDelta);
    }

    [Theory]
    [InlineData(MouseEventType.ButtonPress, InputState.Pressed, true, false)]
    [InlineData(MouseEventType.ButtonRelease, InputState.Released, false, true)]
    [InlineData(MouseEventType.Move, null, false, false)]
    [InlineData(MouseEventType.Scroll, null, false, false)]
    public void StateProperties_ReflectCorrectEventType(MouseEventType eventType, InputState? state, bool isPressed, bool isReleased)
    {
        // Arrange
        var mouseEvent = new MouseEvent("mouse-1", eventType, Vector2.Zero, MouseButton.Left, state);

        // Act & Assert
        Assert.Equal(isPressed, mouseEvent.IsButtonPressed);
        Assert.Equal(isReleased, mouseEvent.IsButtonReleased);
    }

    [Theory]
    [InlineData(MouseEventType.Move, true, false)]
    [InlineData(MouseEventType.Scroll, false, true)]
    [InlineData(MouseEventType.ButtonPress, false, false)]
    [InlineData(MouseEventType.ButtonRelease, false, false)]
    public void EventTypeProperties_ReflectCorrectType(MouseEventType eventType, bool isMove, bool isScroll)
    {
        // Arrange
        var mouseEvent = new MouseEvent("mouse-1", eventType, Vector2.Zero);

        // Act & Assert
        Assert.Equal(isMove, mouseEvent.IsMove);
        Assert.Equal(isScroll, mouseEvent.IsScroll);
    }
}

public class GamepadEventTests
{
    [Fact]
    public void Constructor_ButtonEvent_SetsAllProperties()
    {
        // Arrange
        var deviceId = "gamepad-1";
        var playerId = 0;
        var button = GamepadButton.FaceButtonSouth;
        var state = InputState.Pressed;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var gamepadEvent = new GamepadEvent(deviceId, playerId, button, state, timestamp);

        // Assert
        Assert.Equal(deviceId, gamepadEvent.DeviceId);
        Assert.Equal(playerId, gamepadEvent.PlayerId);
        Assert.Equal(GamepadEventType.Button, gamepadEvent.EventType);
        Assert.Equal(button, gamepadEvent.Button);
        Assert.Equal(state, gamepadEvent.ButtonState);
        Assert.Null(gamepadEvent.Axis);
        Assert.Null(gamepadEvent.AxisValue);
        Assert.Equal(timestamp, gamepadEvent.Timestamp);
    }

    [Fact]
    public void Constructor_AxisEvent_SetsAllProperties()
    {
        // Arrange
        var deviceId = "gamepad-1";
        var playerId = 1;
        var axis = GamepadAxis.LeftStickX;
        var value = 0.75f;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var gamepadEvent = new GamepadEvent(deviceId, playerId, axis, value, timestamp);

        // Assert
        Assert.Equal(deviceId, gamepadEvent.DeviceId);
        Assert.Equal(playerId, gamepadEvent.PlayerId);
        Assert.Equal(GamepadEventType.Axis, gamepadEvent.EventType);
        Assert.Equal(axis, gamepadEvent.Axis);
        Assert.Equal(value, gamepadEvent.AxisValue);
        Assert.Null(gamepadEvent.Button);
        Assert.Null(gamepadEvent.ButtonState);
        Assert.Equal(timestamp, gamepadEvent.Timestamp);
    }

    [Theory]
    [InlineData(true, GamepadEventType.Connected)]
    [InlineData(false, GamepadEventType.Disconnected)]
    public void Constructor_ConnectionEvent_SetsEventType(bool connected, GamepadEventType expectedType)
    {
        // Arrange
        var deviceId = "gamepad-1";
        var playerId = 2;

        // Act
        var gamepadEvent = new GamepadEvent(deviceId, playerId, connected);

        // Assert
        Assert.Equal(expectedType, gamepadEvent.EventType);
        Assert.Equal(playerId, gamepadEvent.PlayerId);
        Assert.Null(gamepadEvent.Button);
        Assert.Null(gamepadEvent.ButtonState);
        Assert.Null(gamepadEvent.Axis);
        Assert.Null(gamepadEvent.AxisValue);
    }

    [Theory]
    [InlineData(GamepadEventType.Button, true, false, false)]
    [InlineData(GamepadEventType.Axis, false, true, false)]
    [InlineData(GamepadEventType.Connected, false, false, true)]
    [InlineData(GamepadEventType.Disconnected, false, false, true)]
    public void EventTypeProperties_ReflectCorrectType(GamepadEventType eventType, bool isButton, bool isAxis, bool isConnection)
    {
        // Arrange
        GamepadEvent gamepadEvent = eventType switch
        {
            GamepadEventType.Button => new GamepadEvent("gamepad-1", 0, GamepadButton.FaceButtonSouth, InputState.Pressed),
            GamepadEventType.Axis => new GamepadEvent("gamepad-1", 0, GamepadAxis.LeftStickX, 0.5f),
            GamepadEventType.Connected => new GamepadEvent("gamepad-1", 0, true),
            GamepadEventType.Disconnected => new GamepadEvent("gamepad-1", 0, false),
            _ => throw new ArgumentOutOfRangeException(nameof(eventType))
        };

        // Act & Assert
        Assert.Equal(isButton, gamepadEvent.IsButtonEvent);
        Assert.Equal(isAxis, gamepadEvent.IsAxisEvent);
        Assert.Equal(isConnection, gamepadEvent.IsConnectionEvent);
    }

    [Theory]
    [InlineData(InputState.Pressed, true, false)]
    [InlineData(InputState.Released, false, true)]
    [InlineData(InputState.Held, false, false)]
    public void ButtonStateProperties_ReflectCorrectState(InputState state, bool isPressed, bool isReleased)
    {
        // Arrange
        var gamepadEvent = new GamepadEvent("gamepad-1", 0, GamepadButton.FaceButtonSouth, state);

        // Act & Assert
        Assert.Equal(isPressed, gamepadEvent.IsButtonPressed);
        Assert.Equal(isReleased, gamepadEvent.IsButtonReleased);
    }
}