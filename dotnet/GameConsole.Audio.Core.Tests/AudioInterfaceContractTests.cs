using GameConsole.Audio.Services;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Audio.Core.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.Audio.Core.
/// </summary>
public class AudioInterfaceContractTests
{
    [Fact]
    public void IService_Should_Inherit_From_Base_IService()
    {
        // Arrange
        var audioServiceType = typeof(GameConsole.Audio.Services.IService);
        var baseServiceType = typeof(GameConsole.Core.Abstractions.IService);

        // Act & Assert
        Assert.True(baseServiceType.IsAssignableFrom(audioServiceType));
        Assert.True(audioServiceType.IsInterface);
    }

    [Fact]
    public void IService_Should_Have_PlayAsync_Method()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act
        var playMethod = serviceType.GetMethod("PlayAsync");

        // Assert
        Assert.NotNull(playMethod);
        Assert.Equal(typeof(Task<bool>), playMethod.ReturnType);
        
        var parameters = playMethod.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("path", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal("category", parameters[1].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.Equal("cancellationToken", parameters[2].Name);
    }

    [Fact]
    public void IService_Should_Have_StopAsync_Method()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act
        var stopMethod = serviceType.GetMethod("StopAsync", new[] { typeof(string), typeof(CancellationToken) });

        // Assert
        Assert.NotNull(stopMethod);
        Assert.Equal(typeof(Task), stopMethod.ReturnType);
        
        var parameters = stopMethod.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("path", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("cancellationToken", parameters[1].Name);
    }

    [Fact]
    public void IService_Should_Have_StopAllAsync_Method()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act
        var stopAllMethod = serviceType.GetMethod("StopAllAsync");

        // Assert
        Assert.NotNull(stopAllMethod);
        Assert.Equal(typeof(Task), stopAllMethod.ReturnType);
        
        var parameters = stopAllMethod.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.Equal("cancellationToken", parameters[0].Name);
    }

    [Fact]
    public void IService_Should_Have_Volume_Management_Methods()
    {
        // Arrange
        var serviceType = typeof(GameConsole.Audio.Services.IService);

        // Act & Assert - SetMasterVolumeAsync
        var setMasterVolumeMethod = serviceType.GetMethod("SetMasterVolumeAsync");
        Assert.NotNull(setMasterVolumeMethod);
        Assert.Equal(typeof(Task), setMasterVolumeMethod.ReturnType);

        // Act & Assert - SetCategoryVolumeAsync
        var setCategoryVolumeMethod = serviceType.GetMethod("SetCategoryVolumeAsync");
        Assert.NotNull(setCategoryVolumeMethod);
        Assert.Equal(typeof(Task), setCategoryVolumeMethod.ReturnType);

        // Act & Assert - GetCategoryVolumeAsync
        var getCategoryVolumeMethod = serviceType.GetMethod("GetCategoryVolumeAsync");
        Assert.NotNull(getCategoryVolumeMethod);
        Assert.Equal(typeof(Task<float>), getCategoryVolumeMethod.ReturnType);
    }

    [Fact]
    public void ISpatialAudioCapability_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange
        var spatialAudioType = typeof(ISpatialAudioCapability);
        var capabilityProviderType = typeof(ICapabilityProvider);

