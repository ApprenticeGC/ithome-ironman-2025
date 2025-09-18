using GameConsole.Plugins.Core;
using Xunit;

namespace GameConsole.Plugins.Core.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.Plugins.Core.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IPlugin_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var pluginType = typeof(IPlugin);
        
        // Assert
        Assert.True(typeof(GameConsole.Core.Abstractions.IService).IsAssignableFrom(pluginType));
    }

    [Fact]
    public void IPlugin_Should_Have_Required_Properties()
    {
        // Arrange
        var pluginType = typeof(IPlugin);
        
        // Act & Assert - Check for required properties
        var metadataProperty = pluginType.GetProperty("Metadata");
        Assert.NotNull(metadataProperty);
        Assert.Equal(typeof(IPluginMetadata), metadataProperty.PropertyType);
        Assert.True(metadataProperty.CanRead);
        
        var contextProperty = pluginType.GetProperty("Context");
        Assert.NotNull(contextProperty);
        Assert.Equal(typeof(IPluginContext), contextProperty.PropertyType);
        Assert.True(contextProperty.CanRead);
        Assert.True(contextProperty.CanWrite);
    }

    [Fact]
    public void IPlugin_Should_Have_Required_Methods()
    {
        // Arrange
        var pluginType = typeof(IPlugin);
        
        // Act & Assert - Check for required methods
        var configureMethod = pluginType.GetMethod("ConfigureAsync");
        Assert.NotNull(configureMethod);
        Assert.Equal(typeof(Task), configureMethod.ReturnType);
        
        var canUnloadMethod = pluginType.GetMethod("CanUnloadAsync");
        Assert.NotNull(canUnloadMethod);
        Assert.Equal(typeof(Task<bool>), canUnloadMethod.ReturnType);
        
        var prepareUnloadMethod = pluginType.GetMethod("PrepareUnloadAsync");
        Assert.NotNull(prepareUnloadMethod);
        Assert.Equal(typeof(Task), prepareUnloadMethod.ReturnType);
    }

    [Fact]
    public void IPluginMetadata_Should_Have_Required_Properties()
    {
        // Arrange
        var metadataType = typeof(IPluginMetadata);
        
        // Act & Assert - Check for required properties
        var idProperty = metadataType.GetProperty("Id");
        Assert.NotNull(idProperty);
        Assert.Equal(typeof(string), idProperty.PropertyType);
        Assert.True(idProperty.CanRead);
        
        var nameProperty = metadataType.GetProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.Equal(typeof(string), nameProperty.PropertyType);
        Assert.True(nameProperty.CanRead);
        
        var versionProperty = metadataType.GetProperty("Version");
        Assert.NotNull(versionProperty);
        Assert.Equal(typeof(Version), versionProperty.PropertyType);
        Assert.True(versionProperty.CanRead);
        
        var descriptionProperty = metadataType.GetProperty("Description");
        Assert.NotNull(descriptionProperty);
        Assert.Equal(typeof(string), descriptionProperty.PropertyType);
        Assert.True(descriptionProperty.CanRead);
        
        var authorProperty = metadataType.GetProperty("Author");
        Assert.NotNull(authorProperty);
        Assert.Equal(typeof(string), authorProperty.PropertyType);
        Assert.True(authorProperty.CanRead);
        
        var dependenciesProperty = metadataType.GetProperty("Dependencies");
        Assert.NotNull(dependenciesProperty);
        Assert.Equal(typeof(IReadOnlyList<string>), dependenciesProperty.PropertyType);
        Assert.True(dependenciesProperty.CanRead);
        
        var propertiesProperty = metadataType.GetProperty("Properties");
        Assert.NotNull(propertiesProperty);
        Assert.Equal(typeof(IReadOnlyDictionary<string, object>), propertiesProperty.PropertyType);
        Assert.True(propertiesProperty.CanRead);
    }

    [Fact]
    public void IPluginContext_Should_Have_Required_Properties()
    {
        // Arrange
        var contextType = typeof(IPluginContext);
        
        // Act & Assert - Check for required properties
        var servicesProperty = contextType.GetProperty("Services");
        Assert.NotNull(servicesProperty);
        Assert.Equal(typeof(IServiceProvider), servicesProperty.PropertyType);
        Assert.True(servicesProperty.CanRead);
        
        var configurationProperty = contextType.GetProperty("Configuration");
        Assert.NotNull(configurationProperty);
        Assert.Equal(typeof(Microsoft.Extensions.Configuration.IConfiguration), configurationProperty.PropertyType);
        Assert.True(configurationProperty.CanRead);
        
        var pluginDirectoryProperty = contextType.GetProperty("PluginDirectory");
        Assert.NotNull(pluginDirectoryProperty);
        Assert.Equal(typeof(string), pluginDirectoryProperty.PropertyType);
        Assert.True(pluginDirectoryProperty.CanRead);
        
        var shutdownTokenProperty = contextType.GetProperty("ShutdownToken");
        Assert.NotNull(shutdownTokenProperty);
        Assert.Equal(typeof(CancellationToken), shutdownTokenProperty.PropertyType);
        Assert.True(shutdownTokenProperty.CanRead);
        
        var propertiesProperty = contextType.GetProperty("Properties");
        Assert.NotNull(propertiesProperty);
        Assert.Equal(typeof(IReadOnlyDictionary<string, object>), propertiesProperty.PropertyType);
        Assert.True(propertiesProperty.CanRead);
    }

    [Fact]
    public void IPluginLifecycleEvents_Should_Have_Required_Events()
    {
        // Arrange
        var eventsType = typeof(IPluginLifecycleEvents);
        
        // Act & Assert - Check for required events
        var events = new[]
        {
            "PluginConfiguring", "PluginConfigured",
            "PluginInitializing", "PluginInitialized",
            "PluginStarting", "PluginStarted",
            "PluginStopping", "PluginStopped",
            "PluginUnloading", "PluginUnloaded",
            "PluginError"
        };

        foreach (var eventName in events)
        {
            var eventInfo = eventsType.GetEvent(eventName);
            Assert.NotNull(eventInfo);
            Assert.Equal(typeof(EventHandler<PluginLifecycleEventArgs>), eventInfo.EventHandlerType);
        }
    }
}