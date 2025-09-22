using GameConsole.UI.Core;
using GameConsole.UI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.UI.Services.Tests;

/// <summary>
/// Tests for ConsoleUIService implementation.
/// </summary>
public class ConsoleUIServiceTests
{
    private readonly Mock<ILogger<ConsoleUIService>> _mockLogger;
    private readonly ConsoleUIService _service;

    public ConsoleUIServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConsoleUIService>>();
        _service = new ConsoleUIService(_mockLogger.Object);
    }

    [Fact]
    public void IsRunning_Should_Be_False_Initially()
    {
        // Act & Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task InitializeAsync_Should_Complete_Successfully()
    {
        // Act & Assert
        await _service.InitializeAsync();
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initializing ConsoleUIService")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initialized ConsoleUIService")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_Should_Set_IsRunning_To_True()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Set_IsRunning_To_False()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task DisplayMessageAsync_Should_Throw_When_Not_Running()
    {
        // Arrange
        var message = new UIMessage("Test message", MessageType.Info);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DisplayMessageAsync(message));
        
        Assert.Contains("not running", exception.Message);
    }

    [Fact]
    public async Task DisplayMenuAsync_Should_Throw_When_Not_Running()
    {
        // Arrange
        var menu = new Menu("Test Menu", new[]
        {
            new MenuItem("item1", "Item 1")
        });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DisplayMenuAsync(menu));
        
        Assert.Contains("not running", exception.Message);
    }

    [Fact]
    public async Task ClearDisplayAsync_Should_Throw_When_Not_Running()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ClearDisplayAsync());
        
        Assert.Contains("not running", exception.Message);
    }

    [Fact]
    public async Task PromptInputAsync_Should_Throw_When_Not_Running()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.PromptInputAsync("Test prompt"));
        
        Assert.Contains("not running", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_Should_Stop_Service_If_Running()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        Assert.True(_service.IsRunning);

        // Act
        await _service.DisposeAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task Service_Lifecycle_Should_Work_Correctly()
    {
        // Arrange & Act
        Assert.False(_service.IsRunning);

        await _service.InitializeAsync();
        Assert.False(_service.IsRunning); // Initialize doesn't start the service

        await _service.StartAsync();
        Assert.True(_service.IsRunning);

        await _service.StopAsync();
        Assert.False(_service.IsRunning);

        await _service.DisposeAsync();
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public void Service_Should_Implement_IService_Interface()
    {
        // Act & Assert
        Assert.IsAssignableFrom<GameConsole.UI.Core.IService>(_service);
        Assert.IsAssignableFrom<GameConsole.Core.Abstractions.IService>(_service);
        Assert.IsAssignableFrom<IAsyncDisposable>(_service);
    }
}