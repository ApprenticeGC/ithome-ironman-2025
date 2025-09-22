using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Deployment.Pipeline.Tests;

public class DeploymentStageManagerTests
{
    private readonly ILogger<DeploymentStageManager> _logger;

    public DeploymentStageManagerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DeploymentStageManager>();
    }

    [Fact]
    public async Task ExecuteStageAsync_WithValidStage_ShouldReturnSuccessResult()
    {
        // Arrange
        var stageManager = new DeploymentStageManager(_logger);
        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging"
        };
        var configuration = new StageConfiguration
        {
            Stage = DeploymentStage.Build,
            Name = "Build Stage",
            TimeoutMinutes = 10
        };

        // Act
        var result = await stageManager.ExecuteStageAsync(context, DeploymentStage.Build, configuration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeploymentStage.Build, result.Stage);
        Assert.True(result.Success);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.NotEmpty(result.Logs);
        Assert.NotEmpty(result.Outputs);
    }

    [Fact]
    public async Task ValidateStageAsync_WithApprovalRequired_ShouldReturnWaitingForApproval()
    {
        // Arrange
        var stageManager = new DeploymentStageManager(_logger);
        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "production"
        };
        var configuration = new StageConfiguration
        {
            Stage = DeploymentStage.Production,
            Name = "Production Deployment",
            RequiresApproval = true,
            Approvers = new List<string> { "ops-team", "tech-lead" }
        };

        // Act
        var result = await stageManager.ValidateStageAsync(context, DeploymentStage.Production, configuration);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CanProceed);
        Assert.True(result.WaitingForApproval);
        Assert.Equal(2, result.RequiredApprovers.Count);
        Assert.Contains("ops-team", result.RequiredApprovers);
        Assert.Contains("tech-lead", result.RequiredApprovers);
    }

    [Fact]
    public async Task ValidateStageAsync_WithoutApprovalRequired_ShouldReturnCanProceed()
    {
        // Arrange
        var stageManager = new DeploymentStageManager(_logger);
        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging"
        };
        var configuration = new StageConfiguration
        {
            Stage = DeploymentStage.Staging,
            Name = "Staging Deployment",
            RequiresApproval = false
        };

        // Act
        var result = await stageManager.ValidateStageAsync(context, DeploymentStage.Staging, configuration);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanProceed);
        Assert.False(result.WaitingForApproval);
    }

    [Fact]
    public async Task RecordApprovalAsync_WithValidParameters_ShouldReturnTrue()
    {
        // Arrange
        var stageManager = new DeploymentStageManager(_logger);
        var deploymentId = Guid.NewGuid().ToString();
        var stage = DeploymentStage.Production;
        var approvedBy = "ops-team";

        // Act
        var result = await stageManager.RecordApprovalAsync(deploymentId, stage, approvedBy);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetApprovalStatusAsync_AfterRecording_ShouldReturnApprovalStatus()
    {
        // Arrange
        var stageManager = new DeploymentStageManager(_logger);
        var deploymentId = Guid.NewGuid().ToString();
        var stage = DeploymentStage.Production;
        var approvedBy = "ops-team";

        // Act
        await stageManager.RecordApprovalAsync(deploymentId, stage, approvedBy);
        var approvalStatus = await stageManager.GetApprovalStatusAsync(deploymentId, stage);

        // Assert
        Assert.NotNull(approvalStatus);
        Assert.True(approvalStatus.IsApproved);
        Assert.Equal(approvedBy, approvalStatus.ApprovedBy);
        Assert.True(approvalStatus.ApprovedAt.HasValue);
        Assert.NotNull(approvalStatus.Comments);
    }

    [Fact]
    public async Task GetApprovalStatusAsync_WithoutRecording_ShouldReturnNull()
    {
        // Arrange
        var stageManager = new DeploymentStageManager(_logger);
        var deploymentId = Guid.NewGuid().ToString();
        var stage = DeploymentStage.Production;

        // Act
        var approvalStatus = await stageManager.GetApprovalStatusAsync(deploymentId, stage);

        // Assert
        Assert.Null(approvalStatus);
    }

    [Fact]
    public async Task ExecuteStageAsync_WithValidationTests_ShouldRunTests()
    {
        // Arrange
        var stageManager = new DeploymentStageManager(_logger);
        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging"
        };
        var configuration = new StageConfiguration
        {
            Stage = DeploymentStage.Test,
            Name = "Test Stage",
            ValidationTests = new List<ValidationTest>
            {
                new ValidationTest
                {
                    Name = "Unit Tests",
                    Type = "unit",
                    IsRequired = true
                },
                new ValidationTest
                {
                    Name = "Integration Tests",
                    Type = "integration",
                    IsRequired = false
                }
            }
        };

        // Act
        var result = await stageManager.ExecuteStageAsync(context, DeploymentStage.Test, configuration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeploymentStage.Test, result.Stage);
        Assert.True(result.Success);
        Assert.Contains("Unit Tests", string.Join(" ", result.Logs));
        Assert.Contains("Integration Tests", string.Join(" ", result.Logs));
    }
}