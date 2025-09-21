using Xunit;
using GameConsole.Deployment.Pipeline;
using GameConsole.Deployment.Pipeline.Services;
using GameConsole.Deployment.Pipeline.Providers;
using GameConsole.Plugins.Lifecycle;
using Moq;

namespace GameConsole.Deployment.Pipeline.Tests;

/// <summary>
/// Tests for the main DeploymentPipeline orchestrator.
/// </summary>
public class DeploymentPipelineTests
{
    private readonly Mock<IPluginLifecycleManager> _mockPluginLifecycleManager;
    private readonly Mock<IRollbackManager> _mockRollbackManager;
    private readonly Mock<IDeploymentProvider> _mockDeploymentProvider;
    private readonly DeploymentPipeline _deploymentPipeline;

    public DeploymentPipelineTests()
    {
        _mockPluginLifecycleManager = new Mock<IPluginLifecycleManager>();
        _mockRollbackManager = new Mock<IRollbackManager>();
        _mockDeploymentProvider = new Mock<IDeploymentProvider>();

        // Setup basic mock behaviors for all dependencies
        _mockPluginLifecycleManager.Setup(m => m.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockPluginLifecycleManager.Setup(m => m.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockPluginLifecycleManager.Setup(m => m.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockPluginLifecycleManager.Setup(m => m.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
        
        _mockRollbackManager.Setup(m => m.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRollbackManager.Setup(m => m.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRollbackManager.Setup(m => m.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRollbackManager.Setup(m => m.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
        
        _mockDeploymentProvider.Setup(m => m.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDeploymentProvider.Setup(m => m.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDeploymentProvider.Setup(m => m.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockDeploymentProvider.Setup(m => m.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _deploymentPipeline = new DeploymentPipeline(
            _mockPluginLifecycleManager.Object,
            _mockRollbackManager.Object,
            _mockDeploymentProvider.Object);
    }

    [Fact]
    public async Task InitializeAsync_Should_Initialize_All_Components()
    {
        // Act
        await _deploymentPipeline.InitializeAsync();

        // Assert - Since the pipeline creates its own StageManager internally, 
        // we can only verify the injected dependencies
        _mockRollbackManager.Verify(m => m.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockDeploymentProvider.Verify(m => m.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_Should_Start_All_Components_And_Set_Running_State()
    {
        // Arrange - Mocks are already set up in constructor

        // Act
        await _deploymentPipeline.StartAsync();

        // Assert
        Assert.True(_deploymentPipeline.IsRunning);
        _mockRollbackManager.Verify(m => m.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockDeploymentProvider.Verify(m => m.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateDeploymentAsync_Should_Return_Invalid_For_Missing_Required_Fields()
    {
        // Arrange
        var deploymentConfig = new DeploymentConfig
        {
            DeploymentId = "", // Missing required field
            Name = "",
            Version = "",
            TargetEnvironment = ""
        };

        // Act
        var result = await _deploymentPipeline.ValidateDeploymentAsync(deploymentConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Deployment ID is required.", result.Errors);
        Assert.Contains("Deployment name is required.", result.Errors);
        Assert.Contains("Version is required.", result.Errors);
        Assert.Contains("Target environment is required.", result.Errors);
        Assert.Contains("At least one deployment stage is required.", result.Errors);
    }

    [Fact]
    public async Task ValidateDeploymentAsync_Should_Return_Valid_For_Proper_Configuration()
    {
        // Arrange
        var deploymentConfig = new DeploymentConfig
        {
            DeploymentId = "deploy-123",
            Name = "Test Deployment",
            Version = "1.0.0",
            TargetEnvironment = "production",
            Stages = new List<StageConfig>
            {
                new StageConfig
                {
                    Id = "stage1",
                    Name = "Build",
                    TargetEnvironment = "production"
                }
            }
        };

        // Act
        var result = await _deploymentPipeline.ValidateDeploymentAsync(deploymentConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ExecutePipelineAsync_Should_Throw_ArgumentNullException_For_Null_Config()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _deploymentPipeline.ExecutePipelineAsync(null!));
    }

    [Fact]
    public async Task GetDeploymentStatusAsync_Should_Return_Not_Found_Status_For_Unknown_Deployment()
    {
        // Act
        var status = await _deploymentPipeline.GetDeploymentStatusAsync("unknown-deployment");

        // Assert
        Assert.Equal("unknown-deployment", status.DeploymentId);
        Assert.Equal(DeploymentStatusValue.Failed, status.Status);
        Assert.Equal("Deployment not found", status.StatusMessage);
    }

    [Fact]
    public async Task RollbackAsync_Should_Delegate_To_RollbackManager()
    {
        // Arrange
        var rollbackConfig = new RollbackConfig
        {
            RollbackId = "rollback-123",
            DeploymentId = "deploy-123",
            Reason = "Test rollback"
        };

        var expectedResult = new RollbackResult
        {
            RollbackId = rollbackConfig.RollbackId,
            DeploymentId = rollbackConfig.DeploymentId,
            Success = true,
            Status = RollbackStatus.Succeeded
        };

        _mockRollbackManager.Setup(m => m.RollbackAsync(rollbackConfig.DeploymentId, rollbackConfig.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _deploymentPipeline.RollbackAsync(rollbackConfig);

        // Assert
        Assert.Equal(expectedResult.RollbackId, result.RollbackId);
        Assert.Equal(expectedResult.Success, result.Success);
        _mockRollbackManager.Verify(m => m.RollbackAsync(rollbackConfig.DeploymentId, rollbackConfig.Reason, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void StatusChanged_Event_Should_Be_Subscribable()
    {
        // Arrange
        DeploymentStatusChangedEventArgs? eventArgs = null;

        // Act - Test that event can be subscribed to
        _deploymentPipeline.StatusChanged += (sender, args) =>
        {
            eventArgs = args;
        };

        // Assert - Event subscription should work without errors
        // In a real implementation, triggering status changes would require 
        // executing deployments, which is tested in integration scenarios
    }

    [Fact]
    public async Task DisposeAsync_Should_Dispose_All_Components()
    {
        // Arrange - Mocks are already set up in constructor

        // Act
        await _deploymentPipeline.DisposeAsync();

        // Assert
        _mockRollbackManager.Verify(m => m.DisposeAsync(), Times.Once);
        _mockDeploymentProvider.Verify(m => m.DisposeAsync(), Times.Once);
    }
}