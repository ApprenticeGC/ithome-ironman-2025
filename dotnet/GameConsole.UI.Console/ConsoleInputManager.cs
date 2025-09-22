using GameConsole.Input.Core;

namespace GameConsole.UI.Console;

/// <summary>
/// Represents keyboard input event arguments.
/// </summary>
public class ConsoleInputEventArgs : EventArgs
{
    /// <summary>
    /// Initializes new console input event arguments.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="modifiers">Modifier keys state.</param>
    /// <param name="character">Character representation if available.</param>
    public ConsoleInputEventArgs(KeyCode key, KeyModifiers modifiers = KeyModifiers.None, char? character = null)
    {
        Key = key;
        Modifiers = modifiers;
        Character = character;
    }
    
    /// <summary>
    /// Gets the pressed key.
    /// </summary>
    public KeyCode Key { get; }
    
    /// <summary>
    /// Gets the modifier keys state.
    /// </summary>
    public KeyModifiers Modifiers { get; }
    
    /// <summary>
    /// Gets the character representation if available.
    /// </summary>
    public char? Character { get; }
    
    /// <summary>
    /// Gets or sets whether the input has been handled.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Represents modifier key states.
/// </summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>No modifier keys.</summary>
    None = 0,
    /// <summary>Shift key.</summary>
    Shift = 1,
    /// <summary>Control key.</summary>
    Control = 2,
    /// <summary>Alt key.</summary>
    Alt = 4,
    /// <summary>Command/Windows key.</summary>
    Command = 8
}

/// <summary>
/// Console input manager that handles keyboard interaction and integrates with input services.
/// </summary>
public class ConsoleInputManager
{
    private readonly TerminalInfo _terminalInfo;
    private readonly Dictionary<KeyCode, List<Action<ConsoleInputEventArgs>>> _keyHandlers = [];
    private readonly List<Func<ConsoleInputEventArgs, bool>> _globalHandlers = [];
    private bool _isCapturingInput = false;
    
    /// <summary>
    /// Initializes a new console input manager.
    /// </summary>
    /// <param name="terminalInfo">Terminal information.</param>
    public ConsoleInputManager(TerminalInfo terminalInfo)
    {
        _terminalInfo = terminalInfo ?? throw new ArgumentNullException(nameof(terminalInfo));
    }
    
    /// <summary>
    /// Event raised when a key is pressed.
    /// </summary>
    public event EventHandler<ConsoleInputEventArgs>? KeyPressed;
    
    /// <summary>
    /// Event raised when text input is received.
    /// </summary>
    public event EventHandler<string>? TextInput;
    
    /// <summary>
    /// Gets whether input capture is active.
    /// </summary>
    public bool IsCapturingInput => _isCapturingInput;
    
