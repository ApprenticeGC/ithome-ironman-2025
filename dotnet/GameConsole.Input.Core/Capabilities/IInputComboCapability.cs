using GameConsole.Core.Abstractions;
using GameConsole.Input.Core.Events;
using GameConsole.Input.Core.Types;
using GameConsole.Input.Core.Devices;
using GameConsole.Input.Core.Mapping;

namespace GameConsole.Input.Core.Capabilities;

/// <summary>
/// Capability for detecting input combinations and sequences.
/// </summary>
public interface IInputComboCapability : ICapabilityProvider
{
    /// <summary>
    /// Registers a new input combo.
    /// </summary>
    /// <param name="comboName">The name of the combo.</param>
    /// <param name="comboDefinition">The combo definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RegisterComboAsync(string comboName, InputComboDefinition comboDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an input combo.
    /// </summary>
    /// <param name="comboName">The name of the combo to unregister.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UnregisterComboAsync(string comboName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered combos.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All registered input combos.</returns>
    Task<IReadOnlyDictionary<string, InputComboDefinition>> GetCombosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Observable for combo activation events.
    /// </summary>
    IObservable<InputComboEvent> ComboActivated { get; }

    /// <summary>
    /// Manually triggers a combo (useful for testing).
    /// </summary>
    /// <param name="comboName">The combo to trigger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task TriggerComboAsync(string comboName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a combo activation event.
/// </summary>
public class InputComboEvent : InputEventBase
{
    /// <summary>
    /// Initializes a new instance of the InputComboEvent class.
    /// </summary>
    /// <param name="comboName">The name of the activated combo.</param>
    /// <param name="matchedInputs">The inputs that matched the combo.</param>
    /// <param name="timestamp">The timestamp when the combo was activated.</param>
    public InputComboEvent(string comboName, IReadOnlyList<IInputEvent> matchedInputs, DateTimeOffset? timestamp = null)
        : base("combo-detector", timestamp)
    {
        ComboName = comboName ?? throw new ArgumentNullException(nameof(comboName));
        MatchedInputs = matchedInputs ?? throw new ArgumentNullException(nameof(matchedInputs));
    }

    /// <summary>
    /// Gets the name of the activated combo.
    /// </summary>
    public string ComboName { get; }

    /// <summary>
    /// Gets the inputs that matched the combo sequence.
    /// </summary>
    public IReadOnlyList<IInputEvent> MatchedInputs { get; }
}

/// <summary>
/// Defines an input combo pattern.
/// </summary>
public class InputComboDefinition
{
    /// <summary>
    /// Gets or sets the sequence of inputs required for this combo.
    /// </summary>
    public required IReadOnlyList<InputComboStep> Steps { get; set; }

