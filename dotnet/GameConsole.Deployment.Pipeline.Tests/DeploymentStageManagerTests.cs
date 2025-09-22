using GameConsole.Deployment.Core;
using GameConsole.Deployment.Pipeline;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Deployment.Pipeline.Tests;

public class DeploymentStageManagerTests
{
    private readonly ILogger<DeploymentStageManager> _logger;
    private readonly DeploymentStageManager _stageManager;

    public DeploymentStageManagerTests()
    {
        _logger = new TestLogger<DeploymentStageManager>();
        _stageManager = new DeploymentStageManager(_logger);
    }

    [Fact]
    public async Task ExecuteStageAsync_WithValidStage_CompletesSuccessfully()
    {
        // Arrange
        var deploymentId = "test-deployment-001";
        var stage = new DeploymentStage
        {
            Id = "stage-001",
            Name = "Build",
            Environment = DeploymentEnvironment.Development,
            Order = 1,
            RequiresApproval = false
        };

        // Act
        await _stageManager.InitializeAsync();
        await _stageManager.StartAsync();
        var result = await _stageManager.ExecuteStageAsync(deploymentId, stage);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StageStatus.Completed, result.Status);
        Assert.Equal(stage, result.Stage);
        Assert.NotNull(result.Output);
        Assert.Contains("Development", result.Output);
    }

    [Fact]
    public async Task ExecuteStageAsync_WithApprovalRequired_ReturnsPendingApproval()
    {
        // Arrange
        var deploymentId = "test-deployment-002";
        var stage = new DeploymentStage
        {
            Id = "stage-002",
            Name = "Production Deploy",
            Environment = DeploymentEnvironment.Production,
            Order = 2,
            RequiresApproval = true
        };

        // Act
        await _stageManager.InitializeAsync();
        await _stageManager.StartAsync();
        var result = await _stageManager.ExecuteStageAsync(deploymentId, stage);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StageStatus.PendingApproval, result.Status);
        Assert.Equal(stage, result.Stage);
    }

    [Fact]
    public async Task ApproveStageAsync_WithPendingStage_CompletesStage()
    {
        // Arrange
        var deploymentId = "test-deployment-003";
        var stage = new DeploymentStage
        {
            Id = "stage-003",
            Name = "Staging Deploy",
            Environment = DeploymentEnvironment.Staging,
            Order = 1,
            RequiresApproval = true
        };

        await _stageManager.InitializeAsync();
        await _stageManager.StartAsync();
        
        // Execute stage (will be pending)
        var initialResult = await _stageManager.ExecuteStageAsync(deploymentId, stage);
        Assert.Equal(StageStatus.PendingApproval, initialResult.Status);

        // Act - Approve the stage
        await _stageManager.ApproveStageAsync(deploymentId, stage.Id);

        // Assert
        var finalResult = _stageManager.GetStageResult(deploymentId, stage.Id);
        Assert.NotNull(finalResult);
        Assert.Equal(StageStatus.Completed, finalResult.Status);
        Assert.NotNull(finalResult.Output);
    }

    [Fact]
    public async Task GetStageResult_WithNonExistentStage_ReturnsNull()
    {
        // Arrange
        var deploymentId = "test-deployment-004";
        var stageId = "non-existent-stage";

        // Act
        await _stageManager.InitializeAsync();
        var result = _stageManager.GetStageResult(deploymentId, stageId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllStageResults_WithMultipleStages_ReturnsAllResults()
    {
        // Arrange
        var deploymentId = "test-deployment-005";
        var stages = new[]
        {
            new DeploymentStage
            {
                Id = "stage-005-1",
                Name = "Build",
                Environment = DeploymentEnvironment.Development,
                Order = 1,
                RequiresApproval = false
            },
            new DeploymentStage
            {
                Id = "stage-005-2",
                Name = "Test",
                Environment = DeploymentEnvironment.Testing,
                Order = 2,
                RequiresApproval = false
            }
        };

        await _stageManager.InitializeAsync();
        await _stageManager.StartAsync();

        // Act - Execute multiple stages
        foreach (var stage in stages)
        {
            await _stageManager.ExecuteStageAsync(deploymentId, stage);
        }

        var results = _stageManager.GetAllStageResults(deploymentId);

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, r => Assert.Equal(StageStatus.Completed, r.Status));
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