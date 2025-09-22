using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace GameConsole.Deployment.Pipeline.Tests;

public class DeploymentPipelineIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public DeploymentPipelineIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task FullDeploymentWorkflow_ShouldCompleteSuccessfully()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(new XunitLoggerProvider(_output)));

        var pipeline = CreateDeploymentPipeline(loggerFactory);

        await pipeline.InitializeAsync();
        await pipeline.StartAsync();

        var deploymentId = Guid.NewGuid().ToString();
        var context = new DeploymentContext
        {
            DeploymentId = deploymentId,
            ArtifactName = "GameConsole.TestApp",
            Version = "2.1.0",
            Environment = "staging",
            Strategy = DeploymentStrategy.Rolling,
            InitiatedBy = "integration-test",
            Configuration = new Dictionary<string, object>
            {
                ["replicas"] = 3,
                ["healthCheck"] = true
            }
        };

        var statusChangedEvents = new List<DeploymentStatusChangedEventArgs>();
        pipeline.DeploymentStatusChanged += (sender, args) => statusChangedEvents.Add(args);

        // Act
        var result = await pipeline.DeployAsync(context);

        // Assert - Deployment Result
        Assert.NotNull(result);
        Assert.Equal(DeploymentStatus.Succeeded, result.Status);
        Assert.Equal(deploymentId, result.Context.DeploymentId);
        Assert.Equal("GameConsole.TestApp", result.Context.ArtifactName);
        Assert.Equal("2.1.0", result.Context.Version);
        Assert.Equal("staging", result.Context.Environment);
        
        // Assert - Timing
        Assert.True(result.Duration.HasValue);
        Assert.True(result.Duration.Value > TimeSpan.Zero);
        Assert.True(result.StartedAt < result.CompletedAt);

        // Assert - Logs and Artifacts
        Assert.NotEmpty(result.Logs);
        Assert.NotEmpty(result.Artifacts);
        Assert.NotEmpty(result.Metrics);

        // Verify stage completion metrics exist
        Assert.Contains("stage_validation_duration_ms", result.Metrics.Keys);
        Assert.Contains("stage_build_duration_ms", result.Metrics.Keys);
        Assert.Contains("stage_test_duration_ms", result.Metrics.Keys);

        // Assert - Status Change Events
        Assert.NotEmpty(statusChangedEvents);
        Assert.Contains(statusChangedEvents, e => e.CurrentStatus == DeploymentStatus.InProgress);
        Assert.Contains(statusChangedEvents, e => e.CurrentStatus == DeploymentStatus.Succeeded);

        // Verify deployment can be retrieved
        var retrievedDeployment = await pipeline.GetDeploymentStatusAsync(deploymentId);
        Assert.NotNull(retrievedDeployment);
        Assert.Equal(DeploymentStatus.Succeeded, retrievedDeployment.Status);

        // Cleanup
        await pipeline.StopAsync();
    }

    [Fact]
    public async Task DeploymentWithApprovalGates_ShouldHandleApprovals()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(new XunitLoggerProvider(_output)));

        var pipeline = CreateDeploymentPipeline(loggerFactory);
        await pipeline.InitializeAsync();
        await pipeline.StartAsync();

        var deploymentId = Guid.NewGuid().ToString();
        var context = new DeploymentContext
        {
            DeploymentId = deploymentId,
            ArtifactName = "GameConsole.ProductionApp",
            Version = "1.5.2",
            Environment = "production",
            Strategy = DeploymentStrategy.BlueGreen,
            InitiatedBy = "ops-team"
        };

        // Act
        var deployTask = pipeline.DeployAsync(context);
        
        // Give some time for deployment to reach approval gate
        await Task.Delay(2000);
        
        // Approve the UAT stage
        var uatApproval = await pipeline.ApproveStageAsync(deploymentId, DeploymentStage.UAT, "qa-lead");
        Assert.True(uatApproval);
        
        // Approve the production stage
        var prodApproval = await pipeline.ApproveStageAsync(deploymentId, DeploymentStage.Production, "ops-manager");
        Assert.True(prodApproval);

        var result = await deployTask;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DeploymentStatus.Succeeded, result.Status);
        
        // Cleanup
        await pipeline.StopAsync();
    }

    [Fact]
    public async Task RollbackWorkflow_ShouldCompleteSuccessfully()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(new XunitLoggerProvider(_output)));

        var rollbackManager = new RollbackManager(
            loggerFactory.CreateLogger<RollbackManager>());

        var deploymentId = Guid.NewGuid().ToString();
        var deploymentContext = new DeploymentContext
        {
            DeploymentId = deploymentId,
            ArtifactName = "GameConsole.TestApp",
            Version = "2.0.0",
            Environment = "production"
        };

        // Register deployment for rollback history
        rollbackManager.RegisterDeployment(deploymentContext);

        var rollbackEvents = new List<RollbackStatusChangedEventArgs>();
        rollbackManager.RollbackStatusChanged += (sender, args) => rollbackEvents.Add(args);

        // Act
        var rollbackResult = await rollbackManager.InitiateRollbackAsync(
            deploymentId, "1.9.0", "Critical bug found in production");

        // Assert
        Assert.NotNull(rollbackResult);
        Assert.Equal(deploymentId, rollbackResult.OriginalDeploymentId);
        Assert.NotEmpty(rollbackResult.RollbackId);
        Assert.Equal("1.9.0", rollbackResult.RolledBackToVersion);
        Assert.Equal("Critical bug found in production", rollbackResult.Reason);
        Assert.True(rollbackResult.Success);
        Assert.True(rollbackResult.Duration.HasValue);

        // Verify rollback events were raised
        Assert.NotEmpty(rollbackEvents);
        Assert.Contains(rollbackEvents, e => e.Status == "Starting");
        Assert.Contains(rollbackEvents, e => e.Status == "Completed");
    }

    private DeploymentPipeline CreateDeploymentPipeline(ILoggerFactory loggerFactory)
    {
        var stageManager = new DeploymentStageManager(
            loggerFactory.CreateLogger<DeploymentStageManager>());
        var rollbackManager = new RollbackManager(
            loggerFactory.CreateLogger<RollbackManager>());
        var ciProvider = new GitHubActionsPipelineProvider(
            loggerFactory.CreateLogger<GitHubActionsPipelineProvider>(), 
            new HttpClient());

        return new DeploymentPipeline(
            loggerFactory.CreateLogger<DeploymentPipeline>(),
            stageManager, rollbackManager, ciProvider);
    }
}

// Helper class for XUnit logging
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_testOutputHelper, categoryName);
    }

    public void Dispose() { }
}

public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
    {
        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            _testOutputHelper.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
        }
        catch
        {
            // Ignore logging errors in tests
        }
    }
}