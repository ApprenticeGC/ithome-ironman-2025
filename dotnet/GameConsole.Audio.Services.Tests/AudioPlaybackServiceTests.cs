using GameConsole.Audio.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

/// <summary>
/// Unit tests for AudioPlaybackService.
/// </summary>
public sealed class AudioPlaybackServiceTests : IDisposable
{
    private readonly Mock<ILogger<AudioPlaybackService>> _mockLogger;
    private readonly AudioPlaybackService _service;

    public AudioPlaybackServiceTests()
    {
        _mockLogger = new Mock<ILogger<AudioPlaybackService>>();
        _service = new AudioPlaybackService(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_SetsIsRunningToFalse_Initially()
    {
        // Arrange & Act
        var isRunning = _service.IsRunning;

        // Assert
        Assert.False(isRunning);
    }

    [Fact]
    public async Task InitializeAsync_CompletesSuccessfully()
    {
        // Act
        await _service.InitializeAsync();

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task StartAsync_SetsIsRunningToTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_SetsIsRunningToFalse()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        Assert.True(_service.IsRunning);

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task PlayAsync_WhenNotRunning_ReturnsFalse()
    {
        // Arrange
        const string testPath = "test.wav";
        const string category = "SFX";

        // Act
        var result = await _service.PlayAsync(testPath, category);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PlayAsync_WhenRunning_ReturnsTrue()
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
    }

    [Fact]
    public async Task PlayAsync_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.PlayAsync(null!, "SFX"));
    }

    [Fact]
    public async Task PlayAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.PlayAsync("", "SFX"));
    }

    [Fact]
    public async Task StopAsync_WithValidPath_CompletesSuccessfully()
    {
        // Arrange
        const string testPath = "test.wav";
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.PlayAsync(testPath, "SFX");

        // Act
        await _service.StopAsync(testPath);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task StopAsync_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.StopAsync(null!));
    }

    [Fact]
    public async Task StopAllAsync_CompletesSuccessfully()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        await _service.PlayAsync("test1.wav", "SFX");
        await _service.PlayAsync("test2.wav", "Music");

        // Act
        await _service.StopAllAsync();

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task SetMasterVolumeAsync_WithValidVolume_CompletesSuccessfully()
    {
        // Act
        await _service.SetMasterVolumeAsync(0.5f);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public async Task SetMasterVolumeAsync_WithInvalidVolume_ThrowsArgumentOutOfRangeException(float volume)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SetMasterVolumeAsync(volume));
    }

    [Fact]
    public async Task SetCategoryVolumeAsync_WithValidParameters_CompletesSuccessfully()
    {
        // Act
        await _service.SetCategoryVolumeAsync("SFX", 0.7f);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public async Task GetCategoryVolumeAsync_WithValidCategory_ReturnsVolume()
    {
        // Arrange
        const string category = "SFX";
        const float expectedVolume = 0.8f;
        await _service.SetCategoryVolumeAsync(category, expectedVolume);

        // Act
        var actualVolume = await _service.GetCategoryVolumeAsync(category);

        // Assert
        Assert.Equal(expectedVolume, actualVolume);
    }

    [Fact]
    public async Task GetCategoryVolumeAsync_WithUnknownCategory_ReturnsDefaultVolume()
    {
        // Act
        var volume = await _service.GetCategoryVolumeAsync("UnknownCategory");

        // Assert
        Assert.Equal(1.0f, volume); // Default volume
    }

    public void Dispose()
    {
        _service.DisposeAsync().AsTask().Wait();
    }
}