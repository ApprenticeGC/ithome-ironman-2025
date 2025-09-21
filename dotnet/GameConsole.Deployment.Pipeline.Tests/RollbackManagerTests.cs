using Xunit;
using GameConsole.Deployment.Pipeline;
using GameConsole.Deployment.Pipeline.Services;

namespace GameConsole.Deployment.Pipeline.Tests;

/// <summary>
/// Tests for the RollbackManager implementation.
/// </summary>
public class RollbackManagerTests
{
    private readonly RollbackManager _rollbackManager;

    public RollbackManagerTests()
    {
        _rollbackManager = new RollbackManager();
    }

    [Fact]
    public async Task InitializeAsync_Should_Load_Version_History()
    {
        // Act
        await _rollbackManager.InitializeAsync();

        // Assert
        var rollbackOptions = await _rollbackManager.GetRollbackOptionsAsync("production");
        Assert.NotEmpty(rollbackOptions);
    }

    [Fact]
    public async Task StartAsync_Should_Set_Running_State()
    {
        // Act
        await _rollbackManager.StartAsync();

        // Assert
        Assert.True(_rollbackManager.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Unset_Running_State()
    {
        // Arrange
        await _rollbackManager.StartAsync();
        Assert.True(_rollbackManager.IsRunning);

        // Act
        await _rollbackManager.StopAsync();

        // Assert
        Assert.False(_rollbackManager.IsRunning);
    }

    [Fact]
    public async Task GetRollbackOptionsAsync_Should_Return_Available_Versions()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();

        // Act
        var options = await _rollbackManager.GetRollbackOptionsAsync("production");

        // Assert
        Assert.NotEmpty(options);
        Assert.All(options, option => 
        {
            Assert.Equal("production", option.Environment);
            Assert.True(option.IsRollbackEligible);
        });
    }

    [Fact]
    public async Task GetRollbackOptionsAsync_Should_Return_Empty_For_Unknown_Environment()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();

        // Act
        var options = await _rollbackManager.GetRollbackOptionsAsync("unknown-environment");

        // Assert
        Assert.Empty(options);
    }

