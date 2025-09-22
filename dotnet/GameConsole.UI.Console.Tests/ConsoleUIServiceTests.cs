using GameConsole.UI.Core;
using GameConsole.UI.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TestLib;
using Xunit;

namespace GameConsole.UI.Console.Tests;

/// <summary>
/// Tests for the ConsoleUIService implementation.
/// </summary>
public class ConsoleUIServiceTests
{
    private readonly ILogger<ConsoleUIService> _logger;

    public ConsoleUIServiceTests()
    {
        _logger = NullLogger<ConsoleUIService>.Instance;
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Act
        var service = new ConsoleUIService(_logger);

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConsoleUIService(null!));
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);

        // Act & Assert
        await service.InitializeAsync();
        
        // Should not throw
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();

        // Act
        await service.StartAsync();

        // Assert
        Assert.True(service.IsRunning);

        // Cleanup
        await service.StopAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act
        await service.StopAsync();

        // Assert
        Assert.False(service.IsRunning);
    }

    [Fact]
    public async Task GetSurfaceSizeAsync_ShouldReturnValidSize()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();

        // Act
        var size = await service.GetSurfaceSizeAsync();

        // Assert
        Assert.NotNull(size);
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public async Task RenderTextAsync_WithValidParameters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();
        var position = new Position(0, 0);
        var text = "Test";

        // Act & Assert
        await service.RenderTextAsync(text, position);
        
        // Should not throw
    }

    [Fact]
    public async Task RenderTextAsync_WithEmptyString_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();
        var position = new Position(0, 0);

        // Act & Assert
        await service.RenderTextAsync("", position);
        await service.RenderTextAsync(null!, position);
        
        // Should not throw
    }

    [Fact]
    public async Task ClearAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();

        // Act & Assert
        await service.ClearAsync();
        
        // Should not throw
    }

    [Fact]
    public async Task ClearAreaAsync_WithValidRectangle_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();
        var bounds = new Rectangle(new Position(0, 0), new Size(10, 5));

        // Act & Assert
        await service.ClearAreaAsync(bounds);
        
        // Should not throw
    }

    [Fact]
    public async Task SetCursorPositionAsync_WithValidPosition_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();
        var position = new Position(0, 0);

        // Act & Assert
        await service.SetCursorPositionAsync(position);
        
        // Should not throw
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ShouldReturnExpectedCapabilities()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);

        // Act
        var capabilities = await service.GetCapabilitiesAsync();

        // Assert
        Assert.NotNull(capabilities);
        Assert.Contains(typeof(IAdvancedTextRenderingCapability), capabilities);
        Assert.Contains(typeof(IComponentManagementCapability), capabilities);
    }

    [Fact]
    public async Task HasCapabilityAsync_WithSupportedCapability_ShouldReturnTrue()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);

        // Act & Assert
        Assert.True(await service.HasCapabilityAsync<IAdvancedTextRenderingCapability>());
        Assert.True(await service.HasCapabilityAsync<IComponentManagementCapability>());
    }

    [Fact]
    public async Task HasCapabilityAsync_WithUnsupportedCapability_ShouldReturnFalse()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);

        // Act & Assert
        Assert.False(await service.HasCapabilityAsync<IDisposable>());
    }

    [Fact]
    public async Task GetCapabilityAsync_WithSupportedCapability_ShouldReturnInstance()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);

        // Act
        var textCapability = await service.GetCapabilityAsync<IAdvancedTextRenderingCapability>();
        var componentCapability = await service.GetCapabilityAsync<IComponentManagementCapability>();

        // Assert
        Assert.NotNull(textCapability);
        Assert.NotNull(componentCapability);
        Assert.Same(service, textCapability);
        Assert.Same(service, componentCapability);
    }

    [Fact]
    public async Task AddComponentAsync_WithValidComponent_ShouldAddSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        var component = new TestUIComponent("test1", new Position(0, 0), new Size(10, 1));

        // Act & Assert
        await service.AddComponentAsync(component);
        
        // Should not throw
    }

    [Fact]
    public async Task AddComponentAsync_WithNullComponent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AddComponentAsync(null!));
    }

    [Fact]
    public async Task RemoveComponentAsync_WithValidId_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        var component = new TestUIComponent("test1", new Position(0, 0), new Size(10, 1));
        await service.AddComponentAsync(component);

        // Act & Assert
        await service.RemoveComponentAsync("test1");
        
        // Should not throw
    }

    [Fact]
    public async Task RenderAllComponentsAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);
        await service.InitializeAsync();
        var component = new TestUIComponent("test1", new Position(0, 0), new Size(10, 1));
        await service.AddComponentAsync(component);

        // Act & Assert
        await service.RenderAllComponentsAsync();
        
        // Should not throw
    }

    [Fact]
    public async Task ServiceLifecycle_ShouldWorkCorrectly()
    {
        // Arrange
        var service = new ConsoleUIService(_logger);

        // Act & Assert
        // Initialize
        await service.InitializeAsync();
        Assert.False(service.IsRunning);

        // Start
        await service.StartAsync();
        Assert.True(service.IsRunning);

        // Stop
        await service.StopAsync();
        Assert.False(service.IsRunning);

        // Dispose
        await service.DisposeAsync();
        Assert.False(service.IsRunning);
    }
}

/// <summary>
/// Test implementation of IUIComponent for testing purposes.
/// </summary>
internal class TestUIComponent : UIComponentBase
{
    public TestUIComponent(string id, Position position, Size size) : base(id, position, size)
    {
    }

    public override async Task RenderAsync(IService uiService, CancellationToken cancellationToken = default)
    {
        if (IsVisible)
        {
            await uiService.RenderTextAsync($"[{Id}]", Position, null, cancellationToken);
        }
    }
}