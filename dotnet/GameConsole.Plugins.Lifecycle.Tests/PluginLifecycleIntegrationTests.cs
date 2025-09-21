using GameConsole.Plugins.Core;
using GameConsole.Plugins.Lifecycle;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.Plugins.Lifecycle.Tests;

public class PluginLifecycleIntegrationTests : IDisposable
{
    private readonly Mock<IPlugin> _mockPlugin;
    private readonly Mock<IPluginMetadata> _mockMetadata;
    private readonly Mock<ILogger<PluginStateTracker>> _mockStateTrackerLogger;
    private readonly Mock<ILogger<PluginRecoveryService>> _mockRecoveryLogger;
    private readonly Mock<ILogger<PluginUpdateManager>> _mockUpdateLogger;
    private readonly Mock<ILogger<PluginLifecycleManager>> _mockLifecycleLogger;
    
    private readonly PluginStateTracker _stateTracker;
    private readonly PluginRecoveryService _recoveryService;
    private readonly PluginUpdateManager _updateManager;
    private readonly PluginLifecycleManager _lifecycleManager;

    public PluginLifecycleIntegrationTests()
    {
        _mockPlugin = new Mock<IPlugin>();
        _mockMetadata = new Mock<IPluginMetadata>();
        
        _mockStateTrackerLogger = new Mock<ILogger<PluginStateTracker>>();
        _mockRecoveryLogger = new Mock<ILogger<PluginRecoveryService>>();
        _mockUpdateLogger = new Mock<ILogger<PluginUpdateManager>>();
        _mockLifecycleLogger = new Mock<ILogger<PluginLifecycleManager>>();

        _mockPlugin.Setup(p => p.Metadata).Returns(_mockMetadata.Object);
        _mockMetadata.Setup(m => m.Name).Returns("TestPlugin");
        _mockMetadata.Setup(m => m.Version).Returns(new Version(1, 0, 0));

        _stateTracker = new PluginStateTracker(_mockStateTrackerLogger.Object);
        _recoveryService = new PluginRecoveryService(_mockRecoveryLogger.Object);
        _updateManager = new PluginUpdateManager(_mockUpdateLogger.Object);
        
        _lifecycleManager = new PluginLifecycleManager(
            _mockLifecycleLogger.Object,
            _stateTracker,
            _recoveryService,
            _updateManager);
    }

    [Fact]
    public async Task LifecycleManager_CompletePluginLifecycle_ShouldSucceed()
    {
        // Arrange
        _mockPlugin.Setup(p => p.IsRunning).Returns(true);
        _mockPlugin.Setup(p => p.CanUnloadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act & Assert - Load
        var loadResult = await _lifecycleManager.LoadPluginAsync(_mockPlugin.Object);
        Assert.True(loadResult);
        Assert.Equal(PluginState.Loaded, _lifecycleManager.GetPluginState(_mockPlugin.Object));

        // Act & Assert - Start
        var startResult = await _lifecycleManager.StartPluginAsync(_mockPlugin.Object);
        Assert.True(startResult);
        Assert.Equal(PluginState.Running, _lifecycleManager.GetPluginState(_mockPlugin.Object));

        // Act & Assert - Stop
        var stopResult = await _lifecycleManager.StopPluginAsync(_mockPlugin.Object);
        Assert.True(stopResult);
        Assert.Equal(PluginState.Stopped, _lifecycleManager.GetPluginState(_mockPlugin.Object));

        // Act & Assert - Unload
        var unloadResult = await _lifecycleManager.UnloadPluginAsync(_mockPlugin.Object);
        Assert.True(unloadResult);

        // Verify plugin methods were called
        _mockPlugin.Verify(p => p.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockPlugin.Verify(p => p.StopAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // Called once for stop, once for unload
        _mockPlugin.Verify(p => p.CanUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockPlugin.Verify(p => p.PrepareUnloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockPlugin.Verify(p => p.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StateTracker_HealthMonitoring_ShouldDetectUnhealthyPlugin()
    {
        // Arrange
        PluginHealthResult? unhealthyResult = null;
        _stateTracker.PluginUnhealthy += (sender, result) => unhealthyResult = result;
        
        _mockPlugin.Setup(p => p.IsRunning).Returns(false); // Simulate unhealthy plugin

        // Act
        _stateTracker.StartTracking(_mockPlugin.Object, PluginState.Running);
        var healthResult = await _stateTracker.PerformHealthCheckAsync(_mockPlugin.Object);

        // Assert
        Assert.False(healthResult.IsHealthy);
        Assert.NotNull(unhealthyResult);
        Assert.Equal(_mockPlugin.Object, unhealthyResult.Plugin);
        Assert.Contains(_mockPlugin.Object, _stateTracker.UnhealthyPlugins);
    }

    [Fact]
    public async Task RecoveryService_AutoRecovery_ShouldBeEnabledByDefault()
    {
        // Act
        var isAutoRecoveryEnabled = _recoveryService.IsAutoRecoveryEnabled(_mockPlugin.Object);

        // Assert
        Assert.True(isAutoRecoveryEnabled);
        await Task.CompletedTask; // Fix async warning
    }

    [Fact]
    public async Task RecoveryService_CreateAndRestoreCheckpoint_ShouldWork()
    {
        // Act
        var createResult = await _recoveryService.CreateCheckpointAsync(_mockPlugin.Object);
        var restoreResult = await _recoveryService.RestoreFromCheckpointAsync(_mockPlugin.Object);

        // Assert
        Assert.True(createResult);
        Assert.True(restoreResult);
    }

    [Fact]
    public void UpdateManager_CanRollback_ShouldReturnFalseForPluginWithoutHistory()
    {
        // Act
        var canRollback = _updateManager.CanRollback(_mockPlugin.Object);

        // Assert
        Assert.False(canRollback);
    }

    [Fact]
    public async Task UpdateManager_CreateBackup_ShouldEnableRollback()
    {
        // Act
        var backupResult = await _updateManager.CreateBackupAsync(_mockPlugin.Object);
        var canRollback = _updateManager.CanRollback(_mockPlugin.Object);

        // Assert
        Assert.True(backupResult);
        Assert.True(canRollback);
    }

    [Fact]
    public async Task LifecycleManager_InitializeAndStart_ShouldWork()
    {
        // Act
        await _lifecycleManager.InitializeAsync();
        await _lifecycleManager.StartAsync();

        // Assert
        Assert.True(_lifecycleManager.IsRunning);
    }

    public void Dispose()
    {
        _stateTracker?.Dispose();
        _recoveryService?.Dispose();
        _updateManager?.Dispose();
        _lifecycleManager?.DisposeAsync().GetAwaiter().GetResult();
    }
}