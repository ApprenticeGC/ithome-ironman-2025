using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Numerics;

namespace GameConsole.Audio.Services;

/// <summary>
/// 3D spatial audio service for positioned audio sources.
/// Provides realistic 3D audio positioning with distance-based attenuation.
/// </summary>
[Service("Audio 3D Service", "1.0.0", "3D spatial audio positioning with distance-based attenuation", 
         Categories = new[] { "Audio", "3D", "Spatial" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class Audio3DService : BaseAudioService, ISpatialAudioCapability
{
    private readonly ConcurrentDictionary<string, Audio3DSource> _audioSources = new();
    
    private Vector3 _listenerPosition = Vector3.Zero;
    private Vector3 _listenerForward = Vector3.UnitZ;
    private Vector3 _listenerUp = Vector3.UnitY;
    
    // 3D audio configuration
    private const float DefaultMinDistance = 1.0f;
    private const float DefaultMaxDistance = 100.0f;
    private const float SpeedOfSound = 343.0f; // meters per second

    public Audio3DService(ILogger<Audio3DService> logger) : base(logger)
    {
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing 3D audio system");
        _logger.LogDebug("Listener initialized at position {Position} facing {Forward}", 
            _listenerPosition, _listenerForward);
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping 3D audio system");
        return StopAllAsync(cancellationToken);
    }

    protected override ValueTask OnDisposeAsync()
    {
        _logger.LogDebug("Disposing 3D audio service");
        _audioSources.Clear();
        return ValueTask.CompletedTask;
    }

    #region ISpatialAudioCapability Implementation

    public Task SetListenerTransformAsync(Vector3 position, Vector3 forward, Vector3 up, CancellationToken cancellationToken = default)
    {
        _listenerPosition = position;
        _listenerForward = Vector3.Normalize(forward);
        _listenerUp = Vector3.Normalize(up);
        
        _logger.LogDebug("Updated listener transform: Position={Position}, Forward={Forward}, Up={Up}", 
            position, _listenerForward, _listenerUp);
        
        // Update all active 3D audio sources
        return UpdateAll3DAudioAsync(cancellationToken);
    }

    public async Task Play3DAudioAsync(string path, Vector3 position, float volume = 1.0f, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audio file path cannot be null or empty", nameof(path));
        
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        if (!IsRunning)
        {
            _logger.LogWarning("Cannot play 3D audio - service is not running");
            return;
        }

        try
        {
            // Create or update 3D audio source
            var audioSource = new Audio3DSource(
                FilePath: path,
                Position: position,
                Velocity: Vector3.Zero,
                Volume: volume,
                MinDistance: DefaultMinDistance,
                MaxDistance: DefaultMaxDistance,
                IsPlaying: true
            );

            _audioSources[path] = audioSource;

            // Calculate 3D audio parameters
            var (spatialVolume, panLeft, panRight) = Calculate3DAudioParameters(audioSource);

            _logger.LogDebug("Playing 3D audio '{Path}' at position {Position} with spatial volume {Volume}, pan L:{PanLeft} R:{PanRight}", 
                path, position, spatialVolume, panLeft, panRight);

            // Register with base service using calculated spatial volume
            var playbackInfo = new AudioPlaybackInfo(
                FilePath: path,
                Category: "3D",
                Format: AudioFormat.Auto,
                Duration: TimeSpan.Zero,
                Position: TimeSpan.Zero,
                IsPlaying: true,
                IsPaused: false,
                IsLooping: false,
                Volume: spatialVolume
            );

            RegisterActiveAudio(path, playbackInfo);

            // In a real implementation, this would integrate with the audio playback system
            // to apply 3D positioning, volume, and panning effects
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play 3D audio '{Path}' at position {Position}", path, position);
            throw;
        }
    }

    #endregion

    #region 3D Audio Management

    /// <summary>
    /// Updates the position of a 3D audio source.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="position">New position of the audio source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task UpdateAudioSourcePositionAsync(string path, Vector3 position, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audio file path cannot be null or empty", nameof(path));

        if (_audioSources.TryGetValue(path, out var audioSource))
        {
            var updatedSource = audioSource with { Position = position };
            _audioSources[path] = updatedSource;
            
            // Recalculate 3D parameters
            var (spatialVolume, panLeft, panRight) = Calculate3DAudioParameters(updatedSource);
            
            _logger.LogDebug("Updated 3D audio source '{Path}' position to {Position}, new spatial volume: {Volume}", 
                path, position, spatialVolume);

            // In a real implementation, this would update the actual audio parameters
        }
        else
        {
            _logger.LogWarning("Cannot update position for 3D audio source '{Path}' - source not found", path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the velocity of a 3D audio source for Doppler effect calculation.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="velocity">Velocity vector of the audio source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task UpdateAudioSourceVelocityAsync(string path, Vector3 velocity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audio file path cannot be null or empty", nameof(path));

        if (_audioSources.TryGetValue(path, out var audioSource))
        {
            var updatedSource = audioSource with { Velocity = velocity };
            _audioSources[path] = updatedSource;
            
            _logger.LogDebug("Updated 3D audio source '{Path}' velocity to {Velocity}", path, velocity);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets information about a 3D audio source.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <returns>3D audio source information, or null if not found.</returns>
    public Audio3DSource? GetAudioSource(string path)
    {
        return _audioSources.TryGetValue(path, out var audioSource) ? audioSource : null;
    }

    /// <summary>
    /// Gets all active 3D audio sources.
    /// </summary>
    /// <returns>Collection of all 3D audio sources.</returns>
    public IEnumerable<Audio3DSource> GetAllAudioSources()
    {
        return _audioSources.Values;
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new Type[] { typeof(ISpatialAudioCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(ISpatialAudioCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(ISpatialAudioCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Base Audio Service Overrides

    public override async Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
    {
        // For 3D audio, redirect to positioned audio at listener position
        await Play3DAudioAsync(path, _listenerPosition, 1.0f, cancellationToken);
        return true;
    }

    public override async Task StopAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audio file path cannot be null or empty", nameof(path));

        _audioSources.TryRemove(path, out _);
        UnregisterActiveAudio(path);
        
        _logger.LogDebug("Stopped 3D audio source '{Path}'", path);
    }

    #endregion

    #region Private Helper Methods

    private (float volume, float panLeft, float panRight) Calculate3DAudioParameters(Audio3DSource audioSource)
    {
        // Calculate distance from listener to audio source
        var sourceToListener = _listenerPosition - audioSource.Position;
        var distance = sourceToListener.Length();

        // Calculate distance-based volume attenuation
        float distanceAttenuation = CalculateDistanceAttenuation(distance, audioSource.MinDistance, audioSource.MaxDistance);

        // Calculate panning based on left-right positioning
        var rightVector = Vector3.Normalize(Vector3.Cross(_listenerForward, _listenerUp));
        var relativePosition = Vector3.Normalize(sourceToListener);
        var panValue = Vector3.Dot(relativePosition, rightVector);

        // Convert pan value to left/right channel volumes
        float panLeft = Math.Max(0.0f, 1.0f - panValue) * 0.5f + 0.5f;
        float panRight = Math.Max(0.0f, 1.0f + panValue) * 0.5f + 0.5f;

        // Apply distance attenuation to final volume
        float finalVolume = audioSource.Volume * distanceAttenuation * GetMasterVolume();

        return (finalVolume, panLeft, panRight);
    }

    private float CalculateDistanceAttenuation(float distance, float minDistance, float maxDistance)
    {
        if (distance <= minDistance)
            return 1.0f;
        
        if (distance >= maxDistance)
            return 0.0f;

        // Linear attenuation between min and max distance
        return 1.0f - (distance - minDistance) / (maxDistance - minDistance);
    }

    private float CalculateDopplerEffect(Audio3DSource audioSource)
    {
        // Calculate Doppler shift based on velocity
        var sourceToListener = _listenerPosition - audioSource.Position;
        var direction = Vector3.Normalize(sourceToListener);
        
        // Project velocity onto direction vector
        var velocityTowardsListener = Vector3.Dot(audioSource.Velocity, direction);
        
        // Calculate Doppler frequency shift
        // f' = f * (v + vr) / (v + vs)
        // Where v = speed of sound, vr = listener velocity (assumed 0), vs = source velocity
        var dopplerRatio = SpeedOfSound / (SpeedOfSound - velocityTowardsListener);
        
        return Math.Max(0.1f, Math.Min(2.0f, dopplerRatio)); // Clamp to reasonable range
    }

    private async Task UpdateAll3DAudioAsync(CancellationToken cancellationToken)
    {
        foreach (var audioSource in _audioSources.Values)
        {
            var (spatialVolume, panLeft, panRight) = Calculate3DAudioParameters(audioSource);
            
            // In a real implementation, this would update the actual audio parameters
            _logger.LogTrace("Updated 3D parameters for '{Path}': Volume={Volume}, PanL={PanLeft}, PanR={PanRight}",
                audioSource.FilePath, spatialVolume, panLeft, panRight);
        }
    }

    #endregion
}