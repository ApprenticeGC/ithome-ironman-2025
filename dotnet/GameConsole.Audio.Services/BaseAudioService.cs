using GameConsole.Core.Abstractions;
using GameConsole.Audio.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Audio.Services;

/// <summary>
/// Base implementation for audio services providing common functionality.
/// </summary>
public abstract class BaseAudioService : GameConsole.Audio.Services.IService
{
    protected readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, float> _categoryVolumes = new();
    private readonly ConcurrentDictionary<string, AudioPlaybackInfo> _activeAudio = new();
    
    private bool _isRunning = false;
    private float _masterVolume = 1.0f;
    private bool _disposed = false;

    protected BaseAudioService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize default category volumes
        _categoryVolumes["SFX"] = 1.0f;
        _categoryVolumes["Music"] = 1.0f;
        _categoryVolumes["Voice"] = 1.0f;
    }

    #region IService Implementation

    public bool IsRunning => _isRunning;

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing {ServiceType}", GetType().Name);
        await OnInitializeAsync(cancellationToken);
        _logger.LogInformation("Initialized {ServiceType}", GetType().Name);
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {ServiceType}", GetType().Name);
        await OnStartAsync(cancellationToken);
        _isRunning = true;
        _logger.LogInformation("Started {ServiceType}", GetType().Name);
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping {ServiceType}", GetType().Name);
        _isRunning = false;
        await OnStopAsync(cancellationToken);
        _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (_isRunning)
        {
            await StopAsync();
        }

        await OnDisposeAsync();
        _disposed = true;
    }

    #endregion

    #region Audio Service Implementation

    public abstract Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default);
    public abstract Task StopAsync(string path, CancellationToken cancellationToken = default);

    public virtual async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping all audio playback");
        
        var activePaths = _activeAudio.Keys.ToList();
        foreach (var path in activePaths)
        {
            await StopAsync(path, cancellationToken);
        }
        
        _activeAudio.Clear();
    }

    public virtual Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default)
    {
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        _masterVolume = volume;
        _logger.LogDebug("Set master volume to {Volume}", volume);
        
        return OnMasterVolumeChangedAsync(volume, cancellationToken);
    }

    public virtual Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));
        
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        _categoryVolumes[category] = volume;
        _logger.LogDebug("Set category '{Category}' volume to {Volume}", category, volume);
        
        return OnCategoryVolumeChangedAsync(category, volume, cancellationToken);
    }

    public virtual Task<float> GetCategoryVolumeAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));

        return Task.FromResult(_categoryVolumes.GetValueOrDefault(category, 1.0f));
    }

    #endregion

    #region Protected Helper Methods

    protected float GetMasterVolume() => _masterVolume;
    
    protected float GetCategoryVolume(string category) => _categoryVolumes.GetValueOrDefault(category, 1.0f);
    
    protected float GetEffectiveVolume(string category) => _masterVolume * GetCategoryVolume(category);
    
    protected void RegisterActiveAudio(string path, AudioPlaybackInfo info)
    {
        _activeAudio[path] = info;
    }
    
    protected void UnregisterActiveAudio(string path)
    {
        _activeAudio.TryRemove(path, out _);
    }
    
    protected IReadOnlyDictionary<string, AudioPlaybackInfo> GetActiveAudio() => _activeAudio;

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;
    
    protected virtual Task OnMasterVolumeChangedAsync(float volume, CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnCategoryVolumeChangedAsync(string category, float volume, CancellationToken cancellationToken = default) => Task.CompletedTask;

    #endregion
}