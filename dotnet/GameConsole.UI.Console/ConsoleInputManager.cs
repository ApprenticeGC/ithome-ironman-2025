using GameConsole.Input.Core;
using GameConsole.Input.Services;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Console;

/// <summary>
/// Implementation of console input manager for UI interactions.
/// </summary>
public class ConsoleInputManager : IConsoleInputManager
{
    private readonly GameConsole.Input.Services.IService _inputService;
    private readonly ILogger<ConsoleInputManager> _logger;
    private bool _inputCaptureEnabled = false;
    private readonly SemaphoreSlim _keyWaitSemaphore = new(0, 1);
    private UIKeyEventArgs? _lastKeyEvent;
    
    public event EventHandler<UIKeyEventArgs>? KeyPressed;
    
    public bool IsInputCaptureEnabled => _inputCaptureEnabled;
    
    public ConsoleInputManager(GameConsole.Input.Services.IService inputService, ILogger<ConsoleInputManager> logger)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _inputService.KeyEvent += OnKeyEvent;
    }
    
    public void SetInputCapture(bool enabled)
    {
        if (_inputCaptureEnabled != enabled)
        {
            _inputCaptureEnabled = enabled;
            _logger.LogDebug("Input capture {Status}", enabled ? "enabled" : "disabled");
        }
    }
    
    public async Task<UIKeyEventArgs> WaitForKeyAsync(CancellationToken cancellationToken = default)
    {
        await _keyWaitSemaphore.WaitAsync(cancellationToken);
        return _lastKeyEvent ?? throw new InvalidOperationException("No key event available");
    }
    
    public async Task<bool> IsKeyPressedAsync(KeyCode key, CancellationToken cancellationToken = default)
    {
        return await _inputService.IsKeyPressedAsync(key, cancellationToken);
    }
    
    private void OnKeyEvent(object? sender, KeyEvent keyEvent)
    {
        if (!_inputCaptureEnabled) return;
        
        var uiKeyEvent = ConvertToUIKeyEvent(keyEvent);
        _lastKeyEvent = uiKeyEvent;
        
        KeyPressed?.Invoke(this, uiKeyEvent);
        
        if (_keyWaitSemaphore.CurrentCount == 0)
        {
            _keyWaitSemaphore.Release();
        }
    }
    
    private static UIKeyEventArgs ConvertToUIKeyEvent(KeyEvent keyEvent)
    {
        // Convert system key event to UI key event
        var character = GetCharacterFromKey(keyEvent.Key, keyEvent.Modifiers.HasFlag(KeyModifiers.Shift));
        
        return new UIKeyEventArgs(
            keyEvent.Key,
            keyEvent.Modifiers.HasFlag(KeyModifiers.Control),
            keyEvent.Modifiers.HasFlag(KeyModifiers.Alt),
            keyEvent.Modifiers.HasFlag(KeyModifiers.Shift),
            character
        );
    }
    
    private static char? GetCharacterFromKey(KeyCode key, bool shift)
    {
        // Simple character mapping - could be expanded
        return key switch
        {
            KeyCode.A => shift ? 'A' : 'a',
            KeyCode.B => shift ? 'B' : 'b',
            KeyCode.C => shift ? 'C' : 'c',
            KeyCode.D => shift ? 'D' : 'd',
            KeyCode.E => shift ? 'E' : 'e',
            KeyCode.F => shift ? 'F' : 'f',
            KeyCode.G => shift ? 'G' : 'g',
            KeyCode.H => shift ? 'H' : 'h',
            KeyCode.I => shift ? 'I' : 'i',
            KeyCode.J => shift ? 'J' : 'j',
            KeyCode.K => shift ? 'K' : 'k',
            KeyCode.L => shift ? 'L' : 'l',
            KeyCode.M => shift ? 'M' : 'm',
            KeyCode.N => shift ? 'N' : 'n',
            KeyCode.O => shift ? 'O' : 'o',
            KeyCode.P => shift ? 'P' : 'p',
            KeyCode.Q => shift ? 'Q' : 'q',
            KeyCode.R => shift ? 'R' : 'r',
            KeyCode.S => shift ? 'S' : 's',
            KeyCode.T => shift ? 'T' : 't',
            KeyCode.U => shift ? 'U' : 'u',
            KeyCode.V => shift ? 'V' : 'v',
            KeyCode.W => shift ? 'W' : 'w',
            KeyCode.X => shift ? 'X' : 'x',
            KeyCode.Y => shift ? 'Y' : 'y',
            KeyCode.Z => shift ? 'Z' : 'z',
            KeyCode.Alpha0 => shift ? ')' : '0',
            KeyCode.Alpha1 => shift ? '!' : '1',
            KeyCode.Alpha2 => shift ? '@' : '2',
            KeyCode.Alpha3 => shift ? '#' : '3',
            KeyCode.Alpha4 => shift ? '$' : '4',
            KeyCode.Alpha5 => shift ? '%' : '5',
            KeyCode.Alpha6 => shift ? '^' : '6',
            KeyCode.Alpha7 => shift ? '&' : '7',
            KeyCode.Alpha8 => shift ? '*' : '8',
            KeyCode.Alpha9 => shift ? '(' : '9',
            KeyCode.Space => ' ',
            KeyCode.Period => shift ? '>' : '.',
            KeyCode.Comma => shift ? '<' : ',',
            KeyCode.Semicolon => shift ? ':' : ';',
            KeyCode.Quote => shift ? '"' : '\'',
            KeyCode.Minus => shift ? '_' : '-',
            KeyCode.Equals => shift ? '+' : '=',
            _ => null
        };
    }
}