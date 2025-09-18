using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Audio.Services.Implementation;

/// <summary>
/// Service for advanced audio mixing and channel management.
/// Provides fine-grained control over audio channels and mixing.
/// </summary>
[Service("AudioMixer", Categories = new[] { "Audio" })]
public sealed class AudioMixerService : BaseAudioService
{
    private readonly ConcurrentDictionary<string, AudioChannel> _channels = new();
    private readonly ConcurrentDictionary<string, AudioBus> _buses = new();
    private float _masterVolume = 1.0f;
    private bool _masterMuted = false;

    public AudioMixerService(ILogger<AudioMixerService>? logger = null) 
        : base(logger)
    {
        // Initialize default buses
        CreateBus("Master", isMaster: true);
        CreateBus("SFX", parentBus: "Master");
        CreateBus("Music", parentBus: "Master");
        CreateBus("Voice", parentBus: "Master");
        CreateBus("Ambient", parentBus: "Master");
    }

    /// <summary>
    /// Creates a new audio channel.
    /// </summary>
    /// <param name="channelId">Unique identifier for the channel.</param>
    /// <param name="busName">Name of the bus to route this channel to.</param>
    /// <param name="volume">Initial volume level (0.0 to 1.0).</param>
    /// <returns>True if channel was created successfully.</returns>
    public async Task<bool> CreateChannelAsync(string channelId, string busName = "Master", float volume = 1.0f)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(channelId))
            throw new ArgumentException("Channel ID cannot be null or empty", nameof(channelId));

        if (_channels.ContainsKey(channelId))
        {
            Logger?.LogWarning("Channel {ChannelId} already exists", channelId);
            return false;
        }

        if (!_buses.ContainsKey(busName))
        {
            Logger?.LogWarning("Bus {BusName} does not exist", busName);
            return false;
        }

        var channel = new AudioChannel(channelId, busName, volume);
        _channels[channelId] = channel;
        
        Logger?.LogDebug("Created audio channel {ChannelId} on bus {BusName}", channelId, busName);
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Sets the volume for a specific channel.
    /// </summary>
    /// <param name="channelId">Channel identifier.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public async Task SetChannelVolumeAsync(string channelId, float volume)
    {
        ThrowIfDisposed();
        
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.Volume = volume;
            Logger?.LogDebug("Set channel {ChannelId} volume to {Volume}", channelId, volume);
        }
        else
        {
            Logger?.LogWarning("Channel {ChannelId} not found", channelId);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the volume for a specific channel.
    /// </summary>
    /// <param name="channelId">Channel identifier.</param>
    /// <returns>Volume level (0.0 to 1.0), or 0.0 if channel not found.</returns>
    public async Task<float> GetChannelVolumeAsync(string channelId)
    {
        ThrowIfDisposed();
        
        await Task.CompletedTask;
        return _channels.TryGetValue(channelId, out var channel) ? channel.Volume : 0.0f;
    }

    /// <summary>
    /// Sets the volume for a specific bus.
    /// </summary>
    /// <param name="busName">Bus name.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public async Task SetBusVolumeAsync(string busName, float volume)
    {
        ThrowIfDisposed();
        
        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");

        if (_buses.TryGetValue(busName, out var bus))
        {
            bus.Volume = volume;
            Logger?.LogDebug("Set bus {BusName} volume to {Volume}", busName, volume);
        }
        else
        {
            Logger?.LogWarning("Bus {BusName} not found", busName);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the volume for a specific bus.
    /// </summary>
    /// <param name="busName">Bus name.</param>
    /// <returns>Volume level (0.0 to 1.0), or 0.0 if bus not found.</returns>
    public async Task<float> GetBusVolumeAsync(string busName)
    {
        ThrowIfDisposed();
        
        await Task.CompletedTask;
        return _buses.TryGetValue(busName, out var bus) ? bus.Volume : 0.0f;
    }

    /// <summary>
    /// Mutes or unmutes a specific channel.
    /// </summary>
    /// <param name="channelId">Channel identifier.</param>
    /// <param name="muted">True to mute, false to unmute.</param>
    public async Task SetChannelMutedAsync(string channelId, bool muted)
    {
        ThrowIfDisposed();
        
        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.IsMuted = muted;
            Logger?.LogDebug("Set channel {ChannelId} muted: {Muted}", channelId, muted);
        }
        else
        {
            Logger?.LogWarning("Channel {ChannelId} not found", channelId);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Mutes or unmutes a specific bus.
    /// </summary>
    /// <param name="busName">Bus name.</param>
    /// <param name="muted">True to mute, false to unmute.</param>
    public async Task SetBusMutedAsync(string busName, bool muted)
    {
        ThrowIfDisposed();
        
        if (_buses.TryGetValue(busName, out var bus))
        {
            bus.IsMuted = muted;
            Logger?.LogDebug("Set bus {BusName} muted: {Muted}", busName, muted);
        }
        else
        {
            Logger?.LogWarning("Bus {BusName} not found", busName);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the effective volume for a channel (considering bus hierarchy).
    /// </summary>
    /// <param name="channelId">Channel identifier.</param>
    /// <returns>Effective volume level (0.0 to 1.0).</returns>
    public async Task<float> GetEffectiveChannelVolumeAsync(string channelId)
    {
        ThrowIfDisposed();
        
        if (!_channels.TryGetValue(channelId, out var channel))
            return 0.0f;

        if (channel.IsMuted || _masterMuted)
            return 0.0f;

        var effectiveVolume = channel.Volume * _masterVolume;
        
        // Apply bus volume hierarchy
        if (_buses.TryGetValue(channel.BusName, out var bus))
        {
            effectiveVolume *= GetBusEffectiveVolume(bus);
        }
        
        return await Task.FromResult(effectiveVolume);
    }

    /// <summary>
    /// Gets all available channels.
    /// </summary>
    /// <returns>Collection of channel information.</returns>
    public async Task<IEnumerable<AudioChannelInfo>> GetChannelsAsync()
    {
        ThrowIfDisposed();
        
        await Task.CompletedTask;
        return _channels.Values.Select(c => new AudioChannelInfo(
            c.ChannelId,
            c.BusName,
            c.Volume,
            c.IsMuted
        ));
    }

    /// <summary>
    /// Gets all available buses.
    /// </summary>
    /// <returns>Collection of bus information.</returns>
    public async Task<IEnumerable<AudioBusInfo>> GetBusesAsync()
    {
        ThrowIfDisposed();
        
        await Task.CompletedTask;
        return _buses.Values.Select(b => new AudioBusInfo(
            b.BusName,
            b.ParentBusName,
            b.Volume,
            b.IsMuted,
            b.IsMaster
        ));
    }

    private void CreateBus(string busName, string? parentBus = null, bool isMaster = false)
    {
        var bus = new AudioBus(busName, parentBus, isMaster);
        _buses[busName] = bus;
        Logger?.LogDebug("Created audio bus {BusName}", busName);
    }

    private float GetBusEffectiveVolume(AudioBus bus)
    {
        if (bus.IsMuted) return 0.0f;
        
        var volume = bus.Volume;
        
        // Apply parent bus volume recursively
        if (!string.IsNullOrEmpty(bus.ParentBusName) && 
            _buses.TryGetValue(bus.ParentBusName, out var parentBus))
        {
            volume *= GetBusEffectiveVolume(parentBus);
        }
        
        return volume;
    }

    /// <summary>
    /// Represents an audio channel.
    /// </summary>
    private sealed class AudioChannel
    {
        public string ChannelId { get; }
        public string BusName { get; }
        public float Volume { get; set; }
        public bool IsMuted { get; set; }

        public AudioChannel(string channelId, string busName, float volume)
        {
            ChannelId = channelId;
            BusName = busName;
            Volume = volume;
            IsMuted = false;
        }
    }

    /// <summary>
    /// Represents an audio bus.
    /// </summary>
    private sealed class AudioBus
    {
        public string BusName { get; }
        public string? ParentBusName { get; }
        public float Volume { get; set; }
        public bool IsMuted { get; set; }
        public bool IsMaster { get; }

        public AudioBus(string busName, string? parentBusName, bool isMaster = false)
        {
            BusName = busName;
            ParentBusName = parentBusName;
            Volume = 1.0f;
            IsMuted = false;
            IsMaster = isMaster;
        }
    }
}

/// <summary>
/// Information about an audio channel.
/// </summary>
public sealed record AudioChannelInfo(
    string ChannelId,
    string BusName,
    float Volume,
    bool IsMuted
);

/// <summary>
/// Information about an audio bus.
/// </summary>
public sealed record AudioBusInfo(
    string BusName,
    string? ParentBusName,
    float Volume,
    bool IsMuted,
    bool IsMaster
);