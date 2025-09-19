using GameConsole.Audio.Core;
using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using System.Numerics;
using System.Reactive;
using Xunit;

namespace GameConsole.Audio.Core.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.Audio.Core.
/// </summary>
public class AudioInterfaceContractTests
{
    [Fact]
    public void AudioService_Should_Inherit_From_Base_IService()
    {
        // Arrange & Act
        var audioServiceType = typeof(GameConsole.Audio.Services.IService);

        // Assert
        Assert.True(typeof(GameConsole.Core.Abstractions.IService).IsAssignableFrom(audioServiceType));
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(audioServiceType));
    }

    [Fact]
    public void AudioService_Should_Have_Core_Audio_Operations()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act & Assert - Check for PlayAsync
        var playMethod = serviceType.GetMethod("PlayAsync");
        Assert.NotNull(playMethod);
        Assert.Equal(typeof(Task<string?>), playMethod.ReturnType);

        // Check for StopAsync
        var stopMethod = serviceType.GetMethod("StopAsync", new[] { typeof(string), typeof(TimeSpan?), typeof(CancellationToken) });
        Assert.NotNull(stopMethod);
        Assert.Equal(typeof(Task<bool>), stopMethod.ReturnType);

        // Check for StopAllAsync
        var stopAllMethod = serviceType.GetMethod("StopAllAsync");
        Assert.NotNull(stopAllMethod);
        Assert.Equal(typeof(Task<int>), stopAllMethod.ReturnType);

        // Check for PauseAsync
        var pauseMethod = serviceType.GetMethod("PauseAsync");
        Assert.NotNull(pauseMethod);
        Assert.Equal(typeof(Task<bool>), pauseMethod.ReturnType);

        // Check for ResumeAsync
        var resumeMethod = serviceType.GetMethod("ResumeAsync");
        Assert.NotNull(resumeMethod);
        Assert.Equal(typeof(Task<bool>), resumeMethod.ReturnType);
    }

    [Fact]
    public void AudioService_Should_Have_Volume_Management()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act & Assert - Check for volume methods
        var setMasterVolumeMethod = serviceType.GetMethod("SetMasterVolumeAsync");
        Assert.NotNull(setMasterVolumeMethod);
        Assert.Equal(typeof(Task), setMasterVolumeMethod.ReturnType);

        var getMasterVolumeMethod = serviceType.GetMethod("GetMasterVolumeAsync");
        Assert.NotNull(getMasterVolumeMethod);
        Assert.Equal(typeof(Task<float>), getMasterVolumeMethod.ReturnType);

        var setCategoryVolumeMethod = serviceType.GetMethod("SetCategoryVolumeAsync");
        Assert.NotNull(setCategoryVolumeMethod);
        Assert.Equal(typeof(Task), setCategoryVolumeMethod.ReturnType);

        var getCategoryVolumeMethod = serviceType.GetMethod("GetCategoryVolumeAsync");
        Assert.NotNull(getCategoryVolumeMethod);
        Assert.Equal(typeof(Task<float>), getCategoryVolumeMethod.ReturnType);

        var setSourceVolumeMethod = serviceType.GetMethod("SetSourceVolumeAsync");
        Assert.NotNull(setSourceVolumeMethod);
        Assert.Equal(typeof(Task<bool>), setSourceVolumeMethod.ReturnType);
    }

    [Fact]
    public void AudioService_Should_Have_State_Query_Methods()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act & Assert
        var getSourceStateMethod = serviceType.GetMethod("GetSourceStateAsync");
        Assert.NotNull(getSourceStateMethod);
        Assert.Equal(typeof(Task<AudioState?>), getSourceStateMethod.ReturnType);

        var getAudioMetadataMethod = serviceType.GetMethod("GetAudioMetadataAsync");
        Assert.NotNull(getAudioMetadataMethod);
        Assert.Equal(typeof(Task<AudioMetadata?>), getAudioMetadataMethod.ReturnType);

        var getActiveSourcesMethod = serviceType.GetMethod("GetActiveSourcesAsync");
        Assert.NotNull(getActiveSourcesMethod);
        Assert.Equal(typeof(Task<IReadOnlyList<string>>), getActiveSourcesMethod.ReturnType);
    }

    [Fact]
    public void AudioService_Should_Have_Event_Streams()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act & Assert
        var audioEventsProperty = serviceType.GetProperty("AudioEvents");
        Assert.NotNull(audioEventsProperty);
        Assert.Equal(typeof(IObservable<IAudioEvent>), audioEventsProperty.PropertyType);

        var stateChangesProperty = serviceType.GetProperty("StateChanges");
        Assert.NotNull(stateChangesProperty);
        Assert.Equal(typeof(IObservable<AudioStateChangedEvent>), stateChangesProperty.PropertyType);

        var volumeChangesProperty = serviceType.GetProperty("VolumeChanges");
        Assert.NotNull(volumeChangesProperty);
        Assert.Equal(typeof(IObservable<AudioVolumeChangedEvent>), volumeChangesProperty.PropertyType);
    }

    [Fact]
    public void SpatialAudioCapability_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange & Act
        var spatialAudioType = typeof(ISpatialAudioCapability);

        // Assert
        Assert.True(typeof(ICapabilityProvider).IsAssignableFrom(spatialAudioType));
    }

    [Fact]
    public void SpatialAudioCapability_Should_Have_Required_Methods()
    {
        // Arrange
        var spatialAudioType = typeof(ISpatialAudioCapability);

        // Act & Assert
        var setListenerTransformMethod = spatialAudioType.GetMethod("SetListenerTransformAsync");
        Assert.NotNull(setListenerTransformMethod);
        Assert.Equal(typeof(Task), setListenerTransformMethod.ReturnType);

        var setSourcePositionMethod = spatialAudioType.GetMethod("SetSourcePositionAsync");
        Assert.NotNull(setSourcePositionMethod);
        Assert.Equal(typeof(Task<bool>), setSourcePositionMethod.ReturnType);

        var setEnvironmentMethod = spatialAudioType.GetMethod("SetEnvironmentAsync");
        Assert.NotNull(setEnvironmentMethod);
        Assert.Equal(typeof(Task), setEnvironmentMethod.ReturnType);

        var getListenerTransformMethod = spatialAudioType.GetMethod("GetListenerTransformAsync");
        Assert.NotNull(getListenerTransformMethod);
        Assert.Equal(typeof(Task<(Vector3 position, Vector3 forward, Vector3 up)>), getListenerTransformMethod.ReturnType);
    }

    [Fact]
    public void VolumeControlCapability_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange & Act
        var volumeControlType = typeof(IVolumeControlCapability);

        // Assert
        Assert.True(typeof(ICapabilityProvider).IsAssignableFrom(volumeControlType));
    }

    [Fact]
    public void VolumeControlCapability_Should_Have_Required_Methods()
    {
        // Arrange
        var volumeControlType = typeof(IVolumeControlCapability);

        // Act & Assert
        var fadeVolumeMethod = volumeControlType.GetMethod("FadeVolumeAsync");
        Assert.NotNull(fadeVolumeMethod);
        Assert.Equal(typeof(Task<bool>), fadeVolumeMethod.ReturnType);

        var createSnapshotMethod = volumeControlType.GetMethod("CreateVolumeSnapshotAsync");
        Assert.NotNull(createSnapshotMethod);
        Assert.Equal(typeof(Task<string>), createSnapshotMethod.ReturnType);

        var restoreSnapshotMethod = volumeControlType.GetMethod("RestoreVolumeSnapshotAsync");
        Assert.NotNull(restoreSnapshotMethod);
        Assert.Equal(typeof(Task<bool>), restoreSnapshotMethod.ReturnType);

        var setCompressionMethod = volumeControlType.GetMethod("SetCompressionAsync");
        Assert.NotNull(setCompressionMethod);
        Assert.Equal(typeof(Task), setCompressionMethod.ReturnType);
    }
}