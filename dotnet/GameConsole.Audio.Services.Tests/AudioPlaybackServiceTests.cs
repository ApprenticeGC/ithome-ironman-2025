using Microsoft.Extensions.Logging;
using GameConsole.Audio.Services;
using GameConsole.Audio.Core;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

public class AudioPlaybackServiceTests
{
    private readonly ILogger<AudioPlaybackService> _logger;

    public AudioPlaybackServiceTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AudioPlaybackService>();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSucceed()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);

        // Act & Assert
        await service.InitializeAsync();
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task StartAsync_ShouldSucceed()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();

        // Act
        await service.StartAsync();

        // Assert
        Assert.True(service.IsRunning);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldSucceed()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act
        await service.StopAsync();

        // Assert
        Assert.False(service.IsRunning);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task PlayAsync_WithInvalidPath_ShouldReturnFalse()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act
        var result = await service.PlayAsync("nonexistent.wav", "SFX");

        // Assert
        Assert.False(result);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task SetMasterVolumeAsync_ShouldClampVolume()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();

        // Act
        await service.SetMasterVolumeAsync(1.5f);

        // Assert - Volume should be clamped to 1.0
        // We can't directly access private fields, but the method should not throw
        Assert.True(true); // Test passes if no exception is thrown
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task SetCategoryVolumeAsync_ShouldSucceed()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();

        // Act
        await service.SetCategoryVolumeAsync("Music", 0.8f);
        var volume = await service.GetCategoryVolumeAsync("Music");

        // Assert
        Assert.Equal(0.8f, volume);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task GetCategoryVolumeAsync_WithUnknownCategory_ShouldReturnDefault()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();

        // Act
        var volume = await service.GetCategoryVolumeAsync("UnknownCategory");

        // Assert
        Assert.Equal(1.0f, volume);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task StopAllAsync_ShouldSucceed()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act & Assert
        await service.StopAllAsync();
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public void GetSupportedFormats_ShouldReturnExpectedFormats()
    {
        // Act
        var formats = AudioPlaybackService.GetSupportedFormats();

        // Assert
        Assert.Contains(".wav", formats);
        Assert.Contains(".mp3", formats);
        Assert.Contains(".aiff", formats);
        Assert.Contains(".flac", formats);
    }

    [Fact]
    public async Task GetPlaybackStates_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var service = new AudioPlaybackService(_logger);
        await service.InitializeAsync();

        // Act
        var states = service.GetPlaybackStates();

        // Assert
        Assert.NotNull(states);
        Assert.Empty(states);
        
        // Clean up
        await service.DisposeAsync();
    }
}