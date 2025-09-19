using GameConsole.Audio.Core;
using GameConsole.Audio.Services;
using System.Numerics;
using Xunit;

namespace GameConsole.Audio.Core.Tests;

/// <summary>
/// Tests for audio types, enums, and data structures.
/// </summary>
public class AudioTypesTests
{
    [Fact]
    public void AudioCategory_Should_Have_Expected_Values()
    {
        // Act & Assert
        Assert.Equal(0, (int)AudioCategory.SFX);
        Assert.Equal(1, (int)AudioCategory.Music);
        Assert.Equal(2, (int)AudioCategory.Voice);
        Assert.Equal(3, (int)AudioCategory.Ambient);
        Assert.Equal(4, (int)AudioCategory.UI);
    }

    [Fact]
    public void AudioState_Should_Have_Expected_Values()
    {
        // Act & Assert
        Assert.Equal(0, (int)AudioState.Stopped);
        Assert.Equal(1, (int)AudioState.Playing);
        Assert.Equal(2, (int)AudioState.Paused);
        Assert.Equal(3, (int)AudioState.Loading);
        Assert.Equal(4, (int)AudioState.Error);
    }

    [Fact]
    public void AudioFormat_Should_Have_Expected_Values()
    {
        // Act & Assert
        Assert.Equal(0, (int)AudioFormat.Unknown);
        Assert.Equal(1, (int)AudioFormat.WAV);
        Assert.Equal(2, (int)AudioFormat.MP3);
        Assert.Equal(3, (int)AudioFormat.OGG);
        Assert.Equal(4, (int)AudioFormat.FLAC);
    }

    [Fact]
    public void AudioPriority_Should_Have_Expected_Values()
    {
        // Act & Assert
        Assert.Equal(0, (int)AudioPriority.Low);
        Assert.Equal(1, (int)AudioPriority.Normal);
        Assert.Equal(2, (int)AudioPriority.High);
        Assert.Equal(3, (int)AudioPriority.Critical);
    }

    [Fact]
    public void AudioEnvironment_Should_Have_Expected_Values()
    {
        // Act & Assert
        Assert.Equal(0, (int)AudioEnvironment.Default);
        Assert.Equal(1, (int)AudioEnvironment.SmallRoom);
        Assert.Equal(2, (int)AudioEnvironment.Hall);
        Assert.Equal(3, (int)AudioEnvironment.Outdoor);
        Assert.Equal(4, (int)AudioEnvironment.Cave);
        Assert.Equal(5, (int)AudioEnvironment.Underwater);
        Assert.Equal(6, (int)AudioEnvironment.Forest);
        Assert.Equal(7, (int)AudioEnvironment.Urban);
    }

    [Fact]
    public void AudioStopReason_Should_Have_Expected_Values()
    {
        // Act & Assert
        Assert.Equal(0, (int)AudioStopReason.Completed);
        Assert.Equal(1, (int)AudioStopReason.Manual);
        Assert.Equal(2, (int)AudioStopReason.Interrupted);
        Assert.Equal(3, (int)AudioStopReason.Error);
        Assert.Equal(4, (int)AudioStopReason.ResourceLimit);
    }

