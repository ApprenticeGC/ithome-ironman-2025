using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Audio.Core.Tests;

/// <summary>
/// Example implementations and usage tests for the audio service interfaces.
/// These demonstrate how the interfaces can be used and show compliance with the capability provider pattern.
/// </summary>
public class AudioServiceUsageExampleTests
{
    /// <summary>
    /// Example test implementation of IAudioService for demonstration purposes.
    /// Shows how the interface would be implemented following the 4-tier architecture.
    /// </summary>
    private class TestAudioService : IAudioService
    {
        public bool IsRunning => true;

        // IService lifecycle methods
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        // ICapabilityProvider methods (inherited through IService)
        public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            var capabilities = new[] { typeof(IAudioDeviceEnumerator) };
            return Task.FromResult<IEnumerable<Type>>(capabilities);
        }

        public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(typeof(T) == typeof(IAudioDeviceEnumerator));
        }

        public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            if (typeof(T) == typeof(IAudioDeviceEnumerator))
            {
                return Task.FromResult<T?>(new TestAudioDeviceEnumerator() as T);
            }
            return Task.FromResult<T?>(null);
        }

        // Core audio functionality
        public Task<bool> PlayAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task StopAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PauseAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ResumeAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        // Volume control
        public Task SetMasterVolumeAsync(float volume, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<float> GetMasterVolumeAsync(CancellationToken cancellationToken = default) => Task.FromResult(1.0f);
        public Task SetCategoryVolumeAsync(string category, float volume, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<float> GetCategoryVolumeAsync(string category, CancellationToken cancellationToken = default) => Task.FromResult(1.0f);

        // Stream management
        public Task<IAudioStream> CreateStreamAsync(string path, string category = "SFX", CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAudioStream>(new TestAudioStream(path, category));
        }

        public Task<IEnumerable<IAudioStream>> GetActiveStreamsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<IAudioStream>>(new List<IAudioStream>());
        }

        // Format support
        public Task<IEnumerable<AudioFormat>> GetSupportedFormatsAsync(CancellationToken cancellationToken = default)
        {
            var formats = new[] { AudioFormat.Wav, AudioFormat.Mp3, AudioFormat.Ogg };
            return Task.FromResult<IEnumerable<AudioFormat>>(formats);
        }

        public Task<bool> IsFormatSupportedAsync(AudioFormat format, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(format is AudioFormat.Wav or AudioFormat.Mp3 or AudioFormat.Ogg);
        }

        // Device management
        public Task SetActiveDeviceAsync(string deviceId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IAudioDevice?> GetActiveDeviceAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAudioDevice?>(new TestAudioDevice());
        }
    }

    private class TestAudioDeviceEnumerator : IAudioDeviceEnumerator
    {
#pragma warning disable CS0067 // Event is never used - this is a test implementation
        public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;
#pragma warning restore CS0067

        public Task<IEnumerable<IAudioDevice>> GetAvailableDevicesAsync(CancellationToken cancellationToken = default)
        {
            var devices = new[] { new TestAudioDevice() };
            return Task.FromResult<IEnumerable<IAudioDevice>>(devices);
        }

        public Task<IAudioDevice?> GetDefaultDeviceAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAudioDevice?>(new TestAudioDevice());
        }

        public Task<IAudioDevice?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAudioDevice?>(new TestAudioDevice());
        }
    }

    private class TestAudioDevice : IAudioDevice
    {
        public string Id => "test-device-1";
        public string Name => "Test Audio Device";
        public bool IsAvailable => true;
        public bool IsDefault => true;
        public IEnumerable<AudioFormat> SupportedFormats => new[] { AudioFormat.Wav, AudioFormat.Mp3 };
    }

    private class TestAudioStream : IAudioStream
    {
        public TestAudioStream(string source, string category)
        {
            Source = source;
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; }
        public string Source { get; }
        public AudioFormat Format => AudioFormat.Wav;
        public TimeSpan Duration => TimeSpan.FromMinutes(3);
        public TimeSpan Position => TimeSpan.Zero;
        public float Volume => 1.0f;
        public bool IsPlaying => false;
        public bool IsPaused => false;
        public bool CanSeek => true;

#pragma warning disable CS0067 // Events are never used - this is a test implementation
        public event EventHandler<AudioStreamPositionChangedEventArgs>? PositionChanged;
        public event EventHandler<AudioStreamStateChangedEventArgs>? StateChanged;
        public event EventHandler<EventArgs>? PlaybackCompleted;
#pragma warning restore CS0067

        public Task PlayAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PauseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetVolumeAsync(float volume, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AudioService_Usage_Example_Should_Work()
    {
        // Arrange
        var audioService = new TestAudioService();

        // Act & Assert - Basic playback
        var playResult = await audioService.PlayAsync("test.wav", "SFX");
        Assert.True(playResult);

        // Act & Assert - Volume control
        await audioService.SetMasterVolumeAsync(0.8f);
        var masterVolume = await audioService.GetMasterVolumeAsync();
        Assert.Equal(1.0f, masterVolume); // Test implementation returns 1.0f

        await audioService.SetCategoryVolumeAsync("Music", 0.6f);
        var categoryVolume = await audioService.GetCategoryVolumeAsync("Music");
        Assert.Equal(1.0f, categoryVolume); // Test implementation returns 1.0f

        // Act & Assert - Stream management
        var stream = await audioService.CreateStreamAsync("test-music.mp3", "Music");
        Assert.NotNull(stream);
        Assert.Equal("test-music.mp3", stream.Source);

        // Act & Assert - Format support
        var supportedFormats = await audioService.GetSupportedFormatsAsync();
        Assert.Contains(AudioFormat.Wav, supportedFormats);
        Assert.Contains(AudioFormat.Mp3, supportedFormats);
        Assert.Contains(AudioFormat.Ogg, supportedFormats);

        var isWavSupported = await audioService.IsFormatSupportedAsync(AudioFormat.Wav);
        Assert.True(isWavSupported);

        // Act & Assert - Device management
        var activeDevice = await audioService.GetActiveDeviceAsync();
        Assert.NotNull(activeDevice);
        Assert.Equal("test-device-1", activeDevice.Id);
    }

    [Fact]
    public async Task AudioService_Should_Support_Capability_Discovery()
    {
        // Arrange
        var audioService = new TestAudioService();

        // Act & Assert - Capability discovery
        var capabilities = await audioService.GetCapabilitiesAsync();
        Assert.Contains(typeof(IAudioDeviceEnumerator), capabilities);

        var hasEnumeratorCapability = await audioService.HasCapabilityAsync<IAudioDeviceEnumerator>();
        Assert.True(hasEnumeratorCapability);

        var enumerator = await audioService.GetCapabilityAsync<IAudioDeviceEnumerator>();
        Assert.NotNull(enumerator);

        // Act & Assert - Device enumeration
        var devices = await enumerator.GetAvailableDevicesAsync();
        Assert.NotEmpty(devices);

        var defaultDevice = await enumerator.GetDefaultDeviceAsync();
        Assert.NotNull(defaultDevice);
        Assert.True(defaultDevice.IsDefault);
    }

    [Fact]
    public async Task AudioStream_Should_Support_Position_Tracking_And_Seeking()
    {
        // Arrange
        var audioService = new TestAudioService();
        var stream = await audioService.CreateStreamAsync("test.wav");

        // Act & Assert - Stream properties
        Assert.True(stream.CanSeek);
        Assert.Equal(TimeSpan.Zero, stream.Position);
        Assert.Equal(TimeSpan.FromMinutes(3), stream.Duration);

        // Act & Assert - Stream control
        await stream.PlayAsync();
        await stream.SeekAsync(TimeSpan.FromSeconds(30));
        await stream.SetVolumeAsync(0.5f);
        await stream.PauseAsync();
        await stream.StopAsync();

        // Act & Assert - Cleanup
        await stream.DisposeAsync();
    }
}