    [Fact]
    public async Task GetRollbackOptionsAsync_Should_Throw_ArgumentException_For_Empty_Environment()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _rollbackManager.GetRollbackOptionsAsync(""));
    }

    [Fact]
    public async Task RollbackAsync_Should_Return_Success_When_Previous_Version_Available()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();

        // Act
        var result = await _rollbackManager.RollbackAsync("deploy-123", "Test rollback");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(RollbackStatus.Succeeded, result.Status);
        Assert.Equal("deploy-123", result.DeploymentId);
        Assert.Equal("Test rollback", result.Reason);
        Assert.NotNull(result.RolledBackToVersion);
    }

    [Fact]
    public async Task RollbackAsync_Should_Throw_ArgumentException_For_Empty_DeploymentId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _rollbackManager.RollbackAsync("", "Test reason"));
    }

    [Fact]
    public async Task RollbackAsync_Should_Throw_ArgumentException_For_Empty_Reason()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _rollbackManager.RollbackAsync("deploy-123", ""));
    }

    [Fact]
    public async Task RollbackToVersionAsync_Should_Return_Success_For_Valid_Version()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();

        // Add a test version
        var testVersion = new DeploymentVersion
        {
            Version = "test-1.0.0",
            DeploymentId = "deploy-test",
            Environment = "production",
            DeployedAt = DateTime.UtcNow.AddDays(-1),
            IsRollbackEligible = true
        };
        _rollbackManager.AddVersion(testVersion);

        // Act
        var result = await _rollbackManager.RollbackToVersionAsync("deploy-123", "test-1.0.0", "Test specific rollback");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(RollbackStatus.Succeeded, result.Status);
        Assert.Equal("test-1.0.0", result.RolledBackToVersion);
    }

    [Fact]
    public async Task RollbackToVersionAsync_Should_Return_Failure_For_Unknown_Version()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();

        // Act
        var result = await _rollbackManager.RollbackToVersionAsync("deploy-123", "unknown-version", "Test rollback");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(RollbackStatus.Failed, result.Status);
        Assert.Contains("Target version 'unknown-version' not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateRollbackAsync_Should_Return_Invalid_For_Missing_DeploymentId()
    {
        // Arrange
        var rollbackConfig = new RollbackConfig
        {
            RollbackId = "rollback-123",
            DeploymentId = "",
            Reason = "Test rollback"
        };

        // Act
        var result = await _rollbackManager.ValidateRollbackAsync(rollbackConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Deployment ID is required for rollback.", result.Errors);
    }

    [Fact]
    public async Task ValidateRollbackAsync_Should_Return_Invalid_For_Missing_Reason()
    {
        // Arrange
        var rollbackConfig = new RollbackConfig
        {
            RollbackId = "rollback-123",
            DeploymentId = "deploy-123",
            Reason = ""
        };

        // Act
        var result = await _rollbackManager.ValidateRollbackAsync(rollbackConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Reason is required for rollback operations.", result.Errors);
    }

    [Fact]
    public async Task ValidateRollbackAsync_Should_Return_Valid_For_Proper_Configuration()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();
        
        var rollbackConfig = new RollbackConfig
        {
            RollbackId = "rollback-123",
            DeploymentId = "deploy-123",
            Reason = "Test rollback"
        };

        // Act
        var result = await _rollbackManager.ValidateRollbackAsync(rollbackConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task GetRollbackStatusAsync_Should_Return_Pending_For_Unknown_Rollback()
    {
        // Act
        var status = await _rollbackManager.GetRollbackStatusAsync("unknown-rollback");

        // Assert
        Assert.Equal(RollbackStatus.Pending, status);
    }

    [Fact]
    public async Task GetRollbackStatusAsync_Should_Throw_ArgumentException_For_Empty_RollbackId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _rollbackManager.GetRollbackStatusAsync(""));
    }

    [Fact]
    public async Task ConfigureAutoRollbackAsync_Should_Complete_Without_Exception()
    {
        // Arrange
        var triggers = new RollbackTriggers
        {
            OnHealthCheckFailure = true,
            OnDeploymentError = true,
            ErrorRateThreshold = 0.05
        };

        // Act & Assert - Should not throw
        await _rollbackManager.ConfigureAutoRollbackAsync(triggers);
    }

    [Fact]
    public async Task ConfigureAutoRollbackAsync_Should_Throw_ArgumentNullException_For_Null_Triggers()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _rollbackManager.ConfigureAutoRollbackAsync(null!));
    }

    [Fact]
    public async Task StatusChanged_Event_Should_Be_Raised_During_Rollback_Operations()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();
        var eventRaised = false;
        RollbackStatusChangedEventArgs? eventArgs = null;

        _rollbackManager.StatusChanged += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        await _rollbackManager.RollbackAsync("deploy-123", "Test rollback");

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(RollbackStatus.Succeeded, eventArgs.CurrentStatus);
    }

    [Fact]
    public async Task AddVersion_Should_Make_Version_Available_For_Rollback()
    {
        // Arrange
        await _rollbackManager.InitializeAsync();
        
        var newVersion = new DeploymentVersion
        {
            Version = "new-version-1.0.0",
            DeploymentId = "deploy-new",
            Environment = "staging",
            DeployedAt = DateTime.UtcNow,
            IsRollbackEligible = true
        };

        // Act
        _rollbackManager.AddVersion(newVersion);
        var options = await _rollbackManager.GetRollbackOptionsAsync("staging");

        // Assert
        Assert.Contains(options, v => v.Version == "new-version-1.0.0");
    }

    [Fact]
    public void AddVersion_Should_Throw_ArgumentNullException_For_Null_Version()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _rollbackManager.AddVersion(null!));
    }

    [Fact]
    public async Task DisposeAsync_Should_Complete_Successfully()
    {
        // Act & Assert
        await _rollbackManager.DisposeAsync();
        // No exception should be thrown
    }
}