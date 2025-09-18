using GameConsole.Plugins.Lifecycle;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Plugins.Lifecycle.Tests;

/// <summary>
/// Tests for the plugin lifecycle manager.
/// </summary>
public class PluginLifecycleManagerTests : IDisposable
{
    private readonly PluginLifecycleManager _manager;
    private readonly string _testPluginPath;

    public PluginLifecycleManagerTests()
    {
        _manager = new PluginLifecycleManager();
        
        // Create a test plugin assembly path (using this test assembly as a mock plugin)
        _testPluginPath = GetType().Assembly.Location;
    }

    [Fact]
    public async Task InitializeAsync_Should_Initialize_Manager()
    {
        // Act
        await _manager.InitializeAsync();

        // Assert
        Assert.False(_manager.IsRunning); // Should not be running yet, only initialized
    }

    [Fact]
    public async Task StartAsync_Should_Start_Manager()
    {
        // Arrange
        await _manager.InitializeAsync();

        // Act
        await _manager.StartAsync();

        // Assert
        Assert.True(_manager.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Stop_Manager()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();

        // Act
        await _manager.StopAsync();

        // Assert
        Assert.False(_manager.IsRunning);
    }

    [Fact]
    public async Task LoadPluginAsync_Should_Load_Valid_Plugin()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();

        // Act
        var result = await _manager.LoadPluginAsync(_testPluginPath);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal(PluginState.Loaded, _manager.GetPluginState(result.Metadata.Id));
    }

    [Fact]
    public async Task LoadPluginAsync_Should_Fail_For_Invalid_Path()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();

        // Act
        var result = await _manager.LoadPluginAsync("invalid_path.dll");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task StartPluginAsync_Should_Start_Loaded_Plugin()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();
        var loadResult = await _manager.LoadPluginAsync(_testPluginPath);
        Assert.True(loadResult.Success);
        var pluginId = loadResult.Metadata!.Id;

        // Act
        var success = await _manager.StartPluginAsync(pluginId);

        // Assert
        Assert.True(success);
        Assert.Equal(PluginState.Running, _manager.GetPluginState(pluginId));
    }

    [Fact]
    public async Task StopPluginAsync_Should_Stop_Running_Plugin()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();
        var loadResult = await _manager.LoadPluginAsync(_testPluginPath);
        var pluginId = loadResult.Metadata!.Id;
        await _manager.StartPluginAsync(pluginId);

        // Act
        var success = await _manager.StopPluginAsync(pluginId);

        // Assert
        Assert.True(success);
        Assert.Equal(PluginState.Stopped, _manager.GetPluginState(pluginId));
    }

    [Fact]
    public async Task UnloadPluginAsync_Should_Unload_Plugin()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();
        var loadResult = await _manager.LoadPluginAsync(_testPluginPath);
        var pluginId = loadResult.Metadata!.Id;

        // Act
        var success = await _manager.UnloadPluginAsync(pluginId);

        // Assert
        Assert.True(success);
        Assert.Equal(PluginState.NotLoaded, _manager.GetPluginState(pluginId));
    }

    [Fact]
    public async Task ReloadPluginAsync_Should_Reload_Plugin()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();
        var loadResult = await _manager.LoadPluginAsync(_testPluginPath);
        var pluginId = loadResult.Metadata!.Id;
        await _manager.StartPluginAsync(pluginId);

        // Act
        var success = await _manager.ReloadPluginAsync(pluginId);

        // Assert
        Assert.True(success);
        Assert.Equal(PluginState.Running, _manager.GetPluginState(pluginId));
    }

    [Fact]
    public async Task GetLoadedPlugins_Should_Return_All_Loaded_Plugins()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();
        var loadResult = await _manager.LoadPluginAsync(_testPluginPath);

        // Act
        var plugins = _manager.GetLoadedPlugins();

        // Assert
        Assert.Single(plugins);
        Assert.Equal(loadResult.Metadata!.Id, plugins.First().Metadata.Id);
    }

    [Fact]
    public async Task ValidatePluginAsync_Should_Validate_Plugin()
    {
        // Act
        var result = await _manager.ValidatePluginAsync(_testPluginPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CheckPluginHealthAsync_Should_Check_Plugin_Health()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();
        var loadResult = await _manager.LoadPluginAsync(_testPluginPath);
        var pluginId = loadResult.Metadata!.Id;
        await _manager.StartPluginAsync(pluginId);

        // Act
        var healthResult = await _manager.CheckPluginHealthAsync(pluginId);

        // Assert
        Assert.Equal(PluginHealth.Healthy, healthResult.Health);
        Assert.NotNull(healthResult.ResponseTime);
    }

    [Fact]
    public async Task PluginStateChanged_Event_Should_Fire_On_State_Changes()
    {
        // Arrange
        await _manager.InitializeAsync();
        await _manager.StartAsync();
        PluginLifecycleEventArgs? capturedEvent = null;
        _manager.PluginStateChanged += (sender, args) => capturedEvent = args;

        // Act
        var loadResult = await _manager.LoadPluginAsync(_testPluginPath);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(loadResult.Metadata!.Id, capturedEvent.PluginId);
        Assert.Equal(PluginState.NotLoaded, capturedEvent.OldState);
        Assert.Equal(PluginState.Loaded, capturedEvent.NewState);
    }

    public void Dispose()
    {
        _manager?.DisposeAsync().AsTask().Wait();
    }
}