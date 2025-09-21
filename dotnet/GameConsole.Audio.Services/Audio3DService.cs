using GameConsole.Core.Abstractions;
using GameConsole.Audio.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Audio.Services;

/// <summary>
/// 3D spatial audio service for position-based audio positioning.
/// </summary>
[Service("Audio3DService", "1.0.0", "3D spatial audio positioning and environmental audio service")]
public class Audio3DService : BaseAudioService, ISpatialAudioCapability
{
    private readonly Dictionary<string, Audio3DInstance> _spatialAudioInstances;
    private Vector3 _listenerPosition = Vector3.Zero;
    private Vector3 _listenerForward = new(0, 0, 1);
    private Vector3 _listenerUp = new(0, 1, 0);
    private float _speedOfSound = 343.0f; // meters per second
    private float _dopplerFactor = 1.0f;

    public Audio3DService(ILogger<Audio3DService> logger) : base(logger)
    {
        _spatialAudioInstances = new Dictionary<string, Audio3DInstance>();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initializing 3D audio system");
        await base.OnInitializeAsync(cancellationToken);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        // Clean up all spatial audio instances
        foreach (var instance in _spatialAudioInstances.Values)
        {
            instance.Dispose();
        }
        _spatialAudioInstances.Clear();
        
        await base.OnDisposeAsync();
    }

    public override async Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
    {
        // Play 3D audio at the listener's position by default
        await Play3DAudioAsync(path, _listenerPosition, GetEffectiveVolume(category), cancellationToken);
        return true;
    }

