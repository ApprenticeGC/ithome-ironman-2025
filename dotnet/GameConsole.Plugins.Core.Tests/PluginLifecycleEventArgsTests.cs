using GameConsole.Plugins.Core;
using Xunit;

namespace GameConsole.Plugins.Core.Tests;

/// <summary>
/// Tests for the PluginLifecycleEventArgs class.
/// </summary>
public class PluginLifecycleEventArgsTests
{
    [Fact]
    public void PluginLifecycleEventArgs_Should_Initialize_Properties_Correctly()
    {
        // Arrange
        var plugin = new TestPlugin();
        var phase = "Testing";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var eventArgs = new PluginLifecycleEventArgs(plugin, phase, exception);

        // Assert
        Assert.Same(plugin, eventArgs.Plugin);
        Assert.Equal(phase, eventArgs.Phase);
        Assert.Same(exception, eventArgs.Exception);
        Assert.False(eventArgs.Cancel);
    }

    [Fact]
    public void PluginLifecycleEventArgs_Should_Initialize_Without_Exception()
    {
        // Arrange
        var plugin = new TestPlugin();
        var phase = "Testing";

        // Act
        var eventArgs = new PluginLifecycleEventArgs(plugin, phase);

        // Assert
        Assert.Same(plugin, eventArgs.Plugin);
        Assert.Equal(phase, eventArgs.Phase);
        Assert.Null(eventArgs.Exception);
        Assert.False(eventArgs.Cancel);
    }

    [Fact]
    public void PluginLifecycleEventArgs_Should_Allow_Setting_Cancel()
    {
        // Arrange
        var plugin = new TestPlugin();
        var eventArgs = new PluginLifecycleEventArgs(plugin, "Testing");

        // Act
        eventArgs.Cancel = true;

        // Assert
        Assert.True(eventArgs.Cancel);
    }

    [Fact]
    public void PluginLifecycleEventArgs_Should_Throw_ArgumentNullException_For_Null_Plugin()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PluginLifecycleEventArgs(null!, "Testing"));
    }

    [Fact]
    public void PluginLifecycleEventArgs_Should_Throw_ArgumentNullException_For_Null_Phase()
    {
        // Arrange
        var plugin = new TestPlugin();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PluginLifecycleEventArgs(plugin, null!));
    }

    #region Test Implementation

    private class TestPlugin : IPlugin
    {
        public IPluginMetadata Metadata { get; } = new TestPluginMetadata();
        public IPluginContext? Context { get; set; }
        public bool IsRunning { get; private set; }

        public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
        {
            Context = context;
            return Task.CompletedTask;
        }

        public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private class TestPluginMetadata : IPluginMetadata
    {
        public string Id => "test.plugin";
        public string Name => "Test Plugin";
        public Version Version => new(1, 0, 0);
        public string Description => "A test plugin";
        public string Author => "Test Author";
        public IReadOnlyList<string> Dependencies => Array.Empty<string>();
        public IReadOnlyDictionary<string, object> Properties => 
            new Dictionary<string, object>();
    }

    #endregion
}