    [Fact]
    public void AudioPosition_TwoD_Should_Create_2D_Position()
    {
        // Act
        var position = AudioPosition.TwoD;

        // Assert
        Assert.False(position.Is3D);
        Assert.Equal(Vector3.Zero, position.Position);
        Assert.Equal(Vector3.Zero, position.Direction);
        Assert.Equal(Vector3.Zero, position.Velocity);
    }

    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(1f, 2f, 3f)]
    [InlineData(-5f, 10f, -15f)]
    public void AudioPosition_ThreeD_Should_Create_3D_Position(float x, float y, float z)
    {
        // Arrange
        var inputPosition = new Vector3(x, y, z);
        var inputDirection = new Vector3(1f, 0f, 0f);
        var inputVelocity = new Vector3(0f, 1f, 0f);

        // Act
        var position = AudioPosition.ThreeD(inputPosition, inputDirection, inputVelocity);

        // Assert
        Assert.True(position.Is3D);
        Assert.Equal(inputPosition, position.Position);
        Assert.Equal(inputDirection, position.Direction);
        Assert.Equal(inputVelocity, position.Velocity);
    }

    [Fact]
    public void AudioPosition_ThreeD_Should_Use_Default_Direction_And_Velocity()
    {
        // Arrange
        var inputPosition = new Vector3(1f, 2f, 3f);

        // Act
        var position = AudioPosition.ThreeD(inputPosition);

        // Assert
        Assert.True(position.Is3D);
        Assert.Equal(inputPosition, position.Position);
        Assert.Equal(Vector3.UnitZ, position.Direction);
        Assert.Equal(Vector3.Zero, position.Velocity);
    }

    [Fact]
    public void AudioMetadata_Should_Initialize_With_Default_Values()
    {
        // Act
        var metadata = new AudioMetadata();

        // Assert
        Assert.Equal(TimeSpan.Zero, metadata.Duration);
        Assert.Equal(AudioFormat.Unknown, metadata.Format);
        Assert.Equal(0, metadata.SampleRate);
        Assert.Equal(0, metadata.Channels);
        Assert.Equal(0, metadata.BitDepth);
        Assert.Equal(0, metadata.FileSize);
        Assert.False(metadata.IsLoopable);
        Assert.Equal(0f, metadata.DefaultVolume);
        Assert.Equal(AudioCategory.SFX, metadata.RecommendedCategory);
    }

    [Fact]
    public void AudioMetadata_Should_Allow_Initialization()
    {
        // Arrange
        var expectedDuration = TimeSpan.FromSeconds(120);
        var expectedFormat = AudioFormat.MP3;
        var expectedSampleRate = 44100;
        var expectedChannels = 2;
        var expectedBitDepth = 16;
        var expectedFileSize = 1024 * 1024;
        var expectedIsLoopable = true;
        var expectedDefaultVolume = 0.8f;
        var expectedCategory = AudioCategory.Music;

        // Act
        var metadata = new AudioMetadata
        {
            Duration = expectedDuration,
            Format = expectedFormat,
            SampleRate = expectedSampleRate,
            Channels = expectedChannels,
            BitDepth = expectedBitDepth,
            FileSize = expectedFileSize,
            IsLoopable = expectedIsLoopable,
            DefaultVolume = expectedDefaultVolume,
            RecommendedCategory = expectedCategory
        };

        // Assert
        Assert.Equal(expectedDuration, metadata.Duration);
        Assert.Equal(expectedFormat, metadata.Format);
        Assert.Equal(expectedSampleRate, metadata.SampleRate);
        Assert.Equal(expectedChannels, metadata.Channels);
        Assert.Equal(expectedBitDepth, metadata.BitDepth);
        Assert.Equal(expectedFileSize, metadata.FileSize);
        Assert.Equal(expectedIsLoopable, metadata.IsLoopable);
        Assert.Equal(expectedDefaultVolume, metadata.DefaultVolume);
        Assert.Equal(expectedCategory, metadata.RecommendedCategory);
    }

    [Fact]
    public void AudioPlaybackConfig_Default_Should_Have_Expected_Values()
    {
        // Act
        var config = AudioPlaybackConfig.Default;

        // Assert
        Assert.Equal(1.0f, config.Volume);
        Assert.False(config.Loop);
        Assert.Equal(AudioPriority.Normal, config.Priority);
        Assert.Null(config.Position);
        Assert.Equal(TimeSpan.Zero, config.StartDelay);
        Assert.Null(config.AutoStopAfter);
        Assert.Equal(TimeSpan.Zero, config.FadeIn);
        Assert.Equal(TimeSpan.Zero, config.FadeOut);
    }

    [Fact]
    public void AudioPlaybackConfig_BackgroundMusic_Should_Have_Expected_Values()
    {
        // Act
        var config = AudioPlaybackConfig.BackgroundMusic();

        // Assert
        Assert.Equal(0.7f, config.Volume);
        Assert.True(config.Loop);
        Assert.Equal(AudioPriority.Low, config.Priority);
        Assert.Equal(TimeSpan.FromSeconds(1), config.FadeIn);
        Assert.Equal(TimeSpan.FromSeconds(2), config.FadeOut);
    }

    [Fact]
    public void AudioPlaybackConfig_BackgroundMusic_Should_Accept_Custom_Volume()
    {
        // Arrange
        var customVolume = 0.5f;

        // Act
        var config = AudioPlaybackConfig.BackgroundMusic(customVolume);

        // Assert
        Assert.Equal(customVolume, config.Volume);
        Assert.True(config.Loop);
        Assert.Equal(AudioPriority.Low, config.Priority);
    }

    [Fact]
    public void AudioPlaybackConfig_SoundEffect_Should_Have_Expected_Values()
    {
        // Act
        var config = AudioPlaybackConfig.SoundEffect();

        // Assert
        Assert.Equal(1.0f, config.Volume);
        Assert.False(config.Loop);
        Assert.Equal(AudioPriority.Normal, config.Priority);
    }

    [Theory]
    [InlineData(0.5f, AudioPriority.Low)]
    [InlineData(0.8f, AudioPriority.High)]
    [InlineData(1.0f, AudioPriority.Critical)]
    public void AudioPlaybackConfig_SoundEffect_Should_Accept_Custom_Parameters(float volume, AudioPriority priority)
    {
        // Act
        var config = AudioPlaybackConfig.SoundEffect(volume, priority);

        // Assert
        Assert.Equal(volume, config.Volume);
        Assert.False(config.Loop);
        Assert.Equal(priority, config.Priority);
    }

    [Fact]
    public void AudioPlaybackConfig_Should_Allow_Custom_Initialization()
    {
        // Arrange
        var customPosition = AudioPosition.ThreeD(new Vector3(1, 2, 3));

        // Act
        var config = new AudioPlaybackConfig
        {
            Volume = 0.6f,
            Loop = true,
            Priority = AudioPriority.High,
            Position = customPosition,
            StartDelay = TimeSpan.FromMilliseconds(500),
            AutoStopAfter = TimeSpan.FromMinutes(5),
            FadeIn = TimeSpan.FromSeconds(0.5),
            FadeOut = TimeSpan.FromSeconds(1.5)
        };

        // Assert
        Assert.Equal(0.6f, config.Volume);
        Assert.True(config.Loop);
        Assert.Equal(AudioPriority.High, config.Priority);
        Assert.Equal(customPosition, config.Position);
        Assert.Equal(TimeSpan.FromMilliseconds(500), config.StartDelay);
        Assert.Equal(TimeSpan.FromMinutes(5), config.AutoStopAfter);
        Assert.Equal(TimeSpan.FromSeconds(0.5), config.FadeIn);
        Assert.Equal(TimeSpan.FromSeconds(1.5), config.FadeOut);
    }
}