        // Act & Assert
        Assert.True(capabilityProviderType.IsAssignableFrom(spatialAudioType));
        Assert.True(spatialAudioType.IsInterface);
    }

    [Fact]
    public void ISpatialAudioCapability_Should_Have_Listener_Methods()
    {
        // Arrange
        var capabilityType = typeof(ISpatialAudioCapability);

        // Act & Assert - SetListenerPositionAsync
        var setListenerPositionMethod = capabilityType.GetMethod("SetListenerPositionAsync");
        Assert.NotNull(setListenerPositionMethod);
        Assert.Equal(typeof(Task), setListenerPositionMethod.ReturnType);

        // Act & Assert - SetListenerOrientationAsync
        var setListenerOrientationMethod = capabilityType.GetMethod("SetListenerOrientationAsync");
        Assert.NotNull(setListenerOrientationMethod);
        Assert.Equal(typeof(Task), setListenerOrientationMethod.ReturnType);

        // Act & Assert - PlayAtPositionAsync
        var playAtPositionMethod = capabilityType.GetMethod("PlayAtPositionAsync");
        Assert.NotNull(playAtPositionMethod);
        Assert.Equal(typeof(Task<bool>), playAtPositionMethod.ReturnType);
    }

    [Fact]
    public void IAudioEffectsCapability_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange
        var effectsType = typeof(IAudioEffectsCapability);
        var capabilityProviderType = typeof(ICapabilityProvider);

        // Act & Assert
        Assert.True(capabilityProviderType.IsAssignableFrom(effectsType));
        Assert.True(effectsType.IsInterface);
    }

    [Fact]
    public void IAudioEffectsCapability_Should_Have_Effect_Methods()
    {
        // Arrange
        var capabilityType = typeof(IAudioEffectsCapability);

        // Act & Assert - ApplyLowPassFilterAsync
        var applyLowPassMethod = capabilityType.GetMethod("ApplyLowPassFilterAsync");
        Assert.NotNull(applyLowPassMethod);
        Assert.Equal(typeof(Task), applyLowPassMethod.ReturnType);

        // Act & Assert - ApplyReverbAsync
        var applyReverbMethod = capabilityType.GetMethod("ApplyReverbAsync");
        Assert.NotNull(applyReverbMethod);
        Assert.Equal(typeof(Task), applyReverbMethod.ReturnType);

        // Act & Assert - ClearEffectsAsync
        var clearEffectsMethod = capabilityType.GetMethod("ClearEffectsAsync");
        Assert.NotNull(clearEffectsMethod);
        Assert.Equal(typeof(Task), clearEffectsMethod.ReturnType);
    }

    [Fact]
    public void Vector3_Should_Have_Required_Properties()
    {
        // Arrange
        var vector3Type = typeof(Vector3);

        // Act & Assert
        var xProperty = vector3Type.GetProperty("X");
        Assert.NotNull(xProperty);
        Assert.Equal(typeof(float), xProperty.PropertyType);
        Assert.True(xProperty.CanRead);

        var yProperty = vector3Type.GetProperty("Y");
        Assert.NotNull(yProperty);
        Assert.Equal(typeof(float), yProperty.PropertyType);
        Assert.True(yProperty.CanRead);

        var zProperty = vector3Type.GetProperty("Z");
        Assert.NotNull(zProperty);
        Assert.Equal(typeof(float), zProperty.PropertyType);
        Assert.True(zProperty.CanRead);
    }

    [Fact]
    public void Vector3_Should_Have_Static_Helper_Vectors()
    {
        // Arrange & Act
        var zero = Vector3.Zero;
        var forward = Vector3.Forward;
        var up = Vector3.Up;

        // Assert
        Assert.Equal(0f, zero.X);
        Assert.Equal(0f, zero.Y);
        Assert.Equal(0f, zero.Z);

        Assert.Equal(0f, forward.X);
        Assert.Equal(0f, forward.Y);
        Assert.Equal(1f, forward.Z);

        Assert.Equal(0f, up.X);
        Assert.Equal(1f, up.Y);
        Assert.Equal(0f, up.Z);
    }

    [Fact]
    public void Vector3_Constructor_Should_Set_Properties()
    {
        // Arrange
        const float x = 1.5f;
        const float y = 2.3f;
        const float z = 4.7f;

        // Act
        var vector = new Vector3(x, y, z);

        // Assert
        Assert.Equal(x, vector.X);
        Assert.Equal(y, vector.Y);
        Assert.Equal(z, vector.Z);
    }

    [Fact]
    public void Vector3_ToString_Should_Return_Formatted_String()
    {
        // Arrange
        var vector = new Vector3(1.5f, 2.3f, 4.7f);

        // Act
        var result = vector.ToString();

        // Assert
        Assert.Equal("(1.5, 2.3, 4.7)", result);
    }
}