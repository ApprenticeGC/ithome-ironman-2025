using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Audio.Core.Tests;

/// <summary>
/// Tests to verify that the audio service interfaces are properly defined and compilable.
/// These tests validate the interface contracts without requiring implementation.
/// </summary>
public class AudioServiceInterfaceTests
{
    [Fact]
    public void IAudioService_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var serviceType = typeof(IAudioService);
        
        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(serviceType));
    }

    [Fact]
    public void IAudioService_Should_Have_Core_Audio_Methods()
    {
        // Arrange
        var serviceType = typeof(IAudioService);
        
        // Act & Assert
        Assert.NotNull(serviceType.GetMethod("PlayAsync"));
        Assert.NotNull(serviceType.GetMethod("StopAsync"));
        Assert.NotNull(serviceType.GetMethod("PauseAsync"));
        Assert.NotNull(serviceType.GetMethod("ResumeAsync"));
        Assert.NotNull(serviceType.GetMethod("StopAllAsync"));
    }

    [Fact]
    public void IAudioService_Should_Have_Volume_Control_Methods()
    {
        // Arrange
        var serviceType = typeof(IAudioService);
        
        // Act & Assert
        Assert.NotNull(serviceType.GetMethod("SetMasterVolumeAsync"));
        Assert.NotNull(serviceType.GetMethod("GetMasterVolumeAsync"));
        Assert.NotNull(serviceType.GetMethod("SetCategoryVolumeAsync"));
        Assert.NotNull(serviceType.GetMethod("GetCategoryVolumeAsync"));
    }

    [Fact]
    public void IAudioService_Should_Have_Stream_Management_Methods()
    {
        // Arrange
        var serviceType = typeof(IAudioService);
        
        // Act & Assert
        Assert.NotNull(serviceType.GetMethod("CreateStreamAsync"));
        Assert.NotNull(serviceType.GetMethod("GetActiveStreamsAsync"));
    }

    [Fact]
    public void IAudioService_Should_Have_Format_Support_Methods()
    {
        // Arrange
        var serviceType = typeof(IAudioService);
        
        // Act & Assert
        Assert.NotNull(serviceType.GetMethod("GetSupportedFormatsAsync"));
        Assert.NotNull(serviceType.GetMethod("IsFormatSupportedAsync"));
    }

    [Fact]
    public void IAudioService_Should_Have_Device_Management_Methods()
    {
        // Arrange
        var serviceType = typeof(IAudioService);
        
        // Act & Assert
        Assert.NotNull(serviceType.GetMethod("SetActiveDeviceAsync"));
        Assert.NotNull(serviceType.GetMethod("GetActiveDeviceAsync"));
    }

    [Fact]
    public void AudioFormat_Should_Include_Required_Formats()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Wav));
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Mp3));
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Ogg));
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Unknown));
    }

    [Fact]
    public void IAudioDevice_Should_Have_Required_Properties()
    {
        // Arrange
        var deviceType = typeof(IAudioDevice);
        
        // Act & Assert
        Assert.NotNull(deviceType.GetProperty("Id"));
        Assert.NotNull(deviceType.GetProperty("Name"));
        Assert.NotNull(deviceType.GetProperty("IsAvailable"));
        Assert.NotNull(deviceType.GetProperty("IsDefault"));
        Assert.NotNull(deviceType.GetProperty("SupportedFormats"));
    }

    [Fact]
    public void IAudioDeviceEnumerator_Should_Support_Hot_Plugging()
    {
        // Arrange
        var enumeratorType = typeof(IAudioDeviceEnumerator);
        
        // Act & Assert
        Assert.NotNull(enumeratorType.GetMethod("GetAvailableDevicesAsync"));
        Assert.NotNull(enumeratorType.GetMethod("GetDefaultDeviceAsync"));
        Assert.NotNull(enumeratorType.GetMethod("GetDeviceByIdAsync"));
        Assert.NotNull(enumeratorType.GetEvent("DeviceChanged"));
    }

    [Fact]
    public void IAudioStream_Should_Support_Position_Tracking_And_Seeking()
    {
        // Arrange
        var streamType = typeof(IAudioStream);
        
        // Act & Assert
        Assert.NotNull(streamType.GetProperty("Position"));
        Assert.NotNull(streamType.GetProperty("Duration"));
        Assert.NotNull(streamType.GetProperty("CanSeek"));
        Assert.NotNull(streamType.GetMethod("SeekAsync"));
        Assert.NotNull(streamType.GetEvent("PositionChanged"));
    }

    [Fact]
    public void IAudioStream_Should_Inherit_From_IAsyncDisposable()
    {
        // Arrange & Act
        var streamType = typeof(IAudioStream);
        
        // Assert
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(streamType));
    }

    [Fact]
    public void All_Async_Methods_Should_Accept_CancellationToken()
    {
        // Arrange
        var serviceType = typeof(IAudioService);
        var asyncMethods = serviceType.GetMethods()
            .Where(m => m.Name.EndsWith("Async"))
            .ToList();
        
        // Act & Assert
        Assert.True(asyncMethods.Count > 0, "Should have async methods");
        
        foreach (var method in asyncMethods)
        {
            var parameters = method.GetParameters();
            var hasCancellationToken = parameters.Any(p => p.ParameterType == typeof(CancellationToken));
            Assert.True(hasCancellationToken, $"Method {method.Name} should accept CancellationToken");
        }
    }
}