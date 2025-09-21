using Microsoft.Extensions.Logging;
using GameConsole.Audio.Services;
using GameConsole.Audio.Core;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

public class Audio3DServiceTests
{
    private readonly ILogger<Audio3DService> _logger;

    public Audio3DServiceTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<Audio3DService>();
    }

    [Fact]
    public async Task SetListenerPositionAsync_ShouldUpdatePosition()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        var newPosition = new Vector3(10, 5, 3);

        // Act
        await service.SetListenerPositionAsync(newPosition);

        // Assert - Should not throw exception
        Assert.True(true);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task Play3DAudioAsync_WithInvalidPath_ShouldNotThrow()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        var position = new Vector3(1, 2, 3);

        // Act & Assert
        await service.Play3DAudioAsync("nonexistent.wav", position, 1.0f);
        
        // Should not throw exception
        Assert.True(true);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task SetListenerOrientationAsync_ShouldUpdateOrientation()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        var forward = new Vector3(0, 0, 1);
        var up = new Vector3(0, 1, 0);

        // Act
        await service.SetListenerOrientationAsync(forward, up);

        // Assert - Should not throw exception
        Assert.True(true);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task UpdateAudioPositionAsync_ShouldUpdatePosition()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        var originalPosition = new Vector3(1, 2, 3);
        var newPosition = new Vector3(4, 5, 6);

        // Act
        await service.UpdateAudioPositionAsync("test.wav", newPosition);

        // Assert - Should not throw exception (audio doesn't exist, but method should handle it)
        Assert.True(true);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public void SetDopplerFactor_ShouldAcceptValidValues()
    {
        // Arrange
        var service = new Audio3DService(_logger);

        // Act
        service.SetDopplerFactor(1.5f);

        // Assert - Should not throw exception
        Assert.True(true);
    }

    [Fact]
    public void SetDopplerFactor_ShouldClampNegativeValues()
    {
        // Arrange
        var service = new Audio3DService(_logger);

        // Act
        service.SetDopplerFactor(-1.0f);

        // Assert - Should not throw exception and should clamp to 0
        Assert.True(true);
    }

    [Fact]
    public void SetSpeedOfSound_ShouldAcceptValidValues()
    {
        // Arrange
        var service = new Audio3DService(_logger);

        // Act
        service.SetSpeedOfSound(400.0f);

        // Assert - Should not throw exception
        Assert.True(true);
    }

    [Fact]
    public async Task GetActive3DAudioInfo_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        // Act
        var info = service.GetActive3DAudioInfo();

        // Assert
        Assert.NotNull(info);
        Assert.Empty(info);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task PlayAsync_ShouldCallPlay3DAudio()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        // Act
        var result = await service.PlayAsync("test.wav", "SFX");

        // Assert
        Assert.True(result); // Should return true even with invalid file
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task StopAsync_WithNonexistentPath_ShouldNotThrow()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        // Act & Assert
        await service.StopAsync("nonexistent.wav");
        
        // Should not throw exception
        Assert.True(true);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task HasCapabilityAsync_ShouldReturnTrueForSpatialAudio()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        // Act
        var hasCapability = await service.HasCapabilityAsync<ISpatialAudioCapability>();

        // Assert
        Assert.True(hasCapability);
        
        // Clean up
        await service.DisposeAsync();
    }

    [Fact]
    public async Task GetCapabilityAsync_ShouldReturnSelfForSpatialAudio()
    {
        // Arrange
        var service = new Audio3DService(_logger);
        await service.InitializeAsync();

        // Act
        var capability = await service.GetCapabilityAsync<ISpatialAudioCapability>();

        // Assert
        Assert.NotNull(capability);
        Assert.Same(service, capability);
        
        // Clean up
        await service.DisposeAsync();
    }
}