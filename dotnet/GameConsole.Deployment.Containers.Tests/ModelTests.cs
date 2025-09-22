using GameConsole.Deployment.Containers.Models;
using Xunit;

namespace GameConsole.Deployment.Containers.Tests;

/// <summary>
/// Tests for deployment data models to verify their behavior and helper methods.
/// </summary>
public class ModelTests
{
    [Fact]
    public void DeploymentConfiguration_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var config = new DeploymentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Image);
        Assert.Equal(1, config.Replicas);
        Assert.NotNull(config.Environment);
        Assert.Empty(config.Environment);
        Assert.NotNull(config.PortMappings);
        Assert.Empty(config.PortMappings);
        Assert.NotNull(config.Labels);
        Assert.Empty(config.Labels);
        Assert.NotNull(config.Metadata);
        Assert.Empty(config.Metadata);
        Assert.Null(config.ResourceLimits);
        Assert.Null(config.HealthCheck);
    }

    [Fact]
    public void DeploymentResult_CreateSuccess_Should_Return_Successful_Result()
    {
        // Arrange
        var deploymentId = "test-deployment-123";
        var message = "Deployment created successfully";

        // Act
        var result = DeploymentResult.CreateSuccess(deploymentId, message);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(deploymentId, result.DeploymentId);
        Assert.Equal(message, result.Message);
        Assert.Null(result.Exception);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
        Assert.True(result.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be recent
    }

    [Fact]
    public void DeploymentResult_CreateSuccess_Should_Use_Default_Message_When_Null()
    {
        // Arrange
        var deploymentId = "test-deployment-123";

        // Act
        var result = DeploymentResult.CreateSuccess(deploymentId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(deploymentId, result.DeploymentId);
        Assert.Equal("Operation completed successfully", result.Message);
    }

    [Fact]
    public void DeploymentResult_CreateFailure_Should_Return_Failed_Result()
    {
        // Arrange
        var message = "Deployment failed";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = DeploymentResult.CreateFailure(message, exception);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.DeploymentId);
        Assert.Equal(message, result.Message);
        Assert.Equal(exception, result.Exception);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
        Assert.True(result.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be recent
    }

    [Fact]
    public void DeploymentStatus_IsReady_Should_Return_True_When_All_Replicas_Ready()
    {
        // Arrange
        var status = new DeploymentStatus
        {
            ReadyReplicas = 3,
            TotalReplicas = 3
        };

        // Act & Assert
        Assert.True(status.IsReady);
    }

    [Fact]
    public void DeploymentStatus_IsReady_Should_Return_False_When_Not_All_Replicas_Ready()
    {
        // Arrange
        var status = new DeploymentStatus
        {
            ReadyReplicas = 2,
            TotalReplicas = 3
        };

        // Act & Assert
        Assert.False(status.IsReady);
    }

    [Fact]
    public void DeploymentStatus_IsReady_Should_Return_False_When_No_Replicas()
    {
        // Arrange
        var status = new DeploymentStatus
        {
            ReadyReplicas = 0,
            TotalReplicas = 0
        };

        // Act & Assert
        Assert.False(status.IsReady);
    }

    [Fact]
    public void DeploymentStatus_IsHealthy_Should_Return_True_For_Healthy_Status()
    {
        // Arrange
        var status = new DeploymentStatus
        {
            HealthStatus = "Healthy"
        };

        // Act & Assert
        Assert.True(status.IsHealthy);
    }

    [Fact]
    public void DeploymentStatus_IsHealthy_Should_Return_False_For_Unhealthy_Status()
    {
        // Arrange
        var status = new DeploymentStatus
        {
            HealthStatus = "Unhealthy"
        };

        // Act & Assert
        Assert.False(status.IsHealthy);
    }

    [Fact]
    public void HealthCheckResult_Healthy_Should_Create_Healthy_Result()
    {
        // Arrange
        var message = "All systems operational";
        var responseTime = TimeSpan.FromMilliseconds(100);

        // Act
        var result = HealthCheckResult.Healthy(message, responseTime);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("Healthy", result.Status);
        Assert.Equal(message, result.Message);
        Assert.Equal(responseTime, result.ResponseTime);
        Assert.True(result.CheckTime <= DateTime.UtcNow);
    }

    [Fact]
    public void HealthCheckResult_Unhealthy_Should_Create_Unhealthy_Result()
    {
        // Arrange
        var message = "Service unavailable";
        var responseTime = TimeSpan.FromMilliseconds(5000);

        // Act
        var result = HealthCheckResult.Unhealthy(message, responseTime);

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal("Unhealthy", result.Status);
        Assert.Equal(message, result.Message);
        Assert.Equal(responseTime, result.ResponseTime);
    }

    [Fact]
    public void HealthCheckResult_Degraded_Should_Create_Degraded_Result()
    {
        // Arrange
        var message = "Performance degraded";
        var responseTime = TimeSpan.FromMilliseconds(3000);

        // Act
        var result = HealthCheckResult.Degraded(message, responseTime);

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal("Degraded", result.Status);
        Assert.Equal(message, result.Message);
        Assert.Equal(responseTime, result.ResponseTime);
    }

    [Fact]
    public void HealthCheckConfiguration_Should_Have_Default_Values()
    {
        // Arrange & Act
        var config = new HealthCheckConfiguration();

        // Assert
        Assert.Equal("/health", config.Path);
        Assert.Equal(80, config.Port);
        Assert.Equal(TimeSpan.FromSeconds(30), config.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), config.Interval);
        Assert.Equal(TimeSpan.FromSeconds(5), config.Timeout);
        Assert.Equal(3, config.FailureThreshold);
    }

    [Fact]
    public void ResourceLimits_Should_Allow_Null_Values()
    {
        // Arrange & Act
        var limits = new ResourceLimits();

        // Assert
        Assert.Null(limits.CpuLimit);
        Assert.Null(limits.MemoryLimit);
        Assert.Null(limits.CpuRequest);
        Assert.Null(limits.MemoryRequest);
    }

    [Fact]
    public void ContainerHealthEvent_Should_Initialize_With_Current_Timestamp()
    {
        // Arrange & Act
        var healthEvent = new ContainerHealthEvent();

        // Assert
        Assert.True(healthEvent.Timestamp <= DateTime.UtcNow);
        Assert.True(healthEvent.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be recent
    }
}