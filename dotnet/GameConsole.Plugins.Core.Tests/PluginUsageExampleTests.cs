using GameConsole.Plugins.Core;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameConsole.Plugins.Core.Tests;

/// <summary>
/// Example implementations and usage tests for plugin interfaces.
/// These tests demonstrate how the plugin framework would be used in practice.
/// </summary>
public class PluginUsageExampleTests
{
    [Fact]
    public async Task ExamplePlugin_Should_Support_Full_Lifecycle()
    {
        // Arrange
        var plugin = new ExamplePlugin();
        var context = new TestPluginContext();
        
        // Act & Assert - Test plugin lifecycle
        Assert.False(plugin.IsRunning);
        
        // Configure
        await plugin.ConfigureAsync(context);
        Assert.Same(context, plugin.Context);
        
        // Initialize
        await plugin.InitializeAsync();
        
        // Start
        await plugin.StartAsync();
        Assert.True(plugin.IsRunning);
        
        // Check if can unload (should be false while running)
        var canUnloadWhileRunning = await plugin.CanUnloadAsync();
        Assert.False(canUnloadWhileRunning);
        
        // Stop
        await plugin.StopAsync();
        Assert.False(plugin.IsRunning);
        
        // Check if can unload (should be true when stopped)
        var canUnloadWhenStopped = await plugin.CanUnloadAsync();
        Assert.True(canUnloadWhenStopped);
        
        // Prepare for unload
        await plugin.PrepareUnloadAsync();
        
        // Dispose
        await plugin.DisposeAsync();
    }

    [Fact]
    public void ExamplePlugin_Should_Have_Correct_Metadata_From_Attribute()
    {
        // Arrange
        var pluginType = typeof(ExamplePlugin);
        
        // Act
        var attribute = pluginType.GetCustomAttributes(typeof(PluginAttribute), false)
            .Cast<PluginAttribute>()
            .FirstOrDefault();
        
        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("example.plugin", attribute.Id);
        Assert.Equal("Example Plugin", attribute.Name);
        Assert.Equal("1.2.3", attribute.Version);
        Assert.Equal("An example plugin for testing", attribute.Description);
        Assert.Equal("Test Team", attribute.Author);
        Assert.Contains("example", attribute.Tags);
        Assert.Contains("test", attribute.Tags);
        Assert.Contains("core.service", attribute.Dependencies);
        Assert.Equal("1.0.0", attribute.MinimumHostVersion);
        Assert.True(attribute.CanUnload);
    }

    [Fact]
    public void PluginLifecycleEvents_Should_Support_Event_Handling()
    {
        // Arrange
        var events = new TestPluginLifecycleEvents();
        var plugin = new ExamplePlugin();
        var eventsFired = new List<string>();

        // Subscribe to events
        events.PluginConfiguring += (s, e) => eventsFired.Add($"Configuring:{e.Plugin.Metadata.Id}");
        events.PluginConfigured += (s, e) => eventsFired.Add($"Configured:{e.Plugin.Metadata.Id}");
        events.PluginStarting += (s, e) => eventsFired.Add($"Starting:{e.Plugin.Metadata.Id}");
        events.PluginStarted += (s, e) => eventsFired.Add($"Started:{e.Plugin.Metadata.Id}");

        // Act - Fire some events
        events.OnPluginConfiguring(plugin, "Configuring");
        events.OnPluginConfigured(plugin, "Configured");
        events.OnPluginStarting(plugin, "Starting");
        events.OnPluginStarted(plugin, "Started");

        // Assert
        Assert.Contains("Configuring:example.plugin", eventsFired);
        Assert.Contains("Configured:example.plugin", eventsFired);
        Assert.Contains("Starting:example.plugin", eventsFired);
        Assert.Contains("Started:example.plugin", eventsFired);
    }

    #region Test Implementations

    [Plugin("example.plugin", "Example Plugin", "1.2.3", "An example plugin for testing", "Test Team",
        Dependencies = new[] { "core.service" },
        MinimumHostVersion = "1.0.0",
        CanUnload = true,
        Tags = new[] { "example", "test" })]
    private class ExamplePlugin : IPlugin
    {
        public IPluginMetadata Metadata { get; } = new ExamplePluginMetadata();
        public IPluginContext? Context { get; set; }
        public bool IsRunning { get; private set; }

        public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            return Task.CompletedTask;
        }

        public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!IsRunning); // Can only unload when stopped
        }

        public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
        {
            // Cleanup logic would go here
            return Task.CompletedTask;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (Context == null)
                throw new InvalidOperationException("Plugin must be configured before initialization");
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
            IsRunning = false;
            return ValueTask.CompletedTask;
        }
    }

    private class ExamplePluginMetadata : IPluginMetadata
    {
        public string Id => "example.plugin";
        public string Name => "Example Plugin";
        public Version Version => new(1, 2, 3);
        public string Description => "An example plugin for testing";
        public string Author => "Test Team";
        public IReadOnlyList<string> Dependencies => new[] { "core.service" };
        public IReadOnlyDictionary<string, object> Properties => 
            new Dictionary<string, object>
            {
                { "CanHotReload", true },
                { "RequiresElevation", false }
            };
    }

    private class TestPluginContext : IPluginContext
    {
        public IServiceProvider Services { get; } = new TestServiceProvider();
        public IConfiguration Configuration { get; } = new ConfigurationBuilder().Build();
        public string PluginDirectory => "/plugins/example";
        public CancellationToken ShutdownToken => CancellationToken.None;
        public IReadOnlyDictionary<string, object> Properties => 
            new Dictionary<string, object>
            {
                { "HostVersion", "1.0.0" },
                { "Environment", "Test" }
            };
    }

    private class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class TestPluginLifecycleEvents : IPluginLifecycleEvents
    {
        public event EventHandler<PluginLifecycleEventArgs>? PluginConfiguring;
        public event EventHandler<PluginLifecycleEventArgs>? PluginConfigured;
        
        #pragma warning disable CS0067 // Event is never used
        public event EventHandler<PluginLifecycleEventArgs>? PluginInitializing;
        public event EventHandler<PluginLifecycleEventArgs>? PluginInitialized;
        #pragma warning restore CS0067 // Event is never used
        
        public event EventHandler<PluginLifecycleEventArgs>? PluginStarting;
        public event EventHandler<PluginLifecycleEventArgs>? PluginStarted;
        
        #pragma warning disable CS0067 // Event is never used
        public event EventHandler<PluginLifecycleEventArgs>? PluginStopping;
        public event EventHandler<PluginLifecycleEventArgs>? PluginStopped;
        public event EventHandler<PluginLifecycleEventArgs>? PluginUnloading;
        public event EventHandler<PluginLifecycleEventArgs>? PluginUnloaded;
        public event EventHandler<PluginLifecycleEventArgs>? PluginError;
        #pragma warning restore CS0067 // Event is never used

        public void OnPluginConfiguring(IPlugin plugin, string phase) => 
            PluginConfiguring?.Invoke(this, new PluginLifecycleEventArgs(plugin, phase));
        
        public void OnPluginConfigured(IPlugin plugin, string phase) => 
            PluginConfigured?.Invoke(this, new PluginLifecycleEventArgs(plugin, phase));
        
        public void OnPluginStarting(IPlugin plugin, string phase) => 
            PluginStarting?.Invoke(this, new PluginLifecycleEventArgs(plugin, phase));
        
        public void OnPluginStarted(IPlugin plugin, string phase) => 
            PluginStarted?.Invoke(this, new PluginLifecycleEventArgs(plugin, phase));
    }

    #endregion
}