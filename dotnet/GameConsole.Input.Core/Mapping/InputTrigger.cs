using GameConsole.Input.Core.Types;
using GameConsole.Input.Core.Devices;

namespace GameConsole.Input.Core.Mapping;

/// <summary>
/// Represents an input trigger that can activate an action.
/// </summary>
public class InputTrigger
{
    /// <summary>
    /// Gets or sets the device type for this trigger.
    /// </summary>
    public required InputDeviceType DeviceType { get; set; }

    /// <summary>
    /// Gets or sets the specific device ID (null for any device of the type).
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the keyboard key (for keyboard triggers).
    /// </summary>
    public KeyCode? KeyCode { get; set; }

    /// <summary>
    /// Gets or sets the mouse button (for mouse triggers).
    /// </summary>
    public MouseButton? MouseButton { get; set; }

    /// <summary>
    /// Gets or sets the gamepad button (for gamepad triggers).
    /// </summary>
    public GamepadButton? GamepadButton { get; set; }

    /// <summary>
    /// Gets or sets the gamepad axis (for analog gamepad triggers).
    /// </summary>
    public GamepadAxis? GamepadAxis { get; set; }

    /// <summary>
    /// Gets or sets the trigger type.
    /// </summary>
    public InputTriggerType TriggerType { get; set; } = InputTriggerType.Press;
}

/// <summary>
/// Represents the type of input trigger.
/// </summary>
public enum InputTriggerType
{
    /// <summary>Triggered on press.</summary>
    Press,
    /// <summary>Triggered on release.</summary>
    Release,
    /// <summary>Triggered while held.</summary>
    Hold,
    /// <summary>Triggered on analog value change.</summary>
    Analog
}