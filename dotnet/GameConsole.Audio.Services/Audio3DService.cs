using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Numerics;

namespace GameConsole.Audio.Services.Implementation;

/// <summary>
/// Service for spatial (3D) audio positioning and management.
/// Provides 3D audio capabilities with position-based sound placement.
/// </summary>
[Service("Audio3D", Categories = new[] { "Audio" })]
public sealed class Audio3DService : BaseAudioService, ISpatialAudioCapability
{
    private readonly ConcurrentDictionary<string, Audio3DSource> _sources = new();
    private AudioListener _listener = new AudioListener();
    private float _speedOfSound = 343.0f; // meters per second
    private float _dopplerFactor = 1.0f;

    public Audio3DService(ILogger<Audio3DService>? logger = null) 
        : base(logger)
    {
    }

    /// <summary>
    /// Gets all available capabilities provided by this service.
    /// </summary>
    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Task.CompletedTask;
        return new[] { typeof(ISpatialAudioCapability) };
    }

    /// <summary>
    /// Checks if the service provides a specific capability.
    /// </summary>
    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await Task.CompletedTask;
        return typeof(T) == typeof(ISpatialAudioCapability);
    }

    /// <summary>
    /// Gets a specific capability instance from the service.
    /// </summary>
    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();
        await Task.CompletedTask;
        return typeof(T) == typeof(ISpatialAudioCapability) ? this as T : null;
    }

    /// <summary>
    /// Sets the position of the audio listener (typically the player/camera).
    /// </summary>
    /// <param name="position">3D position of the listener.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SetListenerPositionAsync(Vector3 position, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        _listener.Position = position;
        
        // Update all audio sources with new listener position
        await UpdateAllSourcesAsync();
        
        Logger?.LogDebug("Set listener position to {Position}", position);
    }

    /// <summary>
    /// Sets the orientation of the audio listener.
    /// </summary>
    /// <param name="forward">Forward direction vector.</param>
    /// <param name="up">Up direction vector.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SetListenerOrientationAsync(Vector3 forward, Vector3 up, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        _listener.Forward = Vector3.Normalize(forward);
        _listener.Up = Vector3.Normalize(up);
        
        // Update all audio sources with new listener orientation
        await UpdateAllSourcesAsync();
        
        Logger?.LogDebug("Set listener orientation - Forward: {Forward}, Up: {Up}", forward, up);
    }

    /// <summary>
    /// Plays audio with 3D positioning.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="position">3D position of the audio source.</param>
    /// <param name="volume">Base volume level.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task Play3DAudioAsync(string path, Vector3 position, float volume, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var sourceId = $"{path}_{position.GetHashCode()}_{DateTime.UtcNow.Ticks}";
        
        var source = new Audio3DSource(sourceId, path, position, volume);
        _sources[sourceId] = source;
        
        await Update3DAudioAsync(source);
        
        Logger?.LogDebug("Started 3D audio playback: {Path} at position {Position}", path, position);
    }

    /// <summary>
    /// Creates a 3D audio source that can be controlled over time.
    /// </summary>
    /// <param name="sourceId">Unique identifier for the source.</param>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="position">Initial 3D position.</param>
    /// <param name="volume">Base volume level.</param>
    /// <returns>True if source was created successfully.</returns>
    public async Task<bool> Create3DSourceAsync(string sourceId, string path, Vector3 position, float volume = 1.0f)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(sourceId))
            throw new ArgumentException("Source ID cannot be null or empty", nameof(sourceId));

        if (_sources.ContainsKey(sourceId))
        {
            Logger?.LogWarning("3D audio source {SourceId} already exists", sourceId);
            return false;
        }

        var source = new Audio3DSource(sourceId, path, position, volume);
        _sources[sourceId] = source;
        
        await Update3DAudioAsync(source);
        
        Logger?.LogDebug("Created 3D audio source {SourceId} at position {Position}", sourceId, position);
        return true;
    }

    /// <summary>
    /// Updates the position of a 3D audio source.
    /// </summary>
    /// <param name="sourceId">Source identifier.</param>
    /// <param name="position">New 3D position.</param>
    /// <param name="velocity">Optional velocity for Doppler effect.</param>
    public async Task UpdateSourcePositionAsync(string sourceId, Vector3 position, Vector3? velocity = null)
    {
        ThrowIfDisposed();
        
        if (_sources.TryGetValue(sourceId, out var source))
        {
            source.Position = position;
            source.Velocity = velocity ?? Vector3.Zero;
            
            await Update3DAudioAsync(source);
            
            Logger?.LogTrace("Updated 3D source {SourceId} position to {Position}", sourceId, position);
        }
        else
        {
            Logger?.LogWarning("3D audio source {SourceId} not found", sourceId);
        }
    }

    /// <summary>
    /// Removes a 3D audio source.
    /// </summary>
    /// <param name="sourceId">Source identifier.</param>
    public async Task Remove3DSourceAsync(string sourceId)
    {
        ThrowIfDisposed();
        
        if (_sources.TryRemove(sourceId, out var source))
        {
            Logger?.LogDebug("Removed 3D audio source {SourceId}", sourceId);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets information about all 3D audio sources.
    /// </summary>
    /// <returns>Collection of source information.</returns>
    public async Task<IEnumerable<Audio3DSourceInfo>> GetSourcesAsync()
    {
        ThrowIfDisposed();
        
        await Task.CompletedTask;
        return _sources.Values.Select(s => new Audio3DSourceInfo(
            s.SourceId,
            s.AudioPath,
            s.Position,
            s.Velocity,
            s.BaseVolume,
            s.EffectiveVolume,
            s.Distance,
            s.AttenuationFactor
        ));
    }

    /// <summary>
    /// Sets the speed of sound for distance calculations.
    /// </summary>
    /// <param name="speedOfSound">Speed of sound in meters per second.</param>
    public async Task SetSpeedOfSoundAsync(float speedOfSound)
    {
        ThrowIfDisposed();
        
        if (speedOfSound <= 0)
            throw new ArgumentOutOfRangeException(nameof(speedOfSound), "Speed of sound must be positive");

        _speedOfSound = speedOfSound;
        
        Logger?.LogDebug("Set speed of sound to {SpeedOfSound} m/s", speedOfSound);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sets the Doppler effect factor.
    /// </summary>
    /// <param name="dopplerFactor">Doppler factor (1.0 = realistic, 0.0 = disabled).</param>
    public async Task SetDopplerFactorAsync(float dopplerFactor)
    {
        ThrowIfDisposed();
        
        if (dopplerFactor < 0)
            throw new ArgumentOutOfRangeException(nameof(dopplerFactor), "Doppler factor cannot be negative");

        _dopplerFactor = dopplerFactor;
        
        Logger?.LogDebug("Set Doppler factor to {DopplerFactor}", dopplerFactor);
        await Task.CompletedTask;
    }

    private async Task UpdateAllSourcesAsync()
    {
        var tasks = _sources.Values.Select(Update3DAudioAsync);
        await Task.WhenAll(tasks);
    }

    private async Task Update3DAudioAsync(Audio3DSource source)
    {
        var distance = Vector3.Distance(_listener.Position, source.Position);
        source.Distance = distance;
        
        // Calculate distance-based attenuation
        var attenuationFactor = CalculateDistanceAttenuation(distance);
        source.AttenuationFactor = attenuationFactor;
        
        // Calculate effective volume
        source.EffectiveVolume = source.BaseVolume * attenuationFactor;
        
        // Calculate panning based on listener orientation
        var directionToSource = Vector3.Normalize(source.Position - _listener.Position);
        var panningFactor = CalculatePanning(directionToSource);
        source.PanningFactor = panningFactor;
        
        // Calculate Doppler effect if velocity is available
        if (_dopplerFactor > 0 && source.Velocity != Vector3.Zero)
        {
            var dopplerShift = CalculateDopplerShift(source);
            source.PitchFactor = dopplerShift;
        }
        
        await Task.CompletedTask;
    }

    private float CalculateDistanceAttenuation(float distance)
    {
        // Simple inverse square law with minimum distance
        const float minDistance = 1.0f;
        const float maxDistance = 100.0f;
        
        if (distance <= minDistance) return 1.0f;
        if (distance >= maxDistance) return 0.0f;
        
        return minDistance / (distance * distance);
    }

    private float CalculatePanning(Vector3 directionToSource)
    {
        // Calculate panning based on right vector
        var right = Vector3.Cross(_listener.Forward, _listener.Up);
        var panValue = Vector3.Dot(directionToSource, right);
        
        // Clamp to [-1, 1] range where -1 = left, 1 = right
        return Math.Clamp(panValue, -1.0f, 1.0f);
    }

    private float CalculateDopplerShift(Audio3DSource source)
    {
        var listenerToSource = source.Position - _listener.Position;
        var distance = listenerToSource.Length();
        
        if (distance < 0.01f) return 1.0f; // Avoid division by zero
        
        var directionToSource = listenerToSource / distance;
        var relativeVelocity = Vector3.Dot(source.Velocity, directionToSource);
        
        // Doppler shift calculation: f' = f * (v + vr) / (v + vs)
        // where v = speed of sound, vr = receiver velocity, vs = source velocity
        var dopplerShift = _speedOfSound / (_speedOfSound - relativeVelocity * _dopplerFactor);
        
        // Clamp to reasonable range
        return Math.Clamp(dopplerShift, 0.5f, 2.0f);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _sources.Clear();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Represents the audio listener (typically player/camera).
    /// </summary>
    private sealed class AudioListener
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Forward { get; set; } = Vector3.UnitZ;
        public Vector3 Up { get; set; } = Vector3.UnitY;
        public Vector3 Velocity { get; set; } = Vector3.Zero;
    }

    /// <summary>
    /// Represents a 3D audio source.
    /// </summary>
    private sealed class Audio3DSource
    {
        public string SourceId { get; }
        public string AudioPath { get; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public float BaseVolume { get; set; }
        public float EffectiveVolume { get; set; }
        public float Distance { get; set; }
        public float AttenuationFactor { get; set; }
        public float PanningFactor { get; set; }
        public float PitchFactor { get; set; } = 1.0f;

        public Audio3DSource(string sourceId, string audioPath, Vector3 position, float baseVolume)
        {
            SourceId = sourceId;
            AudioPath = audioPath;
            Position = position;
            BaseVolume = baseVolume;
            EffectiveVolume = baseVolume;
            Velocity = Vector3.Zero;
        }
    }
}

/// <summary>
/// Information about a 3D audio source.
/// </summary>
public sealed record Audio3DSourceInfo(
    string SourceId,
    string AudioPath,
    Vector3 Position,
    Vector3 Velocity,
    float BaseVolume,
    float EffectiveVolume,
    float Distance,
    float AttenuationFactor
);