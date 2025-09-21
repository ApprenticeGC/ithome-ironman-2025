using GameConsole.Core.Abstractions;
using GameConsole.Audio.Core;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace GameConsole.Audio.Services;

/// <summary>
/// Audio device service for hardware abstraction and device management.
/// </summary>
[Service("AudioDeviceService", "1.0.0", "Audio hardware abstraction and device management service")]
public class AudioDeviceService : BaseAudioService
{
    private readonly List<AudioDeviceInfo> _availableDevices;
    private AudioDeviceInfo? _currentOutputDevice;
    private MMDeviceEnumerator? _deviceEnumerator;

    public AudioDeviceService(ILogger<AudioDeviceService> logger) : base(logger)
    {
        _availableDevices = new List<AudioDeviceInfo>();
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initializing audio device system");
        
        try
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            RefreshDeviceList();
            SetDefaultDevices();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize audio device system");
            throw;
        }

        return Task.CompletedTask;
    }

    protected override ValueTask OnDisposeAsync()
    {
        _deviceEnumerator?.Dispose();
        return ValueTask.CompletedTask;
    }

    public override Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
    {
        // AudioDeviceService doesn't directly play audio - it manages devices
        _logger.LogWarning("AudioDeviceService.PlayAsync called - this service manages devices, not direct playback");
        return Task.FromResult(false);
    }

    public override Task StopAsync(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("AudioDeviceService.StopAsync called - this service manages devices, not direct playback");
        return Task.CompletedTask;
    }

    public override Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AudioDeviceService.StopAllAsync called - no direct playback to stop");
        return Task.CompletedTask;
    }

    #region Device Management

    private void RefreshDeviceList()
    {
        _availableDevices.Clear();

        try
        {
            if (_deviceEnumerator == null)
            {
                _logger.LogWarning("Device enumerator not initialized");
                return;
            }

            var outputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in outputDevices)
            {
                var deviceInfo = new AudioDeviceInfo
                {
                    DeviceId = device.ID,
                    Name = device.FriendlyName,
                    IsDefault = device.ID == _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID,
                    Channels = device.AudioClient.MixFormat.Channels,
                    SampleRate = device.AudioClient.MixFormat.SampleRate
                };
                
                _availableDevices.Add(deviceInfo);
            }

            _logger.LogDebug("Refreshed device list - found {DeviceCount} output devices", _availableDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh device list");
        }
    }

    public Task<List<AudioDeviceInfo>> GetAvailableOutputDevicesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<AudioDeviceInfo>(_availableDevices));
    }

    public Task SetOutputDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var device = _availableDevices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device != null)
        {
            _currentOutputDevice = device;
            _logger.LogInformation("Output device set to: {DeviceName} ({DeviceId})", device.Name, device.DeviceId);
        }
        else
        {
            _logger.LogWarning("Output device not found: {DeviceId}", deviceId);
        }

        return Task.CompletedTask;
    }

    public Task<AudioDeviceInfo?> GetCurrentOutputDeviceAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentOutputDevice);
    }

    private void SetDefaultDevices()
    {
        var defaultDevice = _availableDevices.FirstOrDefault(d => d.IsDefault);
        if (defaultDevice != null)
        {
            _currentOutputDevice = defaultDevice;
        }
    }

    #endregion

    #region Device Properties

    public Task<float> GetDeviceVolumeAsync(CancellationToken cancellationToken = default)
    {
        if (_currentOutputDevice == null || _deviceEnumerator == null)
        {
            return Task.FromResult(-1.0f);
        }

        try
        {
            var device = _deviceEnumerator.GetDevice(_currentOutputDevice.DeviceId);
            return Task.FromResult(device.AudioEndpointVolume.MasterVolumeLevelScalar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device volume");
            return Task.FromResult(-1.0f);
        }
    }

    public Task SetDeviceVolumeAsync(float volume, CancellationToken cancellationToken = default)
    {
        if (_currentOutputDevice == null || _deviceEnumerator == null)
        {
            _logger.LogWarning("No output device selected for volume control");
            return Task.CompletedTask;
        }

        try
        {
            var clampedVolume = Math.Clamp(volume, 0.0f, 1.0f);
            var device = _deviceEnumerator.GetDevice(_currentOutputDevice.DeviceId);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = clampedVolume;
            
            _logger.LogDebug("Device volume set to {Volume} for device {DeviceName}", clampedVolume, _currentOutputDevice.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set device volume");
        }

        return Task.CompletedTask;
    }

    public Task<bool?> IsDeviceMutedAsync(CancellationToken cancellationToken = default)
    {
        if (_currentOutputDevice == null || _deviceEnumerator == null)
        {
            return Task.FromResult<bool?>(null);
        }

        try
        {
            var device = _deviceEnumerator.GetDevice(_currentOutputDevice.DeviceId);
            return Task.FromResult<bool?>(device.AudioEndpointVolume.Mute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device mute status");
            return Task.FromResult<bool?>(null);
        }
    }

    public Task SetDeviceMutedAsync(bool muted, CancellationToken cancellationToken = default)
    {
        if (_currentOutputDevice == null || _deviceEnumerator == null)
        {
            _logger.LogWarning("No output device selected for mute control");
            return Task.CompletedTask;
        }

        try
        {
            var device = _deviceEnumerator.GetDevice(_currentOutputDevice.DeviceId);
            device.AudioEndpointVolume.Mute = muted;
            
            _logger.LogDebug("Device {Action} for device {DeviceName}", muted ? "muted" : "unmuted", _currentOutputDevice.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set device mute status");
        }

        return Task.CompletedTask;
    }

    #endregion
}