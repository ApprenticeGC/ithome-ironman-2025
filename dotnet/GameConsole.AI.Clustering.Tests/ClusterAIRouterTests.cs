using GameConsole.AI.Clustering;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Clustering.Tests;

public class ClusterAIRouterTests
{
    private readonly ILogger<ClusterAIRouter> _logger;
    private readonly ClusterAIRouter _router;

    public ClusterAIRouterTests()
    {
        _logger = new LoggerFactory().CreateLogger<ClusterAIRouter>();
        _router = new ClusterAIRouter(_logger);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        // Act
        await _router.InitializeAsync();

        // Assert
        // No exceptions should be thrown
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        // Arrange
        await _router.InitializeAsync();

        // Act
        await _router.StartAsync();

        // Assert
        Assert.True(_router.IsRunning);
    }

    [Fact]
    public async Task GetAvailableNodesAsync_WithTextProcessing_ShouldReturnNodes()
    {
        // Arrange
        await _router.InitializeAsync();
        await _router.StartAsync();

        // Act
        var nodes = await _router.GetAvailableNodesAsync("text-processing", 5);

        // Assert
        Assert.NotEmpty(nodes);
    }

    [Fact]
    public async Task RouteMessageAsync_WithValidCapability_ShouldReturnNodeAddress()
    {
        // Arrange
        await _router.InitializeAsync();
        await _router.StartAsync();

        // Act
        var nodeAddress = await _router.RouteMessageAsync("test-message-1", "text-processing", 2);

        // Assert
        Assert.NotNull(nodeAddress);
        Assert.NotEmpty(nodeAddress);
    }

    [Fact]
    public async Task RouteMessageAsync_WithInvalidCapability_ShouldThrowException()
    {
        // Arrange
        await _router.InitializeAsync();
        await _router.StartAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _router.RouteMessageAsync("test-message-1", "non-existent-capability", 1));
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
    {
        // Arrange
        await _router.InitializeAsync();
        await _router.StartAsync();

        // Act
        await _router.StopAsync();

        // Assert
        Assert.False(_router.IsRunning);
    }
}