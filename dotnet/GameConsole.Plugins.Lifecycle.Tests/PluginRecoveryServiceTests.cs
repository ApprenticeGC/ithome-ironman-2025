using GameConsole.Plugins.Lifecycle;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Plugins.Lifecycle.Tests;

/// <summary>
/// Tests for the plugin recovery service.
/// </summary>
public class PluginRecoveryServiceTests : IDisposable
{
    private readonly MockPluginLifecycleManager _lifecycleManager;
    private readonly PluginStateTracker _stateTracker;
    private readonly PluginRecoveryService _recoveryService;
    private readonly PluginMetadata _testPlugin;

    public PluginRecoveryServiceTests()
    {
        _lifecycleManager = new MockPluginLifecycleManager();
        _stateTracker = new PluginStateTracker();
        var options = new PluginRecoveryOptions(MaxRecoveryAttempts: 2, RecoveryDelay: TimeSpan.FromMilliseconds(10));
        _recoveryService = new PluginRecoveryService(_lifecycleManager, _stateTracker, options);
        
        _testPlugin = PluginMetadata.Create(
            "test-plugin",
            "Test Plugin",
            new Version(1, 0, 0),
            "/test/path.dll"
        );
    }

    [Fact]
    public async Task CreateCheckpointAsync_Should_Create_Checkpoint()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Running, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);

        // Act
        var success = await _recoveryService.CreateCheckpointAsync(_testPlugin.Id);

        // Assert
        Assert.True(success);
    }

    [Fact]
    public async Task RecoverPluginAsync_Should_Attempt_Restart_Strategy_First()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Failed, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);

        // Act
        var result = await _recoveryService.RecoverPluginAsync(_testPlugin.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(RecoveryStrategy.Restart, result.Strategy);
        Assert.True(_lifecycleManager.StopPluginCalled);
        Assert.True(_lifecycleManager.StartPluginCalled);
    }

    [Fact]
    public async Task RecoverPluginAsync_Should_Fire_RecoveryAttempted_Event()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Failed, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);
        RecoveryAttemptEventArgs? capturedEvent = null;
        _recoveryService.RecoveryAttempted += (sender, args) => capturedEvent = args;

        // Act
        await _recoveryService.RecoverPluginAsync(_testPlugin.Id);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(_testPlugin.Id, capturedEvent.PluginId);
        Assert.Equal(RecoveryStrategy.Restart, capturedEvent.Strategy);
        Assert.Equal(1, capturedEvent.AttemptNumber);
    }

    [Fact]
    public async Task RecoverPluginAsync_Should_Fire_RecoveryCompleted_Event()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Failed, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);
        RecoveryCompletedEventArgs? capturedEvent = null;
        _recoveryService.RecoveryCompleted += (sender, args) => capturedEvent = args;

        // Act
        await _recoveryService.RecoverPluginAsync(_testPlugin.Id);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(_testPlugin.Id, capturedEvent.PluginId);
        Assert.Equal(RecoveryStrategy.Restart, capturedEvent.Strategy);
        Assert.True(capturedEvent.Success);
        Assert.Equal(1, capturedEvent.AttemptNumber);
    }

    [Fact]
    public async Task RecoverPluginAsync_Should_Try_Different_Strategies_On_Multiple_Attempts()
    {
        // Arrange
        var loadedPlugin = new LoadedPlugin(_testPlugin, PluginState.Failed, DateTime.UtcNow);
        _stateTracker.RegisterPlugin(loadedPlugin);
        _lifecycleManager.StartPluginAsyncResult = false; // Make first attempt fail

        // Act - First attempt (should use Restart)
        var result1 = await _recoveryService.RecoverPluginAsync(_testPlugin.Id);
        
        // Reset for second attempt
        _lifecycleManager.StartPluginAsyncResult = true;
        _lifecycleManager.ReloadPluginAsyncResult = true;
        
        // Second attempt (should use Reload)
        var result2 = await _recoveryService.RecoverPluginAsync(_testPlugin.Id);

        // Assert
        Assert.False(result1.Success);
        Assert.Equal(RecoveryStrategy.Restart, result1.Strategy);
        
        Assert.True(result2.Success);
        Assert.Equal(RecoveryStrategy.Reload, result2.Strategy);
        Assert.True(_lifecycleManager.ReloadPluginCalled);
    }

    public void Dispose()
    {
        _recoveryService?.Dispose();
        _stateTracker?.Dispose();
        _lifecycleManager?.DisposeAsync().AsTask().Wait();
    }
}

/// <summary>
/// Mock implementation of IPluginLifecycleManager for testing.
/// </summary>
public class MockPluginLifecycleManager : IPluginLifecycleManager
{
    public bool IsRunning { get; private set; }
    public bool StopPluginCalled { get; private set; }
    public bool StartPluginCalled { get; private set; }
    public bool ReloadPluginCalled { get; private set; }
    public bool StartPluginAsyncResult { get; set; } = true;
    public bool ReloadPluginAsyncResult { get; set; } = true;

    public event EventHandler<PluginLifecycleEventArgs>? PluginStateChanged
    {
        add { }
        remove { }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        var metadata = PluginMetadata.Create("test-plugin", "Test Plugin", new Version(1, 0, 0), pluginPath);
        return Task.FromResult(new PluginLoadResult(true, metadata));
    }

    public Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ReloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        ReloadPluginCalled = true;
        return Task.FromResult(ReloadPluginAsyncResult);
    }

    public Task<bool> StartPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        StartPluginCalled = true;
        return Task.FromResult(StartPluginAsyncResult);
    }

    public Task<bool> StopPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        StopPluginCalled = true;
        return Task.FromResult(true);
    }

    public PluginState GetPluginState(string pluginId)
    {
        return PluginState.Running;
    }

    public IEnumerable<LoadedPlugin> GetLoadedPlugins()
    {
        return Array.Empty<LoadedPlugin>();
    }

    public PluginMetadata? GetPluginInfo(string pluginId)
    {
        return PluginMetadata.Create(pluginId, "Test Plugin", new Version(1, 0, 0), "/test/path.dll");
    }

    public Task<ValidationResult> ValidatePluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    public Task<HealthCheckResult> CheckPluginHealthAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HealthCheckResult(PluginHealth.Healthy));
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}