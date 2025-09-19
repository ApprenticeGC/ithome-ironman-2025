using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Audio.Services;

/// <summary>
/// Audio mixer service for volume and channel management.
/// Provides advanced volume control and channel-based audio organization.
/// </summary>
[Service("Audio Mixer Service", "1.0.0", "Advanced volume control and channel management", 
         Categories = new[] { "Audio", "Mixer", "Channels" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class AudioMixerService : BaseAudioService, IAudioMixerCapability
{
    private readonly ConcurrentDictionary<string, AudioChannel> _channels = new();
    private readonly ConcurrentDictionary<string, string> _audioToChannelMapping = new();

    public AudioMixerService(ILogger<AudioMixerService> logger) : base(logger)
    {
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing audio mixer");
        
        // Create default channels
        await CreateChannelAsync("Master", cancellationToken);
        await CreateChannelAsync("SFX", cancellationToken);
        await CreateChannelAsync("Music", cancellationToken);
        await CreateChannelAsync("Voice", cancellationToken);
        
        _logger.LogDebug("Audio mixer initialized with default channels");
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping audio mixer");
        await StopAllAsync(cancellationToken);
    }

    protected override ValueTask OnDisposeAsync()
    {
        _logger.LogDebug("Disposing audio mixer service");
        _channels.Clear();
        _audioToChannelMapping.Clear();
        return ValueTask.CompletedTask;
    }

    public override async Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audio file path cannot be null or empty", nameof(path));
        
        if (!IsRunning)
        {
            _logger.LogWarning("Cannot play audio - mixer service is not running");
            return false;
        }

        // Assign audio to appropriate channel
        var channelName = GetChannelForCategory(category);
        _audioToChannelMapping[path] = channelName;
        
        if (_channels.TryGetValue(channelName, out var channel))
        {
            // Add to channel's assigned sources
            var updatedSources = new List<string>(channel.AssignedAudioSources) { path };
            var updatedChannel = channel with { AssignedAudioSources = updatedSources };
            _channels[channelName] = updatedChannel;
        }

        // This is a mixer service - it manages channels but delegates actual playback
        // In a real implementation, this would coordinate with AudioPlaybackService
        _logger.LogDebug("Audio '{Path}' assigned to channel '{Channel}' for category '{Category}'", 
            path, channelName, category);
        
        // Register the audio as active
        var playbackInfo = new AudioPlaybackInfo(
            FilePath: path,
            Category: category,
            Format: AudioFormat.Auto,
            Duration: TimeSpan.Zero,
            Position: TimeSpan.Zero,
            IsPlaying: true,
            IsPaused: false,
            IsLooping: false,
            Volume: GetEffectiveChannelVolume(channelName)
        );
        
        RegisterActiveAudio(path, playbackInfo);
        return true;
    }

    public override async Task StopAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audio file path cannot be null or empty", nameof(path));

        // Remove from channel assignment
        if (_audioToChannelMapping.TryRemove(path, out var channelName))
        {
            if (_channels.TryGetValue(channelName, out var channel))
            {
                var updatedSources = channel.AssignedAudioSources.Where(s => s != path).ToList();
                var updatedChannel = channel with { AssignedAudioSources = updatedSources };
                _channels[channelName] = updatedChannel;
            }
        }

        UnregisterActiveAudio(path);
        _logger.LogDebug("Stopped audio '{Path}' and removed from channel '{Channel}'", path, channelName);
    }

    #region IAudioMixerCapability Implementation

    public Task CreateChannelAsync(string channelName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));

        var channel = new AudioChannel(
            Name: channelName,
            Volume: 1.0f,
            IsMuted: false,
            Effects: AudioEffects.None,
            AssignedAudioSources: new List<string>()
        );

        _channels[channelName] = channel;
        _logger.LogDebug("Created audio channel '{ChannelName}'", channelName);
        
        return Task.CompletedTask;
    }

    public Task SetChannelVolumeAsync(string channelName, float volume, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));
        
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        if (!_channels.TryGetValue(channelName, out var channel))
        {
            _logger.LogWarning("Channel '{ChannelName}' does not exist", channelName);
            return Task.CompletedTask;
        }

        var updatedChannel = channel with { Volume = volume };
        _channels[channelName] = updatedChannel;
        
        _logger.LogDebug("Set volume for channel '{ChannelName}' to {Volume}", channelName, volume);
        
        // Notify about volume change for all audio in this channel
        return OnChannelVolumeChangedAsync(channelName, volume, cancellationToken);
    }

    public Task ApplyChannelEffectsAsync(string channelName, AudioEffects effects, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));

        if (!_channels.TryGetValue(channelName, out var channel))
        {
            _logger.LogWarning("Channel '{ChannelName}' does not exist", channelName);
            return Task.CompletedTask;
        }

        var updatedChannel = channel with { Effects = effects };
        _channels[channelName] = updatedChannel;
        
        _logger.LogDebug("Applied effects {Effects} to channel '{ChannelName}'", effects, channelName);
        return Task.CompletedTask;
    }

    #endregion

    #region Additional Public Methods

    /// <summary>
    /// Gets information about a specific channel.
    /// </summary>
    /// <param name="channelName">Name of the channel.</param>
    /// <returns>Channel information, or null if not found.</returns>
    public AudioChannel? GetChannel(string channelName)
    {
        return _channels.TryGetValue(channelName, out var channel) ? channel : null;
    }

    /// <summary>
    /// Gets all available channels.
    /// </summary>
    /// <returns>Collection of all channels.</returns>
    public IEnumerable<AudioChannel> GetAllChannels()
    {
        return _channels.Values;
    }

    /// <summary>
    /// Mutes or unmutes a specific channel.
    /// </summary>
    /// <param name="channelName">Name of the channel.</param>
    /// <param name="muted">True to mute, false to unmute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task SetChannelMutedAsync(string channelName, bool muted, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));

        if (!_channels.TryGetValue(channelName, out var channel))
        {
            _logger.LogWarning("Channel '{ChannelName}' does not exist", channelName);
            return Task.CompletedTask;
        }

        var updatedChannel = channel with { IsMuted = muted };
        _channels[channelName] = updatedChannel;
        
        _logger.LogDebug("{Action} channel '{ChannelName}'", muted ? "Muted" : "Unmuted", channelName);
        return Task.CompletedTask;
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new Type[] { typeof(IAudioMixerCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IAudioMixerCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IAudioMixerCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Private Helper Methods

    private string GetChannelForCategory(string category)
    {
        return category switch
        {
            "SFX" => "SFX",
            "Music" => "Music", 
            "Voice" => "Voice",
            _ => "SFX" // Default fallback
        };
    }

    private float GetEffectiveChannelVolume(string channelName)
    {
        if (!_channels.TryGetValue(channelName, out var channel))
            return GetMasterVolume();

        if (channel.IsMuted)
            return 0.0f;

        return GetMasterVolume() * channel.Volume;
    }

    private async Task OnChannelVolumeChangedAsync(string channelName, float volume, CancellationToken cancellationToken)
    {
        // In a real implementation, this would update the volume of all audio sources in this channel
        // For now, we just log the change
        _logger.LogDebug("Channel '{ChannelName}' volume changed to {Volume}, affecting {Count} audio sources", 
            channelName, volume, _channels[channelName].AssignedAudioSources.Count);
    }

    #endregion
}