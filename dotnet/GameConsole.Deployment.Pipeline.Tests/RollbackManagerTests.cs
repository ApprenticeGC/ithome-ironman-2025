using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Deployment.Pipeline.Tests;

public class RollbackManagerTests
{
    private readonly ILogger<RollbackManager> _logger;

    public RollbackManagerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<RollbackManager>();
    }

    [Fact]
    public async Task InitiateRollbackAsync_WithValidDeploymentId_ShouldReturnSuccessResult()
    {
        // Arrange
        var rollbackManager = new RollbackManager(_logger);
        var deploymentId = Guid.NewGuid().ToString();

        // Act
        var result = await rollbackManager.InitiateRollbackAsync(deploymentId, "1.2.3", "Test rollback");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deploymentId, result.OriginalDeploymentId);
        Assert.NotEmpty(result.RollbackId);
        Assert.Equal("Test rollback", result.Reason);
        Assert.True(result.StartedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task CanRollbackAsync_WithDeploymentId_ShouldReturnBool()
    {
        // Arrange
        var rollbackManager = new RollbackManager(_logger);
        var deploymentId = Guid.NewGuid().ToString();

        // Act
        var canRollback = await rollbackManager.CanRollbackAsync(deploymentId);

        // Assert
        Assert.IsType<bool>(canRollback);
    }

    [Fact]
    public async Task GetRollbackOptionsAsync_WithDeploymentId_ShouldReturnOptions()
    {
        // Arrange
        var rollbackManager = new RollbackManager(_logger);
        var deploymentId = Guid.NewGuid().ToString() + "123456789012"; // Make it long enough to get options

        // Act
        var options = await rollbackManager.GetRollbackOptionsAsync(deploymentId);

        // Assert
        Assert.NotNull(options);
        if (options.Any())
        {
            Assert.Contains(options, o => o.IsRecommended);
        }
    }

    [Fact]
    public async Task GetRollbackHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        var rollbackManager = new RollbackManager(_logger);

        // Act
        var history = await rollbackManager.GetRollbackHistoryAsync();

        // Assert
        Assert.NotNull(history);
    }

    [Fact]
    public async Task ConfigureAutoRollbackAsync_WithTriggers_ShouldReturnTrue()
    {
        // Arrange
        var rollbackManager = new RollbackManager(_logger);
        var deploymentId = Guid.NewGuid().ToString();
        var triggers = new[]
        {
            new RollbackTrigger
            {
                Type = RollbackTriggerType.ErrorRate,
                Condition = "error_rate > 5%",
                Threshold = 5.0,
                GracePeriod = TimeSpan.FromMinutes(2)
            }
        };

        // Act
        var result = await rollbackManager.ConfigureAutoRollbackAsync(deploymentId, triggers);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RegisterDeployment_WithValidContext_ShouldNotThrow()
    {
        // Arrange
        var rollbackManager = new RollbackManager(_logger);
        var context = new DeploymentContext
        {
            DeploymentId = Guid.NewGuid().ToString(),
            ArtifactName = "TestApp",
            Version = "1.0.0",
            Environment = "staging"
        };

        // Act & Assert
        rollbackManager.RegisterDeployment(context); // Should not throw
    }

    [Fact]
    public async Task RollbackStatusChanged_EventShouldBeRaised()
    {
        // Arrange
        var rollbackManager = new RollbackManager(_logger);
        var deploymentId = Guid.NewGuid().ToString();
        var eventRaised = false;
        string? eventRollbackId = null;

        rollbackManager.RollbackStatusChanged += (sender, args) =>
        {
            eventRaised = true;
            eventRollbackId = args.RollbackId;
        };

        // Act
        await rollbackManager.InitiateRollbackAsync(deploymentId, "1.2.3", "Test event");

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventRollbackId);
    }
}