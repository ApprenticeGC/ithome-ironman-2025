namespace GameConsole.Input.Core;

/// <summary>
/// Represents keyboard key codes for input handling.
/// </summary>
public enum KeyCode
{
    None = 0,
    
    // Function keys
    F1 = 1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    
    // Number keys
    Alpha0 = 20, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
    
    // Letter keys
    A = 40, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    
    // Arrow keys
    UpArrow = 70, DownArrow, LeftArrow, RightArrow,
    
    // Modifiers
    LeftShift = 80, RightShift, LeftControl, RightControl, LeftAlt, RightAlt,
    LeftCommand = 86, RightCommand,
    
    // Special keys
    Space = 100, Enter, Escape, Tab, Backspace, Delete,
    Insert = 106, Home, End, PageUp, PageDown,
    
    // Numpad
    Keypad0 = 120, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
    KeypadDivide = 130, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter, KeypadPeriod,
    
    // Punctuation
    Semicolon = 140, Equals, Comma, Minus, Period, Slash, BackQuote,
    LeftBracket = 147, Backslash, RightBracket, Quote
}

/// <summary>
/// Represents mouse button identifiers.
/// </summary>
public enum MouseButton
{
    /// <summary>
    /// Left mouse button (primary button).
    /// </summary>
    Left = 0,
    
    /// <summary>
    /// Right mouse button (secondary button).
    /// </summary>
    Right = 1,
    
    /// <summary>
    /// Middle mouse button (wheel button).
    /// </summary>
    Middle = 2,
    
    /// <summary>
    /// Fourth mouse button (back button).
    /// </summary>
    Button4 = 3,
    
    /// <summary>
    /// Fifth mouse button (forward button).
    /// </summary>
    Button5 = 4
}

/// <summary>
/// Represents gamepad button identifiers following standard controller layouts.
/// </summary>
public enum GamepadButton
{
    /// <summary>
    /// Face button A (bottom button on most controllers).
    /// </summary>
    A = 0,
    
    /// <summary>
    /// Face button B (right button on most controllers).
    /// </summary>
    B = 1,
    
    /// <summary>
    /// Face button X (left button on most controllers).
    /// </summary>
    X = 2,
    
    /// <summary>
    /// Face button Y (top button on most controllers).
    /// </summary>
    Y = 3,
    
    /// <summary>
    /// Left shoulder button.
    /// </summary>
    LeftShoulder = 4,
    
    /// <summary>
    /// Right shoulder button.
    /// </summary>
    RightShoulder = 5,
    
    /// <summary>
    /// Back/Select button.
    /// </summary>
    Back = 6,
    
    /// <summary>
    /// Start button.
    /// </summary>
    Start = 7,
    
    /// <summary>
    /// Left stick button (clicking left analog stick).
    /// </summary>
    LeftStick = 8,
    
    /// <summary>
    /// Right stick button (clicking right analog stick).
    /// </summary>
    RightStick = 9,
    
    /// <summary>
    /// Directional pad up.
    /// </summary>
    DPadUp = 10,
    
    /// <summary>
    /// Directional pad down.
    /// </summary>
    DPadDown = 11,
    
    /// <summary>
    /// Directional pad left.
    /// </summary>
    DPadLeft = 12,
    
    /// <summary>
    /// Directional pad right.
    /// </summary>
    DPadRight = 13
}

/// <summary>
/// Represents gamepad analog axis identifiers.
/// </summary>
public enum GamepadAxis
{
    /// <summary>
    /// Left stick horizontal axis.
    /// </summary>
    LeftStickX = 0,
    
    /// <summary>
    /// Left stick vertical axis.
    /// </summary>
    LeftStickY = 1,
    
    /// <summary>
    /// Right stick horizontal axis.
    /// </summary>
    RightStickX = 2,
    
    /// <summary>
    /// Right stick vertical axis.
    /// </summary>
    RightStickY = 3,
    
    /// <summary>
    /// Left trigger analog value.
    /// </summary>
    LeftTrigger = 4,
    
    /// <summary>
    /// Right trigger analog value.
    /// </summary>
    RightTrigger = 5
}