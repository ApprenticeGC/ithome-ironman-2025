using GameConsole.Plugins.Lifecycle;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Plugins.Lifecycle.Tests;

/// <summary>
/// Tests for PluginStateTracker functionality.
/// </summary>
public class PluginStateTrackerTests
{
    private readonly ILogger<PluginStateTracker> _logger;

    public PluginStateTrackerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { /* No console logging for tests */ });
        _logger = loggerFactory.CreateLogger<PluginStateTracker>();
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully()
    {
        // Act
        using var tracker = new PluginStateTracker(_logger);
        
        // Assert
        Assert.Empty(tracker.TrackedPlugins);
        Assert.Empty(tracker.UnhealthyPlugins);
    }

    [Fact]
    public void StartTracking_Should_Add_Plugin_To_Tracked_Plugins()
    {
        // Arrange
        using var tracker = new PluginStateTracker(_logger);
        var plugin = new MockPlugin("/test/plugin.dll");
        
        // Act
        tracker.StartTracking(plugin, PluginState.Loaded);
        
        // Assert
        Assert.Contains(plugin, tracker.TrackedPlugins);
        Assert.Equal(PluginState.Loaded, tracker.GetPluginState(plugin));
    }

    [Fact]
    public void StopTracking_Should_Remove_Plugin_From_Tracked_Plugins()
    {
        // Arrange
        using var tracker = new PluginStateTracker(_logger);
        var plugin = new MockPlugin("/test/plugin.dll");
        tracker.StartTracking(plugin, PluginState.Loaded);
        
        // Act
        tracker.StopTracking(plugin);
        
        // Assert
        Assert.DoesNotContain(plugin, tracker.TrackedPlugins);
        Assert.Null(tracker.GetPluginState(plugin));
    }

    [Fact]
    public void SetPluginState_Should_Update_Plugin_State()
    {
        // Arrange
        using var tracker = new PluginStateTracker(_logger);
        var plugin = new MockPlugin("/test/plugin.dll");
        tracker.StartTracking(plugin, PluginState.Loaded);
        
        // Act
        tracker.SetPluginState(plugin, PluginState.Running);
        
        // Assert
        Assert.Equal(PluginState.Running, tracker.GetPluginState(plugin));
    }

    [Fact]
    public void SetPluginState_Should_Fire_StateChanged_Event()
    {
        // Arrange
        using var tracker = new PluginStateTracker(_logger);
        var plugin = new MockPlugin("/test/plugin.dll");
        tracker.StartTracking(plugin, PluginState.Loaded);
        
        PluginStateChangedEventArgs? receivedArgs = null;
        tracker.StateChanged += (sender, args) => receivedArgs = args;
        
        // Act
        tracker.SetPluginState(plugin, PluginState.Running);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Same(plugin, receivedArgs.Plugin);
        Assert.Equal(PluginState.Loaded, receivedArgs.PreviousState);
        Assert.Equal(PluginState.Running, receivedArgs.NewState);
    }

    [Fact]
    public void GetPluginsInState_Should_Return_Correct_Plugins()
    {
        // Arrange
        using var tracker = new PluginStateTracker(_logger);
        var plugin1 = new MockPlugin("/test/plugin1.dll");
        var plugin2 = new MockPlugin("/test/plugin2.dll");
        
        tracker.StartTracking(plugin1, PluginState.Loaded);
        tracker.StartTracking(plugin2, PluginState.Running);
        
        // Act
        var loadedPlugins = tracker.GetPluginsInState(PluginState.Loaded);
        var runningPlugins = tracker.GetPluginsInState(PluginState.Running);
        
        // Assert
        Assert.Contains(plugin1, loadedPlugins);
        Assert.Contains(plugin2, runningPlugins);
        Assert.DoesNotContain(plugin1, runningPlugins);
        Assert.DoesNotContain(plugin2, loadedPlugins);
    }

    [Fact]
    public async Task PerformHealthCheckAsync_Should_Return_Health_Result()
    {
        // Arrange
        using var tracker = new PluginStateTracker(_logger);
        var plugin = new MockPlugin("/test/plugin.dll");
        tracker.StartTracking(plugin, PluginState.Running);
        
        // Act
        var result = await tracker.PerformHealthCheckAsync(plugin);
        
        // Assert
        Assert.NotNull(result);
        Assert.Same(plugin, result.Plugin);
        Assert.True(result.ResponseTime >= TimeSpan.Zero);
    }

    [Fact]
    public void SetPluginState_With_Exception_Should_Mark_Plugin_Unhealthy()
    {
        // Arrange
        using var tracker = new PluginStateTracker(_logger);
        var plugin = new MockPlugin("/test/plugin.dll");
        tracker.StartTracking(plugin, PluginState.Running);
        
        var exception = new InvalidOperationException("Test exception");
        
        // Act
        tracker.SetPluginState(plugin, PluginState.Failed, exception);
        
        // Assert
        Assert.Contains(plugin, tracker.UnhealthyPlugins);
    }
}