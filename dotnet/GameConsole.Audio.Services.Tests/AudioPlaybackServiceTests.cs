using GameConsole.Audio.Services;
using GameConsole.Audio.Services.Implementation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

/// <summary>
/// Unit tests for AudioPlaybackService.
/// </summary>
public class AudioPlaybackServiceTests
{
    private readonly Mock<ILogger<AudioPlaybackService>> _loggerMock;
    private readonly AudioPlaybackService _service;

    public AudioPlaybackServiceTests()
    {
        _loggerMock = new Mock<ILogger<AudioPlaybackService>>();
        _service = new AudioPlaybackService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task InitializeAsync_ShouldComplete()
    {
        // Act & Assert
        await _service.InitializeAsync();
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task SetMasterVolumeAsync_WithValidVolume_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await _service.SetMasterVolumeAsync(0.5f);
    }

    [Fact]
    public async Task SetMasterVolumeAsync_WithInvalidVolume_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SetMasterVolumeAsync(-0.1f));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SetMasterVolumeAsync(1.1f));
    }

    [Fact]
    public async Task SetCategoryVolumeAsync_WithValidParameters_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await _service.SetCategoryVolumeAsync("SFX", 0.8f);
    }

    [Fact]
    public async Task GetCategoryVolumeAsync_WithValidCategory_ShouldReturnVolume()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.SetCategoryVolumeAsync("Music", 0.7f);

        // Act
        var volume = await _service.GetCategoryVolumeAsync("Music");

        // Assert
        Assert.Equal(0.7f, volume);
    }

    [Fact]
    public async Task GetCategoryVolumeAsync_WithUnknownCategory_ShouldReturnDefaultVolume()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var volume = await _service.GetCategoryVolumeAsync("Unknown");

        // Assert
        Assert.Equal(1.0f, volume);
    }

    [Fact]
    public async Task StopAllAsync_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await _service.StopAllAsync();
    }

    [Fact]
    public async Task PlayAsync_WithInvalidPath_ShouldReturnFalse()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.PlayAsync("nonexistent_file.wav");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DisposeAsync_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await _service.DisposeAsync();
        Assert.False(_service.IsRunning);
    }
}