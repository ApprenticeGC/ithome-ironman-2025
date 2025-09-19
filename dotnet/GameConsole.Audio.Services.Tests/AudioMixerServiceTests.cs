using GameConsole.Audio.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

/// <summary>
/// Unit tests for AudioMixerService.
/// </summary>
public sealed class AudioMixerServiceTests : IDisposable
{
    private readonly Mock<ILogger<AudioMixerService>> _mockLogger;
    private readonly AudioMixerService _service;

    public AudioMixerServiceTests()
    {
        _mockLogger = new Mock<ILogger<AudioMixerService>>();
        _service = new AudioMixerService(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_CreatesDefaultChannels()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        var channels = _service.GetAllChannels().ToList();
        Assert.Contains(channels, c => c.Name == "Master");
        Assert.Contains(channels, c => c.Name == "SFX");
        Assert.Contains(channels, c => c.Name == "Music");
        Assert.Contains(channels, c => c.Name == "Voice");
    }

    [Fact]
    public async Task CreateChannelAsync_CreatesNewChannel()
    {
        // Arrange
        const string channelName = "TestChannel";

        // Act
        await _service.CreateChannelAsync(channelName);

        // Assert
        var channel = _service.GetChannel(channelName);
        Assert.NotNull(channel);
        Assert.Equal(channelName, channel.Name);
        Assert.Equal(1.0f, channel.Volume);
        Assert.False(channel.IsMuted);
    }

    [Fact]
    public async Task CreateChannelAsync_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateChannelAsync(null!));
    }

    [Fact]
    public async Task SetChannelVolumeAsync_UpdatesChannelVolume()
    {
        // Arrange
        const string channelName = "TestChannel";
        const float expectedVolume = 0.6f;
        await _service.CreateChannelAsync(channelName);

        // Act
        await _service.SetChannelVolumeAsync(channelName, expectedVolume);

        // Assert
        var channel = _service.GetChannel(channelName);
        Assert.Equal(expectedVolume, channel?.Volume);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public async Task SetChannelVolumeAsync_WithInvalidVolume_ThrowsArgumentOutOfRangeException(float volume)
    {
        // Arrange
        const string channelName = "TestChannel";
        await _service.CreateChannelAsync(channelName);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SetChannelVolumeAsync(channelName, volume));
    }

    [Fact]
    public async Task SetChannelMutedAsync_UpdatesMuteState()
    {
        // Arrange
        const string channelName = "TestChannel";
        await _service.CreateChannelAsync(channelName);

        // Act
        await _service.SetChannelMutedAsync(channelName, true);

        // Assert
        var channel = _service.GetChannel(channelName);
        Assert.True(channel?.IsMuted);
    }

    [Fact]
    public async Task ApplyChannelEffectsAsync_UpdatesEffects()
    {
        // Arrange
        const string channelName = "TestChannel";
        const AudioEffects effects = AudioEffects.Reverb | AudioEffects.Echo;
        await _service.CreateChannelAsync(channelName);

        // Act
        await _service.ApplyChannelEffectsAsync(channelName, effects);

        // Assert
        var channel = _service.GetChannel(channelName);
        Assert.Equal(effects, channel?.Effects);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ReturnsAudioMixerCapability()
    {
        // Act
        var capabilities = await _service.GetCapabilitiesAsync();

        // Assert
        Assert.Contains(typeof(IAudioMixerCapability), capabilities);
    }

    [Fact]
    public async Task HasCapabilityAsync_ForAudioMixerCapability_ReturnsTrue()
    {
        // Act
        var hasCapability = await _service.HasCapabilityAsync<IAudioMixerCapability>();

        // Assert
        Assert.True(hasCapability);
    }

    [Fact]
    public async Task HasCapabilityAsync_ForOtherCapability_ReturnsFalse()
    {
        // Act
        var hasCapability = await _service.HasCapabilityAsync<ISpatialAudioCapability>();

        // Assert
        Assert.False(hasCapability);
    }

    [Fact]
    public async Task GetCapabilityAsync_ForAudioMixerCapability_ReturnsService()
    {
        // Act
        var capability = await _service.GetCapabilityAsync<IAudioMixerCapability>();

        // Assert
        Assert.Same(_service, capability);
    }

    [Fact]
    public async Task PlayAsync_AssignsToAppropriateChannel()
    {
        // Arrange
        const string testPath = "test.wav";
        const string category = "SFX";
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.PlayAsync(testPath, category);

        // Assert
        Assert.True(result);
        var sfxChannel = _service.GetChannel("SFX");
        Assert.Contains(testPath, sfxChannel?.AssignedAudioSources ?? new List<string>());
    }

    public void Dispose()
    {
        _service.DisposeAsync().AsTask().Wait();
    }
}