    /// <summary>
    /// Starts capturing keyboard input.
    /// </summary>
    public void StartCapture()
    {
        if (_isCapturingInput) return;
        
        _isCapturingInput = true;
        
        // Start background task to capture console input
        Task.Run(async () =>
        {
            while (_isCapturingInput)
            {
                try
                {
                    if (System.Console.KeyAvailable)
                    {
                        var consoleKeyInfo = System.Console.ReadKey(true);
                        await ProcessConsoleKey(consoleKeyInfo);
                    }
                    else
                    {
                        // Small delay to prevent busy waiting
                        await Task.Delay(10);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Console input not available, exit capture
                    break;
                }
                catch (Exception)
                {
                    // Handle other exceptions gracefully
                    await Task.Delay(100);
                }
            }
        });
    }
    
    /// <summary>
    /// Stops capturing keyboard input.
    /// </summary>
    public void StopCapture()
    {
        _isCapturingInput = false;
    }
    
    /// <summary>
    /// Registers a global input handler.
    /// </summary>
    /// <param name="handler">The handler function that returns true if the input was handled.</param>
    public void RegisterGlobalHandler(Func<ConsoleInputEventArgs, bool> handler)
    {
        if (handler != null)
            _globalHandlers.Add(handler);
    }
    
    /// <summary>
    /// Unregisters a global input handler.
    /// </summary>
    /// <param name="handler">The handler to remove.</param>
    /// <returns>True if the handler was found and removed; otherwise, false.</returns>
    public bool UnregisterGlobalHandler(Func<ConsoleInputEventArgs, bool> handler)
    {
        return _globalHandlers.Remove(handler);
    }
    
    /// <summary>
    /// Registers a handler for a specific key.
    /// </summary>
    /// <param name="key">The key to handle.</param>
    /// <param name="handler">The handler action.</param>
    public void RegisterKeyHandler(KeyCode key, Action<ConsoleInputEventArgs> handler)
    {
        if (handler == null) return;
        
        if (!_keyHandlers.ContainsKey(key))
            _keyHandlers[key] = [];
        
        _keyHandlers[key].Add(handler);
    }
    
    /// <summary>
    /// Unregisters a key handler.
    /// </summary>
    /// <param name="key">The key to unregister.</param>
    /// <param name="handler">The handler to remove.</param>
    /// <returns>True if the handler was found and removed; otherwise, false.</returns>
    public bool UnregisterKeyHandler(KeyCode key, Action<ConsoleInputEventArgs> handler)
    {
        if (_keyHandlers.TryGetValue(key, out var handlers))
        {
            var removed = handlers.Remove(handler);
            if (handlers.Count == 0)
                _keyHandlers.Remove(key);
            return removed;
        }
        return false;
    }
    
    /// <summary>
    /// Clears all registered handlers.
    /// </summary>
    public void ClearHandlers()
    {
        _keyHandlers.Clear();
        _globalHandlers.Clear();
    }
    
    /// <summary>
    /// Processes a keyboard input event.
    /// </summary>
    /// <param name="key">The key code.</param>
    /// <param name="modifiers">Modifier keys state.</param>
    /// <param name="character">Character representation if available.</param>
    /// <returns>True if the input was handled; otherwise, false.</returns>
    public bool ProcessInput(KeyCode key, KeyModifiers modifiers = KeyModifiers.None, char? character = null)
    {
        var args = new ConsoleInputEventArgs(key, modifiers, character);
        
        // Try global handlers first
        foreach (var handler in _globalHandlers)
        {
            try
            {
                if (handler(args))
                {
                    args.Handled = true;
                    break;
                }
            }
            catch (Exception)
            {
                // Continue with next handler if one throws
            }
        }
        
        // Try specific key handlers if not handled
        if (!args.Handled && _keyHandlers.TryGetValue(key, out var keyHandlers))
        {
            foreach (var handler in keyHandlers)
            {
                try
                {
                    handler(args);
                    if (args.Handled)
                        break;
                }
                catch (Exception)
                {
                    // Continue with next handler if one throws
                }
            }
        }
        
        // Raise key pressed event
        KeyPressed?.Invoke(this, args);
        
        // Handle text input
        if (character.HasValue && !args.Handled)
        {
            TextInput?.Invoke(this, character.Value.ToString());
        }
        
        return args.Handled;
    }
    
    /// <summary>
    /// Reads a line of text input with optional prompt.
    /// </summary>
    /// <param name="prompt">Optional prompt text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The input text, or null if cancelled.</returns>
    public async Task<string?> ReadLineAsync(string prompt = "", CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(prompt))
        {
            System.Console.Write(prompt);
        }
        
        var input = new System.Text.StringBuilder();
        var wasCapturing = _isCapturingInput;
        
        try
        {
            StopCapture();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                if (System.Console.KeyAvailable)
                {
                    var keyInfo = System.Console.ReadKey(true);
                    
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        System.Console.WriteLine();
                        return input.ToString();
                    }
                    else if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        System.Console.WriteLine();
                        return null;
                    }
                    else if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (input.Length > 0)
                        {
                            input.Length--;
                            System.Console.Write("\b \b");
                        }
                    }
                    else if (keyInfo.KeyChar >= 32) // Printable character
                    {
                        input.Append(keyInfo.KeyChar);
                        System.Console.Write(keyInfo.KeyChar);
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
            
            return null;
        }
        finally
        {
            if (wasCapturing)
                StartCapture();
        }
    }
    
    /// <summary>
    /// Waits for a single key press.
    /// </summary>
    /// <param name="prompt">Optional prompt text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pressed key, or null if cancelled.</returns>
    public async Task<KeyCode?> ReadKeyAsync(string prompt = "", CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(prompt))
        {
            System.Console.Write(prompt);
        }
        
        var wasCapturing = _isCapturingInput;
        
        try
        {
            StopCapture();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                if (System.Console.KeyAvailable)
                {
                    var keyInfo = System.Console.ReadKey(true);
                    return ConsoleKeyToKeyCode(keyInfo.Key);
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
            
            return null;
        }
        finally
        {
            if (wasCapturing)
                StartCapture();
        }
    }
    
    private async Task ProcessConsoleKey(ConsoleKeyInfo keyInfo)
    {
        var keyCode = ConsoleKeyToKeyCode(keyInfo.Key);
        var modifiers = GetKeyModifiers(keyInfo.Modifiers);
        var character = keyInfo.KeyChar >= 32 ? (char?)keyInfo.KeyChar : null;
        
        await Task.Run(() => ProcessInput(keyCode, modifiers, character));
    }
    
    private static KeyCode ConsoleKeyToKeyCode(ConsoleKey consoleKey)
    {
        return consoleKey switch
        {
            ConsoleKey.A => KeyCode.A,
            ConsoleKey.B => KeyCode.B,
            ConsoleKey.C => KeyCode.C,
            ConsoleKey.D => KeyCode.D,
            ConsoleKey.E => KeyCode.E,
            ConsoleKey.F => KeyCode.F,
            ConsoleKey.G => KeyCode.G,
            ConsoleKey.H => KeyCode.H,
            ConsoleKey.I => KeyCode.I,
            ConsoleKey.J => KeyCode.J,
            ConsoleKey.K => KeyCode.K,
            ConsoleKey.L => KeyCode.L,
            ConsoleKey.M => KeyCode.M,
            ConsoleKey.N => KeyCode.N,
            ConsoleKey.O => KeyCode.O,
            ConsoleKey.P => KeyCode.P,
            ConsoleKey.Q => KeyCode.Q,
            ConsoleKey.R => KeyCode.R,
            ConsoleKey.S => KeyCode.S,
            ConsoleKey.T => KeyCode.T,
            ConsoleKey.U => KeyCode.U,
            ConsoleKey.V => KeyCode.V,
            ConsoleKey.W => KeyCode.W,
            ConsoleKey.X => KeyCode.X,
            ConsoleKey.Y => KeyCode.Y,
            ConsoleKey.Z => KeyCode.Z,
            ConsoleKey.D0 => KeyCode.Alpha0,
            ConsoleKey.D1 => KeyCode.Alpha1,
            ConsoleKey.D2 => KeyCode.Alpha2,
            ConsoleKey.D3 => KeyCode.Alpha3,
            ConsoleKey.D4 => KeyCode.Alpha4,
            ConsoleKey.D5 => KeyCode.Alpha5,
            ConsoleKey.D6 => KeyCode.Alpha6,
            ConsoleKey.D7 => KeyCode.Alpha7,
            ConsoleKey.D8 => KeyCode.Alpha8,
            ConsoleKey.D9 => KeyCode.Alpha9,
            ConsoleKey.F1 => KeyCode.F1,
            ConsoleKey.F2 => KeyCode.F2,
            ConsoleKey.F3 => KeyCode.F3,
            ConsoleKey.F4 => KeyCode.F4,
            ConsoleKey.F5 => KeyCode.F5,
            ConsoleKey.F6 => KeyCode.F6,
            ConsoleKey.F7 => KeyCode.F7,
            ConsoleKey.F8 => KeyCode.F8,
            ConsoleKey.F9 => KeyCode.F9,
            ConsoleKey.F10 => KeyCode.F10,
            ConsoleKey.F11 => KeyCode.F11,
            ConsoleKey.F12 => KeyCode.F12,
            ConsoleKey.UpArrow => KeyCode.UpArrow,
            ConsoleKey.DownArrow => KeyCode.DownArrow,
            ConsoleKey.LeftArrow => KeyCode.LeftArrow,
            ConsoleKey.RightArrow => KeyCode.RightArrow,
            ConsoleKey.Enter => KeyCode.Enter,
            ConsoleKey.Escape => KeyCode.Escape,
            ConsoleKey.Spacebar => KeyCode.Space,
            ConsoleKey.Tab => KeyCode.Tab,
            ConsoleKey.Backspace => KeyCode.Backspace,
            ConsoleKey.Delete => KeyCode.Delete,
            ConsoleKey.Insert => KeyCode.Insert,
            ConsoleKey.Home => KeyCode.Home,
            ConsoleKey.End => KeyCode.End,
            ConsoleKey.PageUp => KeyCode.PageUp,
            ConsoleKey.PageDown => KeyCode.PageDown,
            _ => KeyCode.None
        };
    }
    
    private static KeyModifiers GetKeyModifiers(ConsoleModifiers consoleModifiers)
    {
        var modifiers = KeyModifiers.None;
        
        if (consoleModifiers.HasFlag(ConsoleModifiers.Shift))
            modifiers |= KeyModifiers.Shift;
        if (consoleModifiers.HasFlag(ConsoleModifiers.Control))
            modifiers |= KeyModifiers.Control;
        if (consoleModifiers.HasFlag(ConsoleModifiers.Alt))
            modifiers |= KeyModifiers.Alt;
        
        return modifiers;
    }
}