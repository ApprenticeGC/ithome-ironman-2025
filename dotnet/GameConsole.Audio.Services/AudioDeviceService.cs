using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Audio.Services;

/// <summary>
/// Audio device management service for hardware abstraction.
/// Provides device enumeration, selection, and hardware-specific optimizations.
/// </summary>
[Service("Audio Device Service", "1.0.0", "Hardware abstraction and device management for audio systems", 
         Categories = new[] { "Audio", "Hardware", "Devices" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class AudioDeviceService : BaseAudioService, IAudioDeviceCapability
{
    private readonly List<AudioDevice> _availableDevices = new();
    private AudioDevice? _activeDevice;
    private readonly object _deviceLock = new();

    public AudioDeviceService(ILogger<AudioDeviceService> logger) : base(logger)
    {
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing audio device management");
        
        await RefreshAvailableDevicesAsync(cancellationToken);
        
        // Set default device as active if available
        var defaultDevice = _availableDevices.FirstOrDefault(d => d.IsDefault);
        if (defaultDevice != null)
        {
            await SetActiveDeviceAsync(defaultDevice.Id, cancellationToken);
        }
        
        _logger.LogDebug("Audio device management initialized with {Count} devices", _availableDevices.Count);
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping audio device service");
        await StopAllAsync(cancellationToken);
    }

    protected override ValueTask OnDisposeAsync()
    {
        _logger.LogDebug("Disposing audio device service");
        
        lock (_deviceLock)
        {
            _availableDevices.Clear();
            _activeDevice = null;
        }
        
        return ValueTask.CompletedTask;
    }

    #region IAudioDeviceCapability Implementation

    public Task<IEnumerable<AudioDevice>> GetAudioDevicesAsync(CancellationToken cancellationToken = default)
    {
        lock (_deviceLock)
        {
            return Task.FromResult<IEnumerable<AudioDevice>>(_availableDevices.ToList());
        }
    }

    public Task SetActiveDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        lock (_deviceLock)
        {
            var device = _availableDevices.FirstOrDefault(d => d.Id == deviceId);
            if (device == null)
            {
                _logger.LogWarning("Cannot set active device - device ID '{DeviceId}' not found", deviceId);
                return Task.CompletedTask;
            }

            _activeDevice = device;
            _logger.LogInformation("Set active audio device to '{DeviceName}' ({DeviceId})", device.Name, deviceId);
        }

        return OnActiveDeviceChangedAsync(_activeDevice, cancellationToken);
    }

    public Task<AudioDevice?> GetActiveDeviceAsync(CancellationToken cancellationToken = default)
    {
        lock (_deviceLock)
        {
            return Task.FromResult(_activeDevice);
        }
    }

    #endregion

    #region Device Management

    /// <summary>
    /// Refreshes the list of available audio devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task RefreshAvailableDevicesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing available audio devices");

        try
        {
            var devices = new List<AudioDevice>();
            
            // For now, create a simple default device
            // In a full implementation, this would enumerate actual hardware devices
            var defaultDevice = new AudioDevice(
                Id: "default_output",
                Name: "Default Audio Device",
                Description: "System default audio output device",
                IsDefault: true,
                Type: AudioDeviceType.Default,
                MaxChannels: 2
            );
            
            devices.Add(defaultDevice);

            lock (_deviceLock)
            {
                _availableDevices.Clear();
                _availableDevices.AddRange(devices);
            }

            _logger.LogInformation("Found {Count} audio devices", devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh audio devices");
            throw;
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets device-specific capabilities and limitations.
    /// </summary>
    /// <param name="deviceId">ID of the device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Device capabilities information.</returns>
    public async Task<AudioDeviceCapabilities?> GetDeviceCapabilitiesAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        lock (_deviceLock)
        {
            var device = _availableDevices.FirstOrDefault(d => d.Id == deviceId);
            if (device == null)
            {
                _logger.LogWarning("Device '{DeviceId}' not found", deviceId);
                return null;
            }

            // Extract device-specific capabilities
            var capabilities = new AudioDeviceCapabilities(
                DeviceId: deviceId,
                MaxSampleRate: GetMaxSampleRate(device),
                MinSampleRate: GetMinSampleRate(device),
                SupportedFormats: GetSupportedFormats(device),
                SupportsExclusiveMode: device.Type != AudioDeviceType.Bluetooth,
                SupportsSharedMode: true,
                BufferSizes: GetSupportedBufferSizes(device),
                Latency: GetDeviceLatency(device)
            );

            return capabilities;
        }
    }

    /// <summary>
    /// Tests if a device is currently available and functional.
    /// </summary>
    /// <param name="deviceId">ID of the device to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if device is available and functional.</returns>
    public Task<bool> TestDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        try
        {
            _logger.LogDebug("Testing audio device '{DeviceId}'", deviceId);

            // Simplified device test - just check if it exists in our list
            lock (_deviceLock)
            {
                var exists = _availableDevices.Any(d => d.Id == deviceId);
                _logger.LogDebug("Device '{DeviceId}' test {Result}", deviceId, exists ? "successful" : "failed");
                return Task.FromResult(exists);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device '{DeviceId}' test failed", deviceId);
            return Task.FromResult(false);
        }
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new Type[] { typeof(IAudioDeviceCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IAudioDeviceCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IAudioDeviceCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region Base Audio Service Overrides

    public override async Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            _logger.LogWarning("Cannot play audio - device service is not running");
            return false;
        }

        if (_activeDevice == null)
        {
            _logger.LogWarning("Cannot play audio - no active device set");
            return false;
        }

        // This service manages devices but delegates actual playback
        // In a real implementation, this would coordinate with AudioPlaybackService
        // using the selected device
        
        _logger.LogDebug("Playing audio '{Path}' on device '{DeviceName}' ({DeviceId})", 
            path, _activeDevice.Name, _activeDevice.Id);
        
        // Register as active
        var playbackInfo = new AudioPlaybackInfo(
            FilePath: path,
            Category: category,
            Format: AudioFormat.Auto,
            Duration: TimeSpan.Zero,
            Position: TimeSpan.Zero,
            IsPlaying: true,
            IsPaused: false,
            IsLooping: false,
            Volume: GetEffectiveVolume(category)
        );

        RegisterActiveAudio(path, playbackInfo);
        return true;
    }

    public override async Task StopAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Audio file path cannot be null or empty", nameof(path));

        UnregisterActiveAudio(path);
        _logger.LogDebug("Stopped audio '{Path}'", path);
    }

    #endregion

    #region Private Helper Methods

    private AudioDeviceType DetermineDeviceType(string productName)
    {
        var lowerName = productName.ToLowerInvariant();
        
        if (lowerName.Contains("headphone") || lowerName.Contains("headset"))
            return AudioDeviceType.Headphones;
        
        if (lowerName.Contains("speaker") || lowerName.Contains("internal"))
            return AudioDeviceType.Speakers;
        
        if (lowerName.Contains("usb"))
            return AudioDeviceType.USB;
        
        if (lowerName.Contains("bluetooth") || lowerName.Contains("bt"))
            return AudioDeviceType.Bluetooth;
        
        if (lowerName.Contains("hdmi"))
            return AudioDeviceType.HDMI;

        return AudioDeviceType.Other;
    }

    private int GetMaxSampleRate(AudioDevice device)
    {
        // Return typical maximum sample rates based on device type
        return device.Type switch
        {
            AudioDeviceType.USB => 192000,
            AudioDeviceType.HDMI => 192000,
            AudioDeviceType.Headphones => 96000,
            AudioDeviceType.Speakers => 48000,
            AudioDeviceType.Bluetooth => 48000,
            _ => 44100
        };
    }

    private int GetMinSampleRate(AudioDevice device)
    {
        return 8000; // Standard minimum for most devices
    }

    private AudioFormat[] GetSupportedFormats(AudioDevice device)
    {
        // Return commonly supported formats
        return new[] { AudioFormat.WAV, AudioFormat.MP3, AudioFormat.AAC };
    }

    private int[] GetSupportedBufferSizes(AudioDevice device)
    {
        // Return typical buffer sizes in samples
        return new[] { 128, 256, 512, 1024, 2048, 4096 };
    }

    private TimeSpan GetDeviceLatency(AudioDevice device)
    {
        // Estimate latency based on device type
        return device.Type switch
        {
            AudioDeviceType.USB => TimeSpan.FromMilliseconds(10),
            AudioDeviceType.Bluetooth => TimeSpan.FromMilliseconds(150),
            AudioDeviceType.HDMI => TimeSpan.FromMilliseconds(20),
            _ => TimeSpan.FromMilliseconds(30)
        };
    }

    private async Task OnActiveDeviceChangedAsync(AudioDevice? device, CancellationToken cancellationToken)
    {
        // Notify other services about device change
        // In a real implementation, this might restart audio playback on the new device
        _logger.LogInformation("Active audio device changed to: {DeviceName}", device?.Name ?? "None");
    }

    #endregion
}

/// <summary>
/// Detailed capabilities information for an audio device.
/// </summary>
public sealed record AudioDeviceCapabilities(
    string DeviceId,
    int MaxSampleRate,
    int MinSampleRate,
    AudioFormat[] SupportedFormats,
    bool SupportsExclusiveMode,
    bool SupportsSharedMode,
    int[] BufferSizes,
    TimeSpan Latency
);