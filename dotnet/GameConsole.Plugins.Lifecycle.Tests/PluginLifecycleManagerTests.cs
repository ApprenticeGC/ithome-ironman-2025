using GameConsole.Plugins.Lifecycle;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Plugins.Lifecycle.Tests;

/// <summary>
/// Tests for PluginLifecycleManager functionality.
/// </summary>
public class PluginLifecycleManagerTests
{
    private readonly ILogger<PluginLifecycleManager> _logger;
    private readonly ILogger<PluginStateTracker> _stateTrackerLogger;
    private readonly ILogger<PluginRecoveryService> _recoveryLogger;
    private readonly ILogger<PluginUpdateManager> _updateLogger;

    public PluginLifecycleManagerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { /* No console logging for tests */ });
        _logger = loggerFactory.CreateLogger<PluginLifecycleManager>();
        _stateTrackerLogger = loggerFactory.CreateLogger<PluginStateTracker>();
        _recoveryLogger = loggerFactory.CreateLogger<PluginRecoveryService>();
        _updateLogger = loggerFactory.CreateLogger<PluginUpdateManager>();
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        // Act
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        
        // Assert
        Assert.Empty(lifecycleManager.ManagedPlugins);
        Assert.False(lifecycleManager.IsRunning);
    }

    [Fact]
    public async Task InitializeAsync_Should_Set_Service_As_Initialized()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        
        // Act
        await lifecycleManager.InitializeAsync();
        
        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task StartAsync_Should_Set_IsRunning_To_True()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        
        // Act
        await lifecycleManager.StartAsync();
        
        // Assert
        Assert.True(lifecycleManager.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Set_IsRunning_To_False()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        await lifecycleManager.StartAsync();
        
        // Act
        await lifecycleManager.StopAsync();
        
        // Assert
        Assert.False(lifecycleManager.IsRunning);
    }

    [Fact]
    public void GetPluginState_Should_Return_Current_Plugin_State()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        var plugin = new MockPlugin("/test/plugin.dll");
        stateTracker.StartTracking(plugin, PluginState.Loaded);
        
        // Act
        var state = lifecycleManager.GetPluginState(plugin);
        
        // Assert
        Assert.Equal(PluginState.Loaded, state);
    }

    [Fact]
    public async Task CheckPluginHealthAsync_Should_Return_Health_Result()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        var plugin = new MockPlugin("/test/plugin.dll");
        stateTracker.StartTracking(plugin, PluginState.Running);
        
        // Act
        var result = await lifecycleManager.CheckPluginHealthAsync(plugin);
        
        // Assert
        Assert.NotNull(result);
        Assert.Same(plugin, result.Plugin);
    }

    [Fact]
    public void GetPluginDependents_Should_Return_Empty_For_Mock_Test()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        
        var basePlugin = new MockPlugin("/test/base.dll");
        
        // Act
        var dependents = lifecycleManager.GetPluginDependents(basePlugin);
        
        // Assert
        Assert.Empty(dependents); // Empty for basic mock test
    }

    [Fact]
    public void GetPluginDependencies_Should_Return_Empty_For_Mock_Test()
    {
        // Arrange
        var stateTracker = new PluginStateTracker(_stateTrackerLogger);
        var recoveryService = new PluginRecoveryService(_recoveryLogger, stateTracker);
        var updateManager = new PluginUpdateManager(_updateLogger, stateTracker);
        
        using var lifecycleManager = new PluginLifecycleManager(_logger, stateTracker, recoveryService, updateManager);
        
        var plugin = new MockPlugin("/test/plugin.dll");
        
        // Act
        var dependencies = lifecycleManager.GetPluginDependencies(plugin);
        
        // Assert
        Assert.Empty(dependencies); // Empty for basic mock test
    }


}