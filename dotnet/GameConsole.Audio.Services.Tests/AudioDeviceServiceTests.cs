using GameConsole.Audio.Services.Implementation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.Audio.Services.Tests;

/// <summary>
/// Unit tests for AudioDeviceService.
/// </summary>
public class AudioDeviceServiceTests
{
    private readonly Mock<ILogger<AudioDeviceService>> _loggerMock;
    private readonly AudioDeviceService _service;

    public AudioDeviceServiceTests()
    {
        _loggerMock = new Mock<ILogger<AudioDeviceService>>();
        _service = new AudioDeviceService(_loggerMock.Object);
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
    public async Task GetAvailableDevicesAsync_ShouldReturnDevices()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var devices = await _service.GetAvailableDevicesAsync();

        // Assert
        Assert.NotEmpty(devices);
        Assert.Contains(devices, d => d.IsDefault);
    }

    [Fact]
    public async Task GetCurrentDeviceAsync_ShouldReturnDevice()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var device = await _service.GetCurrentDeviceAsync();

        // Assert
        Assert.NotNull(device);
    }

    [Fact]
    public async Task GetDefaultDeviceAsync_ShouldReturnDefaultDevice()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var device = await _service.GetDefaultDeviceAsync();

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsDefault);
    }

    [Fact]
    public async Task TestDeviceAsync_WithValidDevice_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();
        var devices = await _service.GetAvailableDevicesAsync();
        var firstDevice = devices.First();

        // Act
        var result = await _service.TestDeviceAsync(firstDevice.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TestDeviceAsync_WithInvalidDevice_ShouldReturnFalse()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var result = await _service.TestDeviceAsync("nonexistent_device");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_WithValidDevice_ShouldReturnInfo()
    {
        // Arrange
        await _service.InitializeAsync();
        var devices = await _service.GetAvailableDevicesAsync();
        var firstDevice = devices.First();

        // Act
        var info = await _service.GetDeviceInfoAsync(firstDevice.Id);

        // Assert
        Assert.NotNull(info);
        Assert.Equal(firstDevice.Id, info.Id);
        Assert.Equal(firstDevice.Name, info.Name);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_WithInvalidDevice_ShouldReturnNull()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var info = await _service.GetDeviceInfoAsync("nonexistent_device");

        // Assert
        Assert.Null(info);
    }

    [Fact]
    public async Task HasCapabilityAsync_WithAudioDeviceCapability_ShouldReturnTrue()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        var hasCapability = await _service.HasCapabilityAsync<IAudioDeviceCapability>();

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
        Assert.Contains(typeof(IAudioDeviceCapability), capabilities);
    }
}