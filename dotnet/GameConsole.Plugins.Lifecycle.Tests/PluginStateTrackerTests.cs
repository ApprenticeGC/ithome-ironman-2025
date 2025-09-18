using GameConsole.Plugins.Lifecycle;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Plugins.Lifecycle.Tests;

/// <summary>
/// Tests for the plugin state tracker.
/// </summary>
public class PluginStateTrackerTests : IDisposable
{
    private readonly PluginStateTracker _stateTracker;
    private readonly PluginMetadata _testPlugin;

    public PluginStateTrackerTests()
    {
        var options = new PluginStateTrackerOptions(
            HealthCheckInterval: TimeSpan.FromMilliseconds(100),
            WatchdogTimeout: TimeSpan.FromMilliseconds(50),
            MaxFailureCount: 2
        );
        _stateTracker = new PluginStateTracker(options);
        
        _testPlugin = PluginMetadata.Create(
            "test-plugin",
            "Test Plugin",
            new Version(1, 0, 0),
            "/test/path.dll"
        );
    }

    [Fact]
    public void RegisterPlugin_Should_Add_Plugin_To_Tracking()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Loaded, DateTime.UtcNow);

        // Act
        _stateTracker.RegisterPlugin(loadedPlugin);

        // Assert
        var trackedPlugins = _stateTracker.GetTrackedPlugins();
        Assert.Single(trackedPlugins);
        Assert.Equal(_testPlugin.Id, trackedPlugins.First().Metadata.Id);
    }

    [Fact]
    public void UnregisterPlugin_Should_Remove_Plugin_From_Tracking()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Loaded, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);

        // Act
        _stateTracker.UnregisterPlugin(_testPlugin.Id);

        // Assert
        var trackedPlugins = _stateTracker.GetTrackedPlugins();
        Assert.Empty(trackedPlugins);
    }

    [Fact]
    public void UpdatePluginState_Should_Update_Plugin_State()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Loaded, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);

        // Act
        _stateTracker.UpdatePluginState(_testPlugin.Id, PluginState.Running);

        // Assert
        var plugin = _stateTracker.GetPluginState(_testPlugin.Id);
        Assert.NotNull(plugin);
        Assert.Equal(PluginState.Running, plugin.State);
    }

    [Fact]
    public void UpdatePluginHealth_Should_Update_Plugin_Health()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Running, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);

        // Act
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Healthy, "All good");

        // Assert
        var plugin = _stateTracker.GetPluginState(_testPlugin.Id);
        Assert.NotNull(plugin);
        Assert.Equal(PluginHealth.Healthy, plugin.Health);
        Assert.NotNull(plugin.LastHealthCheck);
    }

    [Fact]
    public void UpdatePluginHealth_Should_Fire_HealthChanged_Event()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Running, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);
        PluginHealthChangedEventArgs? capturedEvent = null;
        _stateTracker.PluginHealthChanged += (sender, args) => capturedEvent = args;

        // Act
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Healthy, "Test message");

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(_testPlugin.Id, capturedEvent.PluginId);
        Assert.Equal(PluginHealth.Unknown, capturedEvent.OldHealth);
        Assert.Equal(PluginHealth.Healthy, capturedEvent.NewHealth);
        Assert.Equal("Test message", capturedEvent.Message);
    }

    [Fact]
    public void UpdatePluginHealth_Should_Track_Failures()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Running, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);

        // Act
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Failed, "Error 1");
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Failed, "Error 2");

        // Assert
        var failureCount = _stateTracker.GetFailureCount(_testPlugin.Id);
        Assert.Equal(2, failureCount);
    }

    [Fact]
    public void UpdatePluginHealth_Should_Fire_FailureDetected_Event_After_Max_Failures()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Running, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);
        PluginFailureEventArgs? capturedEvent = null;
        _stateTracker.PluginFailureDetected += (sender, args) => capturedEvent = args;

        // Act
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Failed, "Error 1");
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Failed, "Error 2");

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(_testPlugin.Id, capturedEvent.PluginId);
        Assert.Equal(2, capturedEvent.FailureCount);
    }

    [Fact]
    public void UpdatePluginHealth_Should_Reset_Failure_Count_On_Recovery()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Running, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Failed, "Error");

        // Act
        _stateTracker.UpdatePluginHealth(_testPlugin.Id, PluginHealth.Healthy, "Recovered");

        // Assert
        var failureCount = _stateTracker.GetFailureCount(_testPlugin.Id);
        Assert.Equal(0, failureCount);
    }

    [Fact]
    public void GetPluginState_Should_Return_Null_For_Unknown_Plugin()
    {
        // Act
        var plugin = _stateTracker.GetPluginState("unknown-plugin");

        // Assert
        Assert.Null(plugin);
    }

    public void Dispose()
    {
        _stateTracker?.Dispose();
    }
}