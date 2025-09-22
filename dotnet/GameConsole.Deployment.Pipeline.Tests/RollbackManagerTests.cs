using GameConsole.Deployment.Core;
using GameConsole.Deployment.Pipeline;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Deployment.Pipeline.Tests;

public class RollbackManagerTests
{
    private readonly ILogger<RollbackManager> _logger;
    private readonly RollbackManager _rollbackManager;

    public RollbackManagerTests()
    {
        _logger = new TestLogger<RollbackManager>();
        _rollbackManager = new RollbackManager(_logger);
    }

    [Fact]
    public async Task InitiateRollbackAsync_WithValidDeployment_ReturnsRollbackId()
    {
        // Arrange
        var deploymentId = "test-deployment-001";
        
        await _rollbackManager.InitializeAsync();
        await _rollbackManager.StartAsync();

        // Act
        var rollbackId = await _rollbackManager.InitiateRollbackAsync(deploymentId);

        // Assert
        Assert.NotNull(rollbackId);
        Assert.StartsWith("rollback-", rollbackId);
        Assert.Contains(deploymentId, rollbackId);
    }

    [Fact]
    public async Task GetRollbackStatusAsync_WithValidRollbackId_ReturnsStatus()
    {
        // Arrange
        var deploymentId = "test-deployment-002";
        
        await _rollbackManager.InitializeAsync();
        await _rollbackManager.StartAsync();
        
        var rollbackId = await _rollbackManager.InitiateRollbackAsync(deploymentId);
        
        // Wait a bit for the rollback to complete
        await Task.Delay(100);

        // Act
        var status = await _rollbackManager.GetRollbackStatusAsync(rollbackId);

        // Assert
        Assert.True(status == DeploymentStatus.InProgress || status == DeploymentStatus.Succeeded);
    }

    [Fact]
    public async Task CanRollbackAsync_WithValidDeployment_ReturnsTrue()
    {
        // Arrange
        var deploymentId = "test-deployment-003";
        
        await _rollbackManager.InitializeAsync();
        await _rollbackManager.StartAsync();

        // Act
        var canRollback = await _rollbackManager.CanRollbackAsync(deploymentId);

        // Assert
        Assert.True(canRollback);
    }

    [Fact]
    public async Task CanRollbackAsync_WithEmptyDeploymentId_ReturnsFalse()
    {
        // Arrange
        var deploymentId = "";
        
        await _rollbackManager.InitializeAsync();
        await _rollbackManager.StartAsync();

        // Act
        var canRollback = await _rollbackManager.CanRollbackAsync(deploymentId);

        // Assert
        Assert.False(canRollback);
    }

    [Fact]
    public async Task GetRollbackTargetsAsync_WithValidDeployment_ReturnsTargets()
    {
        // Arrange
        var deploymentId = "test-deployment-004";
        
        await _rollbackManager.InitializeAsync();
        await _rollbackManager.StartAsync();

        // Act
        var targets = await _rollbackManager.GetRollbackTargetsAsync(deploymentId);

        // Assert
        Assert.NotNull(targets);
        Assert.NotEmpty(targets);
        Assert.Contains("v1.0.0", targets);
    }

    [Fact]
    public async Task RollbackStatusChanged_WhenRollbackCompletes_RaisesEvent()
    {
        // Arrange
        var deploymentId = "test-deployment-005";
        RollbackStatusChangedEventArgs? eventArgs = null;
        
        await _rollbackManager.InitializeAsync();
        await _rollbackManager.StartAsync();

        _rollbackManager.RollbackStatusChanged += (sender, args) =>
        {
            eventArgs = args;
        };

        // Act
        var rollbackId = await _rollbackManager.InitiateRollbackAsync(deploymentId);
        
        // Wait for the rollback to complete and event to fire
        await Task.Delay(5000);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(rollbackId, eventArgs.RollbackId);
        Assert.Equal(deploymentId, eventArgs.OriginalDeploymentId);
        Assert.Equal(DeploymentStatus.Succeeded, eventArgs.NewStatus);
    }

    // Simple test logger implementation
    private class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Simple console output for tests
            Console.WriteLine(formatter(state, exception));
        }
    }
}