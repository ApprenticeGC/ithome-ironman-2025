using GameConsole.Deployment.Containers;
using NUnit.Framework;

namespace GameConsole.Deployment.Containers.Tests;

/// <summary>
/// Tests for the ContainerConfiguration data types.
/// </summary>
[TestFixture]
public class ContainerConfigurationTests
{
    [Test]
    public void ContainerConfiguration_ShouldInitializeWithDefaultValues()
    {
        // Act
        var config = new ContainerConfiguration();

        // Assert
        Assert.That(config.Image, Is.EqualTo(string.Empty));
        Assert.That(config.ServiceName, Is.EqualTo(string.Empty));
        Assert.That(config.PortMappings, Is.Not.Null);
        Assert.That(config.PortMappings, Is.Empty);
        Assert.That(config.EnvironmentVariables, Is.Not.Null);
        Assert.That(config.EnvironmentVariables, Is.Empty);
        Assert.That(config.VolumeMounts, Is.Not.Null);
        Assert.That(config.VolumeMounts, Is.Empty);
        Assert.That(config.Labels, Is.Not.Null);
        Assert.That(config.Labels, Is.Empty);
        Assert.That(config.Replicas, Is.EqualTo(1));
        Assert.That(config.Strategy, Is.EqualTo(DeploymentStrategy.RollingUpdate));
        Assert.That(config.ResourceLimits, Is.Null);
        Assert.That(config.HealthCheck, Is.Null);
    }

    [Test]
    public void ContainerConfiguration_WithSyntax_ShouldWorkAsExpected()
    {
        // Arrange
        var config = new ContainerConfiguration
        {
            ServiceName = "test-service",
            Image = "nginx:latest"
        };

        // Act
        var modifiedConfig = config with { Replicas = 3 };

        // Assert
        Assert.That(modifiedConfig.ServiceName, Is.EqualTo("test-service"));
        Assert.That(modifiedConfig.Image, Is.EqualTo("nginx:latest"));
        Assert.That(modifiedConfig.Replicas, Is.EqualTo(3));
        Assert.That(config.Replicas, Is.EqualTo(1)); // Original unchanged
    }

    [Test]
    public void HealthCheckConfiguration_ShouldInitializeWithDefaultValues()
    {
        // Act
        var healthCheck = new HealthCheckConfiguration();

        // Assert
        Assert.That(healthCheck.Path, Is.EqualTo("/health"));
        Assert.That(healthCheck.Port, Is.EqualTo(80));
        Assert.That(healthCheck.Interval, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(healthCheck.Timeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(healthCheck.FailureThreshold, Is.EqualTo(3));
        Assert.That(healthCheck.SuccessThreshold, Is.EqualTo(1));
        Assert.That(healthCheck.InitialDelay, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public void ResourceLimits_ShouldInitializeWithNullValues()
    {
        // Act
        var limits = new ResourceLimits();

        // Assert
        Assert.That(limits.CpuLimit, Is.Null);
        Assert.That(limits.MemoryLimit, Is.Null);
        Assert.That(limits.CpuRequest, Is.Null);
        Assert.That(limits.MemoryRequest, Is.Null);
    }

    [Test]
    public void DeploymentResult_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new DeploymentResult();

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.DeploymentId, Is.EqualTo(string.Empty));
        Assert.That(result.ServiceName, Is.EqualTo(string.Empty));
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.Metadata, Is.Not.Null);
        Assert.That(result.Metadata, Is.Empty);
        Assert.That(result.DeployedAt, Is.GreaterThan(DateTime.MinValue));
    }

    [Test]
    public void HealthCheckResult_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new HealthCheckResult();

        // Assert
        Assert.That(result.ServiceName, Is.EqualTo(string.Empty));
        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unknown));
        Assert.That(result.Message, Is.Null);
        Assert.That(result.Details, Is.Not.Null);
        Assert.That(result.Details, Is.Empty);
        Assert.That(result.CheckedAt, Is.GreaterThan(DateTime.MinValue));
        Assert.That(result.ResponseTime, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void ServiceStatus_ShouldInitializeWithDefaultValues()
    {
        // Act
        var status = new ServiceStatus();

        // Assert
        Assert.That(status.ServiceName, Is.EqualTo(string.Empty));
        Assert.That(status.Status, Is.EqualTo(DeploymentStatus.Deploying));
        Assert.That(status.RunningInstances, Is.EqualTo(0));
        Assert.That(status.DesiredInstances, Is.EqualTo(0));
        Assert.That(status.Details, Is.Not.Null);
        Assert.That(status.Details, Is.Empty);
        Assert.That(status.LastUpdated, Is.GreaterThan(DateTime.MinValue));
    }

    [TestCase(DeploymentStrategy.RollingUpdate)]
    [TestCase(DeploymentStrategy.BlueGreen)]
    [TestCase(DeploymentStrategy.Recreate)]
    [TestCase(DeploymentStrategy.Canary)]
    public void DeploymentStrategy_ShouldSupportAllValues(DeploymentStrategy strategy)
    {
        // Act
        var config = new ContainerConfiguration { Strategy = strategy };

        // Assert
        Assert.That(config.Strategy, Is.EqualTo(strategy));
    }

    [TestCase(HealthStatus.Unknown)]
    [TestCase(HealthStatus.Healthy)]
    [TestCase(HealthStatus.Unhealthy)]
    [TestCase(HealthStatus.Degraded)]
    [TestCase(HealthStatus.Checking)]
    public void HealthStatus_ShouldSupportAllValues(HealthStatus status)
    {
        // Act
        var result = new HealthCheckResult { Status = status };

        // Assert
        Assert.That(result.Status, Is.EqualTo(status));
    }

    [TestCase(DeploymentStatus.Deploying)]
    [TestCase(DeploymentStatus.Running)]
    [TestCase(DeploymentStatus.Failed)]
    [TestCase(DeploymentStatus.Updating)]
    [TestCase(DeploymentStatus.Terminating)]
    [TestCase(DeploymentStatus.Terminated)]
    [TestCase(DeploymentStatus.Unknown)]
    public void DeploymentStatus_ShouldSupportAllValues(DeploymentStatus status)
    {
        // Act
        var serviceStatus = new ServiceStatus { Status = status };

        // Assert
        Assert.That(serviceStatus.Status, Is.EqualTo(status));
    }
}