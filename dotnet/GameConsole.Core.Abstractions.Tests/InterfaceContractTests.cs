using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.Core.Abstractions.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IService_Should_Inherit_From_IAsyncDisposable()
    {
        // Arrange & Act
        var serviceType = typeof(IService);

        // Assert
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(serviceType));
    }

    [Fact]
    public void IService_Should_Have_Required_Methods()
    {
        // Arrange
        var serviceType = typeof(IService);

        // Act & Assert - Check for required methods
        var initializeMethod = serviceType.GetMethod("InitializeAsync");
        Assert.NotNull(initializeMethod);
        Assert.Equal(typeof(Task), initializeMethod.ReturnType);

        var startMethod = serviceType.GetMethod("StartAsync");
        Assert.NotNull(startMethod);
        Assert.Equal(typeof(Task), startMethod.ReturnType);

        var stopMethod = serviceType.GetMethod("StopAsync");
        Assert.NotNull(stopMethod);
        Assert.Equal(typeof(Task), stopMethod.ReturnType);

        var isRunningProperty = serviceType.GetProperty("IsRunning");
        Assert.NotNull(isRunningProperty);
        Assert.Equal(typeof(bool), isRunningProperty.PropertyType);
        Assert.True(isRunningProperty.CanRead);
    }

    [Fact]
    public void ICapabilityProvider_Should_Have_Required_Methods()
    {
        // Arrange
        var capabilityProviderType = typeof(ICapabilityProvider);

        // Act & Assert
        var getCapabilitiesMethod = capabilityProviderType.GetMethod("GetCapabilitiesAsync");
        Assert.NotNull(getCapabilitiesMethod);
        Assert.Equal(typeof(Task<IEnumerable<Type>>), getCapabilitiesMethod.ReturnType);

        var hasCapabilityMethods = capabilityProviderType.GetMethods()
            .Where(m => m.Name == "HasCapabilityAsync" && m.IsGenericMethod)
            .ToArray();
        Assert.Single(hasCapabilityMethods);

        var getCapabilityMethods = capabilityProviderType.GetMethods()
            .Where(m => m.Name == "GetCapabilityAsync" && m.IsGenericMethod)
            .ToArray();
        Assert.Single(getCapabilityMethods);
    }

    [Fact]
    public void IServiceMetadata_Should_Have_Required_Properties()
    {
        // Arrange
        var metadataType = typeof(IServiceMetadata);

        // Act & Assert
        var nameProperty = metadataType.GetProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.Equal(typeof(string), nameProperty.PropertyType);
        Assert.True(nameProperty.CanRead);

        var versionProperty = metadataType.GetProperty("Version");
        Assert.NotNull(versionProperty);
        Assert.Equal(typeof(string), versionProperty.PropertyType);
        Assert.True(versionProperty.CanRead);

        var descriptionProperty = metadataType.GetProperty("Description");
        Assert.NotNull(descriptionProperty);
        Assert.Equal(typeof(string), descriptionProperty.PropertyType);
        Assert.True(descriptionProperty.CanRead);

        var categoriesProperty = metadataType.GetProperty("Categories");
        Assert.NotNull(categoriesProperty);
        Assert.Equal(typeof(IEnumerable<string>), categoriesProperty.PropertyType);
        Assert.True(categoriesProperty.CanRead);

        var propertiesProperty = metadataType.GetProperty("Properties");
        Assert.NotNull(propertiesProperty);
        Assert.Equal(typeof(IReadOnlyDictionary<string, object>), propertiesProperty.PropertyType);
        Assert.True(propertiesProperty.CanRead);
    }
}
