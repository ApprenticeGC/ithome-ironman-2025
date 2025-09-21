using GameConsole.Plugins.Core;
using GameConsole.Plugins.Lifecycle;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.Plugins.Lifecycle.Tests;

public class PluginStateTrackerTests : IDisposable
{
    private readonly Mock<IPlugin> _mockPlugin;
    private readonly Mock<IPluginMetadata> _mockMetadata;
    private readonly Mock<ILogger<PluginStateTracker>> _mockLogger;
    private readonly PluginStateTracker _stateTracker;

    public PluginStateTrackerTests()
    {
        _mockPlugin = new Mock<IPlugin>();
        _mockMetadata = new Mock<IPluginMetadata>();
        _mockLogger = new Mock<ILogger<PluginStateTracker>>();

        _mockPlugin.Setup(p => p.Metadata).Returns(_mockMetadata.Object);
        _mockMetadata.Setup(m => m.Name).Returns("TestPlugin");

        _stateTracker = new PluginStateTracker(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PluginStateTracker(null!));
    }

    [Fact]
    public void GetPluginState_WithUntrackedPlugin_ReturnsNull()
    {
        // Act
        var state = _stateTracker.GetPluginState(_mockPlugin.Object);

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public void StartTracking_WithPlugin_SetsInitialState()
    {
        // Act
        _stateTracker.StartTracking(_mockPlugin.Object, PluginState.Loaded);

        // Assert
        var state = _stateTracker.GetPluginState(_mockPlugin.Object);
        Assert.Equal(PluginState.Loaded, state);
    }

    [Fact]
    public void StartTracking_WithNullPlugin_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _stateTracker.StartTracking(null!));
    }

    [Fact]
    public void SetPluginState_UpdatesStateAndTriggersEvent()
    {
        // Arrange
        PluginStateChangedEventArgs? eventArgs = null;
        _stateTracker.StateChanged += (sender, args) => eventArgs = args;

        _stateTracker.StartTracking(_mockPlugin.Object, PluginState.Loaded);

        // Act
        _stateTracker.SetPluginState(_mockPlugin.Object, PluginState.Running);

        // Assert
        var state = _stateTracker.GetPluginState(_mockPlugin.Object);
        Assert.Equal(PluginState.Running, state);
        
        Assert.NotNull(eventArgs);
        Assert.Equal(_mockPlugin.Object, eventArgs.Plugin);
        Assert.Equal(PluginState.Loaded, eventArgs.PreviousState);
        Assert.Equal(PluginState.Running, eventArgs.NewState);
        Assert.Null(eventArgs.Exception);
    }

    [Fact]
    public void SetPluginState_WithException_IncludesExceptionInEvent()
    {
        // Arrange
        PluginStateChangedEventArgs? eventArgs = null;
        _stateTracker.StateChanged += (sender, args) => eventArgs = args;
        
        var exception = new InvalidOperationException("Test error");
        _stateTracker.StartTracking(_mockPlugin.Object, PluginState.Running);

        // Act
        _stateTracker.SetPluginState(_mockPlugin.Object, PluginState.Failed, exception);

        // Assert
        var state = _stateTracker.GetPluginState(_mockPlugin.Object);
        Assert.Equal(PluginState.Failed, state);
        
        Assert.NotNull(eventArgs);
        Assert.Equal(exception, eventArgs.Exception);
    }

    [Fact]
    public async Task PerformHealthCheckAsync_WithRunningPlugin_ReturnsHealthyResult()
    {
        // Arrange
        _mockPlugin.Setup(p => p.IsRunning).Returns(true);
        _stateTracker.StartTracking(_mockPlugin.Object, PluginState.Running);

        // Act
        var result = await _stateTracker.PerformHealthCheckAsync(_mockPlugin.Object);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal(_mockPlugin.Object, result.Plugin);
        Assert.True(result.ResponseTime > TimeSpan.Zero);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task PerformHealthCheckAsync_WithNonRunningPlugin_ReturnsUnhealthyResult()
    {
        // Arrange
        PluginHealthResult? unhealthyEventResult = null;
        _stateTracker.PluginUnhealthy += (sender, result) => unhealthyEventResult = result;
        
        _mockPlugin.Setup(p => p.IsRunning).Returns(false);
        _stateTracker.StartTracking(_mockPlugin.Object, PluginState.Stopped);

        // Act
        var result = await _stateTracker.PerformHealthCheckAsync(_mockPlugin.Object);

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal(_mockPlugin.Object, result.Plugin);
        Assert.NotNull(unhealthyEventResult);
        Assert.Equal(result, unhealthyEventResult);
    }

    [Fact]
    public void StopTracking_RemovesPluginFromTracking()
    {
        // Arrange
        _stateTracker.StartTracking(_mockPlugin.Object, PluginState.Running);
        Assert.NotNull(_stateTracker.GetPluginState(_mockPlugin.Object));

        // Act
        _stateTracker.StopTracking(_mockPlugin.Object);

        // Assert
        Assert.Null(_stateTracker.GetPluginState(_mockPlugin.Object));
    }

    [Fact]
    public void StopTracking_WithNullPlugin_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _stateTracker.StopTracking(null!));
    }

    [Fact]
    public async Task PerformHealthCheckAsync_WithNullPlugin_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _stateTracker.PerformHealthCheckAsync(null!));
    }

    public void Dispose()
    {
        _stateTracker?.Dispose();
    }
}