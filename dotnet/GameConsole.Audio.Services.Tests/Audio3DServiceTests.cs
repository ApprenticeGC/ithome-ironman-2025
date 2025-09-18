using GameConsole.Audio.Services.Implementation;
using Microsoft.Extensions.Logging;
using Moq;
using System.Numerics;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

/// <summary>
/// Unit tests for Audio3DService.
/// </summary>
public class Audio3DServiceTests
{
    private readonly Mock<ILogger<Audio3DService>> _loggerMock;
    private readonly Audio3DService _service;

    public Audio3DServiceTests()
    {
        _loggerMock = new Mock<ILogger<Audio3DService>>();
        _service = new Audio3DService(_loggerMock.Object);
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
    public async Task SetListenerPositionAsync_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        var position = new Vector3(1, 0, 0);

        // Act & Assert
        await _service.SetListenerPositionAsync(position);
    }

    [Fact]
    public async Task SetListenerOrientationAsync_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        var forward = Vector3.UnitZ;
        var up = Vector3.UnitY;

        // Act & Assert
        await _service.SetListenerOrientationAsync(forward, up);
    }

    [Fact]
    public async Task Create3DSourceAsync_WithValidParameters_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        var sourceId = "test_source";
        var path = "test.wav";
        var position = Vector3.Zero;

        // Act
        var result = await _service.Create3DSourceAsync(sourceId, path, position);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Create3DSourceAsync_WithDuplicateId_ShouldReturnFalse()
    {
        // Arrange
        await _service.InitializeAsync();
        var sourceId = "test_source";
        var path = "test.wav";
        var position = Vector3.Zero;
        await _service.Create3DSourceAsync(sourceId, path, position);

        // Act
        var result = await _service.Create3DSourceAsync(sourceId, path, position);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateSourcePositionAsync_WithValidSource_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        var sourceId = "test_source";
        var path = "test.wav";
        var position = Vector3.Zero;
        var newPosition = new Vector3(5, 0, 0);
        await _service.Create3DSourceAsync(sourceId, path, position);

        // Act & Assert
        await _service.UpdateSourcePositionAsync(sourceId, newPosition);
    }

    [Fact]
    public async Task GetSourcesAsync_ShouldReturnSources()
    {
        // Arrange
        await _service.InitializeAsync();
        var sourceId = "test_source";
        var path = "test.wav";
        var position = Vector3.Zero;
        await _service.Create3DSourceAsync(sourceId, path, position);

        // Act
        var sources = await _service.GetSourcesAsync();

        // Assert
        Assert.Single(sources);
        var source = sources.First();
        Assert.Equal(sourceId, source.SourceId);
        Assert.Equal(path, source.AudioPath);
    }

    [Fact]
    public async Task Remove3DSourceAsync_WithValidSource_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();
        var sourceId = "test_source";
        var path = "test.wav";
        var position = Vector3.Zero;
        await _service.Create3DSourceAsync(sourceId, path, position);

        // Act
        await _service.Remove3DSourceAsync(sourceId);

        // Assert
        var sources = await _service.GetSourcesAsync();
        Assert.Empty(sources);
    }

    [Fact]
    public async Task SetSpeedOfSoundAsync_WithValidValue_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act & Assert
        await _service.SetSpeedOfSoundAsync(300.0f);
    }

    [Fact]
    public async Task SetSpeedOfSoundAsync_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SetSpeedOfSoundAsync(-100.0f));
    }

    [Fact]
    public async Task SetDopplerFactorAsync_WithValidValue_ShouldComplete()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act & Assert
        await _service.SetDopplerFactorAsync(0.5f);
    }

    [Fact]
    public async Task SetDopplerFactorAsync_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.SetDopplerFactorAsync(-0.5f));
    }

    [Fact]
    public async Task HasCapabilityAsync_WithSpatialAudioCapability_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var hasCapability = await _service.HasCapabilityAsync<ISpatialAudioCapability>();

        // Assert
        Assert.True(hasCapability);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ShouldReturnCapabilities()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var capabilities = await _service.GetCapabilitiesAsync();

        // Assert
        Assert.Contains(typeof(ISpatialAudioCapability), capabilities);
    }
}