    /// <summary>
    /// Gets or sets the maximum time allowed between inputs in the sequence.
    /// </summary>
    public TimeSpan MaxTimeBetweenInputs { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the maximum total time for the entire combo sequence.
    /// </summary>
    public TimeSpan MaxTotalTime { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets a value indicating whether the combo should reset if an unexpected input occurs.
    /// </summary>
    public bool ResetOnUnexpectedInput { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this combo is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of this combo (higher values take precedence).
    /// </summary>
    public int Priority { get; set; } = 0;
}

/// <summary>
/// Represents a single step in an input combo sequence.
/// </summary>
public class InputComboStep
{
    /// <summary>
    /// Gets or sets the required input trigger for this step.
    /// </summary>
    public required InputTrigger Trigger { get; set; }

    /// <summary>
    /// Gets or sets required modifier keys for this step.
    /// </summary>
    public KeyModifiers RequiredModifiers { get; set; } = KeyModifiers.None;

    /// <summary>
    /// Gets or sets excluded modifier keys for this step.
    /// </summary>
    public KeyModifiers ExcludedModifiers { get; set; } = KeyModifiers.None;

    /// <summary>
    /// Gets or sets the minimum duration this input must be held (for hold-type steps).
    /// </summary>
    public TimeSpan? MinHoldDuration { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration this input can be held (for precise timing).
    /// </summary>
    public TimeSpan? MaxHoldDuration { get; set; }

    /// <summary>
    /// Gets or sets whether this step can be satisfied by any of multiple inputs.
    /// </summary>
    public IReadOnlyList<InputTrigger>? AlternativeTriggers { get; set; }
}

/// <summary>
/// Capability for measuring input latency and performance metrics.
/// </summary>
public interface IInputLatencyCapability : ICapabilityProvider
{
    /// <summary>
    /// Starts latency measurement for a specific device.
    /// </summary>
    /// <param name="deviceId">The device to measure latency for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StartLatencyMeasurementAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops latency measurement for a specific device.
    /// </summary>
    /// <param name="deviceId">The device to stop measuring latency for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopLatencyMeasurementAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current latency metrics for a device.
    /// </summary>
    /// <param name="deviceId">The device to get metrics for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current latency metrics.</returns>
    Task<InputLatencyMetrics?> GetLatencyMetricsAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latency metrics for all monitored devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latency metrics for all devices.</returns>
    Task<IReadOnlyDictionary<string, InputLatencyMetrics>> GetAllLatencyMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets latency statistics for a device.
    /// </summary>
    /// <param name="deviceId">The device to reset statistics for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResetLatencyStatisticsAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Observable for real-time latency updates.
    /// </summary>
    IObservable<InputLatencyUpdate> LatencyUpdates { get; }
}

/// <summary>
/// Contains latency metrics for an input device.
/// </summary>
public class InputLatencyMetrics
{
    /// <summary>
    /// Gets or sets the device ID these metrics apply to.
    /// </summary>
    public required string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the average input latency.
    /// </summary>
    public TimeSpan AverageLatency { get; set; }

    /// <summary>
    /// Gets or sets the minimum recorded latency.
    /// </summary>
    public TimeSpan MinLatency { get; set; }

    /// <summary>
    /// Gets or sets the maximum recorded latency.
    /// </summary>
    public TimeSpan MaxLatency { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile latency.
    /// </summary>
    public TimeSpan P95Latency { get; set; }

    /// <summary>
    /// Gets or sets the 99th percentile latency.
    /// </summary>
    public TimeSpan P99Latency { get; set; }

    /// <summary>
    /// Gets or sets the total number of input events measured.
    /// </summary>
    public long TotalEvents { get; set; }

    /// <summary>
    /// Gets or sets the measurement period start time.
    /// </summary>
    public DateTimeOffset MeasurementStartTime { get; set; }

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset LastUpdateTime { get; set; }

    /// <summary>
    /// Gets or sets the input rate (events per second).
    /// </summary>
    public double InputRate { get; set; }
}

/// <summary>
/// Represents a real-time latency update.
/// </summary>
public class InputLatencyUpdate
{
    /// <summary>
    /// Gets or sets the device ID.
    /// </summary>
    public required string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the measured latency for this update.
    /// </summary>
    public TimeSpan Latency { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of this update.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the event that triggered this latency measurement.
    /// </summary>
    public IInputEvent? TriggerEvent { get; set; }
}

/// <summary>
/// Capability for accessibility features and input assistance.
/// </summary>
public interface IAccessibilityCapability : ICapabilityProvider
{
    /// <summary>
    /// Enables sticky keys functionality.
    /// </summary>
    /// <param name="enabled">Whether to enable sticky keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetStickyKeysAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables filter keys functionality to ignore brief or repeated keystrokes.
    /// </summary>
    /// <param name="enabled">Whether to enable filter keys.</param>
    /// <param name="filterDuration">Minimum duration for key presses to be recognized.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetFilterKeysAsync(bool enabled, TimeSpan? filterDuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables toggle keys functionality for lock keys (Caps Lock, Num Lock, etc.).
    /// </summary>
    /// <param name="enabled">Whether to enable toggle keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetToggleKeysAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures mouse keys functionality to use numeric keypad as mouse.
    /// </summary>
    /// <param name="enabled">Whether to enable mouse keys.</param>
    /// <param name="sensitivity">Mouse movement sensitivity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetMouseKeysAsync(bool enabled, float sensitivity = 1.0f, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the repeat rate for key repeat functionality.
    /// </summary>
    /// <param name="initialDelay">Delay before first repeat.</param>
    /// <param name="repeatRate">Rate of subsequent repeats.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetKeyRepeatAsync(TimeSpan initialDelay, TimeSpan repeatRate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current accessibility settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current accessibility configuration.</returns>
    Task<AccessibilitySettings> GetAccessibilitySettingsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains accessibility settings configuration.
/// </summary>
public class AccessibilitySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether sticky keys is enabled.
    /// </summary>
    public bool StickyKeysEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether filter keys is enabled.
    /// </summary>
    public bool FilterKeysEnabled { get; set; }

    /// <summary>
    /// Gets or sets the filter keys duration.
    /// </summary>
    public TimeSpan FilterKeysDuration { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets a value indicating whether toggle keys is enabled.
    /// </summary>
    public bool ToggleKeysEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether mouse keys is enabled.
    /// </summary>
    public bool MouseKeysEnabled { get; set; }

    /// <summary>
    /// Gets or sets the mouse keys sensitivity.
    /// </summary>
    public float MouseKeysSensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the key repeat initial delay.
    /// </summary>
    public TimeSpan KeyRepeatInitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the key repeat rate.
    /// </summary>
    public TimeSpan KeyRepeatRate { get; set; } = TimeSpan.FromMilliseconds(30);
}