    public override Task StopAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_spatialAudioInstances.TryGetValue(path, out var instance))
        {
            instance.Stop();
            instance.Dispose();
            _spatialAudioInstances.Remove(path);
            
            _logger.LogDebug("Stopped 3D audio: {Path}", path);
        }

        return Task.CompletedTask;
    }

    public override async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        var paths = _spatialAudioInstances.Keys.ToList();
        foreach (var path in paths)
        {
            await StopAsync(path, cancellationToken);
        }
        
        _logger.LogDebug("Stopped all 3D audio instances");
    }

    #region ISpatialAudioCapability Implementation

    public Task SetListenerPositionAsync(Vector3 position, CancellationToken cancellationToken = default)
    {
        _listenerPosition = position;
        
        // Update all spatial audio instances with new listener position
        foreach (var instance in _spatialAudioInstances.Values)
        {
            instance.UpdateSpatialParameters(_listenerPosition, _listenerForward, _listenerUp);
        }
        
        _logger.LogDebug("Listener position updated to ({X}, {Y}, {Z})", position.X, position.Y, position.Z);
        return Task.CompletedTask;
    }

    public async Task Play3DAudioAsync(string path, Vector3 position, float volume, CancellationToken cancellationToken = default)
    {
        if (!ValidateAudioPath(path))
        {
            return;
        }

        try
        {
            // Stop any existing instance for this path
            if (_spatialAudioInstances.ContainsKey(path))
            {
                await StopAsync(path, cancellationToken);
            }

            // Create new 3D audio instance
            var instance = new Audio3DInstance(path, position, volume, _logger);
            instance.UpdateSpatialParameters(_listenerPosition, _listenerForward, _listenerUp);
            
            // Store and start playback
            _spatialAudioInstances[path] = instance;
            await instance.StartAsync(cancellationToken);
            
            _logger.LogDebug("Started 3D audio: {Path} at position ({X}, {Y}, {Z}) with volume {Volume}", 
                path, position.X, position.Y, position.Z, volume);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play 3D audio: {Path}", path);
        }
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(ISpatialAudioCapability) };
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

    #region 3D Audio Configuration

    /// <summary>
    /// Sets the listener's orientation in 3D space.
    /// </summary>
    /// <param name="forward">Forward direction vector.</param>
    /// <param name="up">Up direction vector.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task SetListenerOrientationAsync(Vector3 forward, Vector3 up, CancellationToken cancellationToken = default)
    {
        _listenerForward = forward;
        _listenerUp = up;
        
        // Update all spatial audio instances
        foreach (var instance in _spatialAudioInstances.Values)
        {
            instance.UpdateSpatialParameters(_listenerPosition, _listenerForward, _listenerUp);
        }
        
        _logger.LogDebug("Listener orientation updated - Forward: ({X}, {Y}, {Z}), Up: ({X2}, {Y2}, {Z2})", 
            forward.X, forward.Y, forward.Z, up.X, up.Y, up.Z);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the position of an existing 3D audio source.
    /// </summary>
    /// <param name="path">Path to the audio file.</param>
    /// <param name="position">New 3D position.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    public Task UpdateAudioPositionAsync(string path, Vector3 position, CancellationToken cancellationToken = default)
    {
        if (_spatialAudioInstances.TryGetValue(path, out var instance))
        {
            instance.Position = position;
            instance.UpdateSpatialParameters(_listenerPosition, _listenerForward, _listenerUp);
            
            _logger.LogDebug("Updated position for 3D audio: {Path} to ({X}, {Y}, {Z})", 
                path, position.X, position.Y, position.Z);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the Doppler effect factor for 3D audio.
    /// </summary>
    /// <param name="factor">Doppler factor (1.0 = realistic, 0.0 = disabled).</param>
    public void SetDopplerFactor(float factor)
    {
        _dopplerFactor = Math.Max(0.0f, factor);
        _logger.LogDebug("Doppler factor set to {Factor}", _dopplerFactor);
    }

    /// <summary>
    /// Sets the speed of sound for 3D audio calculations.
    /// </summary>
    /// <param name="speedMs">Speed of sound in meters per second.</param>
    public void SetSpeedOfSound(float speedMs)
    {
        _speedOfSound = Math.Max(1.0f, speedMs);
        _logger.LogDebug("Speed of sound set to {Speed} m/s", _speedOfSound);
    }

    /// <summary>
    /// Gets information about all active 3D audio instances.
    /// </summary>
    /// <returns>Dictionary of audio paths and their 3D properties.</returns>
    public Dictionary<string, Audio3DInfo> GetActive3DAudioInfo()
    {
        var info = new Dictionary<string, Audio3DInfo>();
        
        foreach (var kvp in _spatialAudioInstances)
        {
            var instance = kvp.Value;
            var distance = CalculateDistance(_listenerPosition, instance.Position);
            
            info[kvp.Key] = new Audio3DInfo
            {
                Position = instance.Position,
                Volume = instance.Volume,
                Distance = distance,
                IsPlaying = instance.IsPlaying
            };
        }
        
        return info;
    }

    #endregion

    private static float CalculateDistance(Vector3 listener, Vector3 source)
    {
        var dx = source.X - listener.X;
        var dy = source.Y - listener.Y;
        var dz = source.Z - listener.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}

/// <summary>
/// Represents a 3D audio instance with spatial properties.
/// </summary>
internal class Audio3DInstance : IDisposable
{
    public string Path { get; }
    public Vector3 Position { get; set; }
    public float Volume { get; set; }
    public bool IsPlaying { get; private set; }

    private readonly ILogger _logger;
    private bool _disposed = false;

    public Audio3DInstance(string path, Vector3 position, float volume, ILogger logger)
    {
        Path = path;
        Position = position;
        Volume = volume;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsPlaying = true;
        _logger.LogDebug("3D audio instance started: {Path}", Path);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        IsPlaying = false;
        _logger.LogDebug("3D audio instance stopped: {Path}", Path);
    }

    public void UpdateSpatialParameters(Vector3 listenerPosition, Vector3 listenerForward, Vector3 listenerUp)
    {
        if (_disposed) return;

        // Calculate spatial audio parameters
        var distance = CalculateDistance(listenerPosition, Position);
        var attenuatedVolume = CalculateVolumeAttenuation(Volume, distance);
        
        // In a real implementation, this would update the actual audio engine
        _logger.LogTrace("Updated spatial parameters for {Path}: distance={Distance:F2}, volume={Volume:F2}", 
            Path, distance, attenuatedVolume);
    }

    private static float CalculateDistance(Vector3 listener, Vector3 source)
    {
        var dx = source.X - listener.X;
        var dy = source.Y - listener.Y;
        var dz = source.Z - listener.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static float CalculateVolumeAttenuation(float originalVolume, float distance)
    {
        // Simple linear distance attenuation
        var maxDistance = 100.0f;
        var attenuation = Math.Max(0.0f, 1.0f - (distance / maxDistance));
        return originalVolume * attenuation;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}

/// <summary>
/// Information about a 3D audio source.
/// </summary>
public class Audio3DInfo
{
    public required Vector3 Position { get; init; }
    public required float Volume { get; init; }
    public required float Distance { get; init; }
    public required bool IsPlaying { get; init; }
}