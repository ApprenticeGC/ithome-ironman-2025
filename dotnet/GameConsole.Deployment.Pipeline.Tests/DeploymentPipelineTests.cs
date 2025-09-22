using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Deployment.Pipeline.Tests;

public class DeploymentPipelineTests
{
    private readonly ILogger<DeploymentPipeline> _logger;
    private readonly ILogger<DeploymentStageManager> _stageManagerLogger;
    private readonly ILogger<RollbackManager> _rollbackManagerLogger;
    private readonly ILogger<GitHubActionsPipelineProvider> _ciProviderLogger;

    public DeploymentPipelineTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DeploymentPipeline>();
        _stageManagerLogger = loggerFactory.CreateLogger<DeploymentStageManager>();
        _rollbackManagerLogger = loggerFactory.CreateLogger<RollbackManager>();
        _ciProviderLogger = loggerFactory.CreateLogger<GitHubActionsPipelineProvider>();
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();

        // Act & Assert
        await pipeline.InitializeAsync();
        Assert.False(pipeline.IsRunning);
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();
        await pipeline.InitializeAsync();

        // Act
        await pipeline.StartAsync();

        // Assert
        Assert.True(pipeline.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();
        await pipeline.InitializeAsync();
        await pipeline.StartAsync();

        // Act
        await pipeline.StopAsync();

        // Assert
        Assert.False(pipeline.IsRunning);
    }

    [Fact]
    public async Task DeployAsync_WithValidContext_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();
        await pipeline.InitializeAsync();
        await pipeline.StartAsync();

        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging",
            Strategy = DeploymentStrategy.Rolling,
            InitiatedBy = "test-user"
        };

        // Act
        var result = await pipeline.DeployAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(context.DeploymentId, result.Context.DeploymentId);
        Assert.Equal(DeploymentStatus.Succeeded, result.Status);
        Assert.True(result.Duration.HasValue);
        Assert.NotEmpty(result.Logs);
        Assert.NotEmpty(result.Metrics);
    }

    [Fact]
    public async Task DeployAsync_WhenNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();
        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.DeployAsync(context));
    }

    [Fact]
    public async Task GetDeploymentStatusAsync_WithValidDeploymentId_ShouldReturnStatus()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();
        await pipeline.InitializeAsync();
        await pipeline.StartAsync();

        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging"
        };

        // Act
        var deployTask = pipeline.DeployAsync(context);
        var status = await pipeline.GetDeploymentStatusAsync(context.DeploymentId);
        var result = await deployTask;

        // Assert
        Assert.NotNull(status);
        Assert.Equal(context.DeploymentId, status.Context.DeploymentId);
    }

    [Fact]
    public async Task GetDeploymentStatusAsync_WithInvalidDeploymentId_ShouldReturnNull()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();

        // Act
        var status = await pipeline.GetDeploymentStatusAsync("invalid-id");

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task CancelDeploymentAsync_WithActiveDeployment_ShouldReturnTrue()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();
        await pipeline.InitializeAsync();
        await pipeline.StartAsync();

        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging"
        };

        // Start deployment and immediately cancel
        var deployTask = Task.Run(() => pipeline.DeployAsync(context));
        await Task.Delay(100); // Give deployment time to start

        // Act
        var cancelled = await pipeline.CancelDeploymentAsync(context.DeploymentId);

        // Assert
        Assert.True(cancelled);
    }

    [Fact]
    public async Task GetDeploymentHistoryAsync_ShouldReturnDeploymentResults()
    {
        // Arrange
        var pipeline = CreateDeploymentPipeline();
        await pipeline.InitializeAsync();
        await pipeline.StartAsync();

        // Act
        var history = await pipeline.GetDeploymentHistoryAsync();

        // Assert
        Assert.NotNull(history);
    }

    private DeploymentPipeline CreateDeploymentPipeline()
    {
        var stageManager = new DeploymentStageManager(_stageManagerLogger);
        var rollbackManager = new RollbackManager(_rollbackManagerLogger);
        var ciProvider = new GitHubActionsPipelineProvider(_ciProviderLogger, new HttpClient());

        return new DeploymentPipeline(_logger, stageManager, rollbackManager, ciProvider);
    }
}