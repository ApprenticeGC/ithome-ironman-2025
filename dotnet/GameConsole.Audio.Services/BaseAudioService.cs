using GameConsole.Core.Abstractions;
using GameConsole.Audio.Services;
using Microsoft.Extensions.Logging;

namespace GameConsole.Audio.Services;

/// <summary>
/// Base implementation for audio services providing common functionality.
/// </summary>
public abstract class BaseAudioService : IService
{
    protected readonly ILogger _logger;
    protected readonly Dictionary<string, float> _categoryVolumes;
    protected float _masterVolume = 1.0f;
    private bool _isRunning = false;

    protected BaseAudioService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _categoryVolumes = new Dictionary<string, float>
        {
            ["Master"] = 1.0f,
            ["Music"] = 1.0f,
            ["SFX"] = 1.0f,
            ["Voice"] = 1.0f,
            ["Ambient"] = 1.0f
        };
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
        await OnStopAsync(cancellationToken);
        _isRunning = false;
        _logger.LogInformation("Stopped {ServiceType}", GetType().Name);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        await OnDisposeAsync();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region IAudioService Implementation

    public abstract Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default);
    public abstract Task StopAsync(string path, CancellationToken cancellationToken = default);
    public abstract Task StopAllAsync(CancellationToken cancellationToken = default);

    public virtual Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default)
    {
        _masterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        _logger.LogDebug("Master volume set to {Volume}", _masterVolume);
        return Task.CompletedTask;
    }

    public virtual Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default)
    {
        var clampedVolume = Math.Clamp(volume, 0.0f, 1.0f);
        _categoryVolumes[category] = clampedVolume;
        _logger.LogDebug("Category {Category} volume set to {Volume}", category, clampedVolume);
        return Task.CompletedTask;
    }

    public virtual Task<float> GetCategoryVolumeAsync(string category, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_categoryVolumes.GetValueOrDefault(category, 1.0f));
    }

    #endregion

    #region Protected Methods

    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;

    protected float GetEffectiveVolume(string category)
    {
        var categoryVolume = _categoryVolumes.GetValueOrDefault(category, 1.0f);
        return _masterVolume * categoryVolume;
    }

    protected bool ValidateAudioPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Invalid audio path: null or empty");
            return false;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("Audio file not found: {Path}", path);
            return false;
        }

        return true;
    }

    #endregion
}