using GameConsole.Core.Abstractions;
using GameConsole.Input.Core.Events;
using GameConsole.Input.Core.Types;
using GameConsole.Input.Core.Devices;
using GameConsole.Input.Core.Mapping;

namespace GameConsole.Input.Core.Capabilities;

/// <summary>
/// Capability for recording and playing back input sequences.
/// </summary>
public interface IInputRecordingCapability : ICapabilityProvider
{
    /// <summary>
    /// Starts recording input events.
    /// </summary>
    /// <param name="recordingName">Name for the recording session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recording session ID.</returns>
    Task<string> StartRecordingAsync(string recordingName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the current recording session.
    /// </summary>
    /// <param name="recordingId">The recording session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopRecordingAsync(string recordingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays back a recorded input sequence.
    /// </summary>
    /// <param name="recordingId">The recording to play back.</param>
    /// <param name="playbackOptions">Options for playback behavior.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PlaybackAsync(string recordingId, InputPlaybackOptions? playbackOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available recordings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of recording metadata.</returns>
    Task<IEnumerable<InputRecording>> GetRecordingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recording.
    /// </summary>
    /// <param name="recordingId">The recording to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether recording is currently active.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Gets a value indicating whether playback is currently active.
    /// </summary>
    bool IsPlayingBack { get; }
}

/// <summary>
/// Options for input playback behavior.
/// </summary>
public class InputPlaybackOptions
{
    /// <summary>
    /// Gets or sets the playback speed multiplier (1.0 = normal speed).
    /// </summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets a value indicating whether to loop the playback.
    /// </summary>
    public bool Loop { get; set; } = false;

    /// <summary>
    /// Gets or sets the device mappings for playback (original device ID -> target device ID).
    /// </summary>
    public Dictionary<string, string> DeviceMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to suppress actual input events during playback.
    /// </summary>
    public bool SuppressRealInput { get; set; } = false;
}

/// <summary>
/// Represents metadata about an input recording.
/// </summary>
public class InputRecording
{
    /// <summary>
    /// Gets or sets the recording ID.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the recording name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the duration of the recording.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the number of input events in the recording.
    /// </summary>
    public int EventCount { get; set; }

    /// <summary>
    /// Gets or sets the device types involved in the recording.
    /// </summary>
    public HashSet<string> DeviceTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata about the recording.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Capability for configuring input mappings and key bindings.
/// </summary>
public interface IInputMappingCapability : ICapabilityProvider
{
    /// <summary>
    /// Creates a new input mapping profile.
    /// </summary>
    /// <param name="profileName">Name of the mapping profile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile ID.</returns>
    Task<string> CreateMappingProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an input mapping for a specific action.
    /// </summary>
    /// <param name="profileId">The mapping profile ID.</param>
    /// <param name="actionName">The action to map.</param>
    /// <param name="mapping">The input mapping configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetInputMappingAsync(string profileId, string actionName, InputMapping mapping, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current input mapping for an action.
    /// </summary>
    /// <param name="profileId">The mapping profile ID.</param>
    /// <param name="actionName">The action name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The input mapping, or null if not mapped.</returns>
    Task<InputMapping?> GetInputMappingAsync(string profileId, string actionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an input mapping for an action.
    /// </summary>
    /// <param name="profileId">The mapping profile ID.</param>
    /// <param name="actionName">The action name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveInputMappingAsync(string profileId, string actionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mappings in a profile.
    /// </summary>
    /// <param name="profileId">The mapping profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All input mappings in the profile.</returns>
    Task<IReadOnlyDictionary<string, InputMapping>> GetAllMappingsAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a mapping profile.
    /// </summary>
    /// <param name="profileId">The mapping profile ID to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active mapping profile ID.
    /// </summary>
    string? ActiveProfileId { get; }

    /// <summary>
    /// Gets all available mapping profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available mapping profiles.</returns>
    Task<IEnumerable<InputMappingProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an input mapping configuration.
/// </summary>
public class InputMapping
{
    /// <summary>
    /// Gets or sets the primary input for this mapping.
    /// </summary>
    public required InputTrigger Primary { get; set; }

    /// <summary>
    /// Gets or sets the secondary (alternative) input for this mapping.
    /// </summary>
    public InputTrigger? Secondary { get; set; }

    /// <summary>
    /// Gets or sets required modifier keys.
    /// </summary>
    public KeyModifiers RequiredModifiers { get; set; } = KeyModifiers.None;

    /// <summary>
    /// Gets or sets excluded modifier keys.
    /// </summary>
    public KeyModifiers ExcludedModifiers { get; set; } = KeyModifiers.None;

    /// <summary>
    /// Gets or sets the mapping sensitivity (for analog inputs).
    /// </summary>
    public float Sensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the dead zone for analog inputs.
    /// </summary>
    public float DeadZone { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets a value indicating whether this mapping is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Represents an input mapping profile.
/// </summary>
public class InputMappingProfile
{
    /// <summary>
    /// Gets or sets the profile ID.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the profile name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    public DateTimeOffset ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default profile.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the profile.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}