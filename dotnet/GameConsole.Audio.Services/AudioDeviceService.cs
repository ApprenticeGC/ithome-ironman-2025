using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Audio.Services.Implementation;

/// <summary>
/// Service for audio device management and hardware abstraction.
/// Provides interface for managing audio output devices and their capabilities.
/// </summary>
[Service("AudioDevice", Categories = new[] { "Audio" })]
public sealed class AudioDeviceService : BaseAudioService, IAudioDeviceCapability
{
    private readonly ConcurrentDictionary<string, AudioDevice> _devices = new();
    private AudioDevice? _currentDevice;
    private readonly object _deviceLock = new object();

    public AudioDeviceService(ILogger<AudioDeviceService>? logger = null) 
        : base(logger)
    {
    }

    /// <summary>
    /// Gets all available capabilities provided by this service.
    /// </summary>
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(IAudioDeviceCapability) });
    }

    /// <summary>
    /// Checks if the service provides a specific capability.
    /// </summary>
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.FromResult(typeof(T) == typeof(IAudioDeviceCapability));
    }

    /// <summary>
    /// Gets a specific capability instance from the service.
    /// </summary>
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        ThrowIfDisposed();
        return Task.FromResult(typeof(T) == typeof(IAudioDeviceCapability) ? this as T : null);
    }

    /// <summary>
    /// Gets all available audio output devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of available audio devices.</returns>
    public async Task<IEnumerable<AudioDevice>> GetAvailableDevicesAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        await RefreshDeviceListAsync();
        
        return _devices.Values.OrderBy(d => d.IsDefault ? 0 : 1).ThenBy(d => d.Name);
    }

    /// <summary>
    /// Gets the currently selected audio output device.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current audio device, or null if none selected.</returns>
    public Task<AudioDevice?> GetCurrentDeviceAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        lock (_deviceLock)
        {
            return Task.FromResult(_currentDevice);
        }
    }

    /// <summary>
    /// Sets the current audio output device.
    /// </summary>
    /// <param name="device">Device to set as current.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SetCurrentDeviceAsync(AudioDevice device, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        if (!_devices.ContainsKey(device.Id))
        {
            Logger?.LogWarning("Device {DeviceId} not found in available devices", device.Id);
            return;
        }

        lock (_deviceLock)
        {
            _currentDevice = device;
        }
        
        Logger?.LogInformation("Set current audio device to {DeviceName} ({DeviceId})", device.Name, device.Id);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets detailed information about a specific device.
    /// </summary>
    /// <param name="deviceId">Device identifier.</param>
    /// <returns>Device information, or null if not found.</returns>
    public async Task<AudioDeviceInfo?> GetDeviceInfoAsync(string deviceId)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(deviceId))
            throw new ArgumentException("Device ID cannot be null or empty", nameof(deviceId));

        if (!_devices.TryGetValue(deviceId, out var device))
            return null;

        var capabilities = await GetDeviceCapabilitiesAsync(deviceId);
        
        return new AudioDeviceInfo(
            device.Id,
            device.Name,
            device.IsDefault,
            device.Type,
            capabilities.SupportedSampleRates,
            capabilities.SupportedChannelCounts,
            capabilities.SupportedFormats,
            capabilities.IsEnabled,
            capabilities.DriverName,
            capabilities.DriverVersion
        );
    }

    /// <summary>
    /// Tests if a device is available and functional.
    /// </summary>
    /// <param name="deviceId">Device identifier.</param>
    /// <returns>True if device is available and functional.</returns>
    public Task<bool> TestDeviceAsync(string deviceId)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrEmpty(deviceId))
            return Task.FromResult(false);

        // Simplified implementation - just check if device exists
        var deviceExists = _devices.ContainsKey(deviceId);
        Logger?.LogDebug("Device test for {DeviceId}: {Exists}", deviceId, deviceExists);
        return Task.FromResult(deviceExists);
    }

    /// <summary>
    /// Gets the default audio output device.
    /// </summary>
    /// <returns>Default audio device, or null if none available.</returns>
    public async Task<AudioDevice?> GetDefaultDeviceAsync()
    {
        ThrowIfDisposed();
        
        await RefreshDeviceListAsync();
        
        return _devices.Values.FirstOrDefault(d => d.IsDefault);
    }

    /// <summary>
    /// Refreshes the list of available devices.
    /// </summary>
    public async Task RefreshDeviceListAsync()
    {
        ThrowIfDisposed();
        
        _devices.Clear();
        
        try
        {
            // Create some mock devices for demonstration
            // In a real implementation, this would enumerate actual hardware devices
            var defaultDevice = new AudioDevice(
                Id: "default",
                Name: "System Default Audio Device",
                IsDefault: true,
                Type: AudioDeviceType.Speakers
            );
            
            var headphones = new AudioDevice(
                Id: "headphones_1",
                Name: "Generic Headphones",
                IsDefault: false,
                Type: AudioDeviceType.Headphones
            );
            
            var speakers = new AudioDevice(
                Id: "speakers_1", 
                Name: "Generic Speakers",
                IsDefault: false,
                Type: AudioDeviceType.Speakers
            );

            _devices[defaultDevice.Id] = defaultDevice;
            _devices[headphones.Id] = headphones;
            _devices[speakers.Id] = speakers;

            // Set current device to default if none is set
            if (_currentDevice == null)
            {
                await SetCurrentDeviceAsync(defaultDevice);
            }
            
            Logger?.LogDebug("Refreshed device list: found {DeviceCount} devices", _devices.Count);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to refresh device list");
        }
        
        await Task.CompletedTask;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await RefreshDeviceListAsync();
    }

    private Task<AudioDeviceCapabilities> GetDeviceCapabilitiesAsync(string deviceId)
    {
        // Return default capabilities for simplicity
        var capabilities = new AudioDeviceCapabilities(
            SupportedSampleRates: new[] { 44100, 48000, 96000 },
            SupportedChannelCounts: new[] { 1, 2 },
            SupportedFormats: new[] { "PCM", "Float" },
            IsEnabled: true,
            DriverName: "Generic Audio Driver",
            DriverVersion: "1.0.0"
        );
        
        return Task.FromResult(capabilities);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _devices.Clear();
        _currentDevice = null;
        await Task.CompletedTask;
    }
}

/// <summary>
/// Detailed information about an audio device.
/// </summary>
public sealed record AudioDeviceInfo(
    string Id,
    string Name,
    bool IsDefault,
    AudioDeviceType Type,
    IEnumerable<int> SupportedSampleRates,
    IEnumerable<int> SupportedChannelCounts,
    IEnumerable<string> SupportedFormats,
    bool IsEnabled,
    string DriverName,
    string DriverVersion
);

/// <summary>
/// Technical capabilities of an audio device.
/// </summary>
public sealed record AudioDeviceCapabilities(
    IEnumerable<int> SupportedSampleRates,
    IEnumerable<int> SupportedChannelCounts,
    IEnumerable<string> SupportedFormats,
    bool IsEnabled,
    string DriverName,
    string DriverVersion
);