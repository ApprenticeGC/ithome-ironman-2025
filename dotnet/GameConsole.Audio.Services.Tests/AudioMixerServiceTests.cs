using Microsoft.Extensions.Logging;
using GameConsole.Audio.Services;
using GameConsole.Audio.Core;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

public class AudioMixerServiceTests
{
    private readonly ILogger<AudioMixerService> _logger;

    public AudioMixerServiceTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AudioMixerService>();
    }

    [Fact]
    public async Task InitializeAsync_ShouldInitializeChannels()
    {
        // Arrange
        var service = new AudioMixerService(_logger);

        // Act
        await service.InitializeAsync();

        // Assert - Service should initialize without throwing
        Assert.True(true);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task SetChannelVolumeAsync_ShouldSetVolume()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();

        // Act
        await service.SetChannelVolumeAsync(AudioChannel.Music, 0.7f);
        var volume = await service.GetChannelVolumeAsync(AudioChannel.Music);

        // Assert
        Assert.Equal(0.7f, volume);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task SetChannelMutedAsync_ShouldMuteAndUnmute()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();
        await service.SetChannelVolumeAsync(AudioChannel.SFX, 0.8f);

        // Act - Mute
        await service.SetChannelMutedAsync(AudioChannel.SFX, true);
        var mutedVolume = await service.GetChannelVolumeAsync(AudioChannel.SFX);

        // Act - Unmute
        await service.SetChannelMutedAsync(AudioChannel.SFX, false);
        var unmutedVolume = await service.GetChannelVolumeAsync(AudioChannel.SFX);

        // Assert
        Assert.Equal(0.0f, mutedVolume);
        Assert.Equal(0.8f, unmutedVolume);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task ApplyEffectsAsync_ShouldApplyEffects()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();

        var effects = new Dictionary<string, object>
        {
            ["reverb"] = 0.5f,
            ["echo"] = 0.3f
        };

        // Act
        await service.ApplyEffectsAsync(AudioChannel.Music, effects);
        var channelEffects = service.GetChannelEffects(AudioChannel.Music);

        // Assert
        Assert.Equal(2, channelEffects.Count);
        Assert.Equal(0.5f, channelEffects["reverb"]);
        Assert.Equal(0.3f, channelEffects["echo"]);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task GetAudioLevelsAsync_ShouldReturnLevels()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();

        // Act
        var levels = await service.GetAudioLevelsAsync();

        // Assert
        Assert.NotNull(levels);
        Assert.True(levels.Count > 0);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task GetChannelEffects_ShouldReturnAppliedEffects()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();

        var effects = new Dictionary<string, object>
        {
            ["reverb"] = 0.7f,
            ["delay"] = 200
        };

        // Act
        await service.ApplyEffectsAsync(AudioChannel.Voice, effects);
        var channelEffects = service.GetChannelEffects(AudioChannel.Voice);

        // Assert
        Assert.Equal(2, channelEffects.Count);
        Assert.Equal(0.7f, channelEffects["reverb"]);
        Assert.Equal(200, channelEffects["delay"]);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task PlayAsync_ShouldLogWarning()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();

        // Act
        var result = await service.PlayAsync("test.wav", "SFX");

        // Assert
        Assert.False(result);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task HasCapabilityAsync_ShouldReturnTrueForMixingCapability()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();

        // Act
        var hasCapability = await service.HasCapabilityAsync<IAudioMixingCapability>();

        // Assert
        Assert.True(hasCapability);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task GetCapabilityAsync_ShouldReturnSelfForMixingCapability()
    {
        // Arrange
        var service = new AudioMixerService(_logger);
        await service.InitializeAsync();

        // Act
        var capability = await service.GetCapabilityAsync<IAudioMixingCapability>();

        // Assert
        Assert.NotNull(capability);
        Assert.Same(service, capability);
        
        // Clean up
        await service.DisposeAsync();
    }
}