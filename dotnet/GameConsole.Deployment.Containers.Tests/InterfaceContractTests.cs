using GameConsole.Deployment.Containers.Interfaces;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Deployment.Containers.Tests;

/// <summary>
/// Tests to verify the interface contracts for the deployment system.
/// Ensures interfaces follow GameConsole architecture patterns.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IContainerOrchestrator_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var orchestratorType = typeof(IContainerOrchestrator);

        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(orchestratorType));
    }

    [Fact]
    public void IDeploymentProvider_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var providerType = typeof(IDeploymentProvider);

        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(providerType));
    }

    [Fact]
    public void IContainerHealthMonitor_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var monitorType = typeof(IContainerHealthMonitor);

        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(monitorType));
    }

    [Fact]
    public void IContainerOrchestrator_Should_Have_Required_Methods()
    {
        // Arrange
        var orchestratorType = typeof(IContainerOrchestrator);

        // Act & Assert - Check for required methods
        var deployMethod = orchestratorType.GetMethod("DeployAsync");
        Assert.NotNull(deployMethod);
        Assert.Equal(typeof(Task<>).MakeGenericType(typeof(GameConsole.Deployment.Containers.Models.DeploymentResult)), deployMethod.ReturnType);

        var scaleMethod = orchestratorType.GetMethod("ScaleAsync");
        Assert.NotNull(scaleMethod);

        var getStatusMethod = orchestratorType.GetMethod("GetStatusAsync");
        Assert.NotNull(getStatusMethod);

        var removeMethod = orchestratorType.GetMethod("RemoveAsync");
        Assert.NotNull(removeMethod);

        var listMethod = orchestratorType.GetMethod("ListDeploymentsAsync");
        Assert.NotNull(listMethod);

        var updateMethod = orchestratorType.GetMethod("UpdateAsync");
        Assert.NotNull(updateMethod);
    }

    [Fact]
    public void IDeploymentProvider_Should_Have_Required_Methods()
    {
        // Arrange
        var providerType = typeof(IDeploymentProvider);

        // Act & Assert - Check for required methods
        var providerTypeProperty = providerType.GetProperty("ProviderType");
        Assert.NotNull(providerTypeProperty);
        Assert.Equal(typeof(string), providerTypeProperty.PropertyType);
        Assert.True(providerTypeProperty.CanRead);

        var createMethod = providerType.GetMethod("CreateDeploymentAsync");
        Assert.NotNull(createMethod);

        var updateMethod = providerType.GetMethod("UpdateDeploymentAsync");
        Assert.NotNull(updateMethod);

        var deleteMethod = providerType.GetMethod("DeleteDeploymentAsync");
        Assert.NotNull(deleteMethod);

        var getStatusMethod = providerType.GetMethod("GetDeploymentStatusAsync");
        Assert.NotNull(getStatusMethod);

        var scaleMethod = providerType.GetMethod("ScaleDeploymentAsync");
        Assert.NotNull(scaleMethod);

        var listMethod = providerType.GetMethod("ListDeploymentsAsync");
        Assert.NotNull(listMethod);

        var supportsMethod = providerType.GetMethod("SupportsConfiguration");
        Assert.NotNull(supportsMethod);
        Assert.Equal(typeof(bool), supportsMethod.ReturnType);

        var capabilitiesMethod = providerType.GetMethod("GetCapabilitiesAsync");
        Assert.NotNull(capabilitiesMethod);
    }

    [Fact]
    public void IContainerHealthMonitor_Should_Have_Required_Methods()
    {
        // Arrange
        var monitorType = typeof(IContainerHealthMonitor);

        // Act & Assert - Check for required methods
        var checkHealthMethod = monitorType.GetMethod("CheckHealthAsync");
        Assert.NotNull(checkHealthMethod);

        var startMonitoringMethod = monitorType.GetMethod("StartMonitoringAsync");
        Assert.NotNull(startMonitoringMethod);

        var stopMonitoringMethod = monitorType.GetMethod("StopMonitoringAsync");
        Assert.NotNull(stopMonitoringMethod);

        var getAllHealthMethod = monitorType.GetMethod("GetAllHealthStatusAsync");
        Assert.NotNull(getAllHealthMethod);

        var configureHealthMethod = monitorType.GetMethod("ConfigureHealthCheckAsync");
        Assert.NotNull(configureHealthMethod);

        var getHistoryMethod = monitorType.GetMethod("GetHealthHistoryAsync");
        Assert.NotNull(getHistoryMethod);
    }

    [Fact]
    public void IContainerHealthMonitor_Should_Have_Required_Events()
    {
        // Arrange
        var monitorType = typeof(IContainerHealthMonitor);

        // Act & Assert - Check for required events
        var healthChangedEvent = monitorType.GetEvent("HealthChanged");
        Assert.NotNull(healthChangedEvent);

        var healthCheckFailedEvent = monitorType.GetEvent("HealthCheckFailed");
        Assert.NotNull(healthCheckFailedEvent);

        var healthCheckRecoveredEvent = monitorType.GetEvent("HealthCheckRecovered");
        Assert.NotNull(healthCheckRecoveredEvent);
    }

    [Fact]
    public void IContainerOrchestrator_Should_Have_Required_Events()
    {
        // Arrange
        var orchestratorType = typeof(IContainerOrchestrator);

        // Act & Assert - Check for required events
        var deploymentStatusChangedEvent = orchestratorType.GetEvent("DeploymentStatusChanged");
        Assert.NotNull(deploymentStatusChangedEvent);
    }
}