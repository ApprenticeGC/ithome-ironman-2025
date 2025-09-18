namespace GameConsole.Input.Core.Types;

/// <summary>
/// Represents keyboard key codes for input handling.
/// </summary>
public enum KeyCode
{
    /// <summary>Unknown or unmapped key.</summary>
    Unknown = 0,

    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Number keys
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Letter keys
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Arrow keys
    LeftArrow, RightArrow, UpArrow, DownArrow,

    // Modifier keys
    LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt,
    LeftCommand, RightCommand, LeftWindows, RightWindows,

    // Navigation keys
    Home, End, PageUp, PageDown, Insert, Delete, Backspace,

    // Special keys
    Tab, CapsLock, NumLock, ScrollLock, PrintScreen, Pause,
    Enter, Space, Escape,

    // Numpad keys
    Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
    KeypadPlus, KeypadMinus, KeypadMultiply, KeypadDivide, KeypadPeriod, KeypadEnter,

    // Punctuation and symbols
    Semicolon, Comma, Period, Slash, Backslash, Quote, Backtick,
    LeftBracket, RightBracket, Minus, Equals,

    // Additional function keys
    F13, F14, F15, F16, F17, F18, F19, F20, F21, F22, F23, F24
}

/// <summary>
/// Represents mouse button types.
/// </summary>
public enum MouseButton
{
    /// <summary>Left mouse button.</summary>
    Left = 0,
    /// <summary>Right mouse button.</summary>
    Right = 1,
    /// <summary>Middle mouse button (scroll wheel).</summary>
    Middle = 2,
    /// <summary>Fourth mouse button (back).</summary>
    Button4 = 3,
    /// <summary>Fifth mouse button (forward).</summary>
    Button5 = 4
}

/// <summary>
/// Represents gamepad button types.
/// </summary>
public enum GamepadButton
{
    /// <summary>Face button (A on Xbox, Cross on PlayStation).</summary>
    FaceButtonSouth,
    /// <summary>Face button (B on Xbox, Circle on PlayStation).</summary>
    FaceButtonEast,
    /// <summary>Face button (X on Xbox, Square on PlayStation).</summary>
    FaceButtonWest,
    /// <summary>Face button (Y on Xbox, Triangle on PlayStation).</summary>
    FaceButtonNorth,

    /// <summary>Left shoulder button.</summary>
    LeftShoulder,
    /// <summary>Right shoulder button.</summary>
    RightShoulder,

    /// <summary>Left analog stick button.</summary>
    LeftStick,
    /// <summary>Right analog stick button.</summary>
    RightStick,

    /// <summary>D-pad up.</summary>
    DPadUp,
    /// <summary>D-pad down.</summary>
    DPadDown,
    /// <summary>D-pad left.</summary>
    DPadLeft,
    /// <summary>D-pad right.</summary>
    DPadRight,

    /// <summary>Start button (Menu button on Xbox).</summary>
    Start,
    /// <summary>Select button (View button on Xbox).</summary>
    Select,

    /// <summary>System button (Xbox button, PlayStation button).</summary>
    System
}

/// <summary>
/// Represents gamepad axis types for analog inputs.
/// </summary>
public enum GamepadAxis
{
    /// <summary>Left stick horizontal axis.</summary>
    LeftStickX,
    /// <summary>Left stick vertical axis.</summary>
    LeftStickY,
    /// <summary>Right stick horizontal axis.</summary>
    RightStickX,
    /// <summary>Right stick vertical axis.</summary>
    RightStickY,
    /// <summary>Left trigger axis.</summary>
    LeftTrigger,
    /// <summary>Right trigger axis.</summary>
    RightTrigger
}