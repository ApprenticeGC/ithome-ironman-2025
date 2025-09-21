using GameConsole.Deployment.Containers.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameConsole.Deployment.Containers.Tests;

public class DockerDeploymentProviderTests
{
    private readonly Mock<ILogger<DockerDeploymentProvider>> _mockLogger;
    private readonly DockerDeploymentProvider _provider;

    public DockerDeploymentProviderTests()
    {
        _mockLogger = new Mock<ILogger<DockerDeploymentProvider>>();
        _provider = new DockerDeploymentProvider(_mockLogger.Object);
    }

    [Fact]
    public void ProviderName_ShouldReturnDocker()
    {
        Assert.Equal("Docker", _provider.ProviderName);
    }

    [Fact]
    public void SupportedFeatures_ShouldIncludeExpectedFeatures()
    {
        var features = _provider.SupportedFeatures;
        
        Assert.Contains("ContainerDeployment", features);
        Assert.Contains("AutomatedBuilds", features);
        Assert.Contains("PortMapping", features);
        Assert.Contains("VolumeMount", features);
        Assert.Contains("EnvironmentVariables", features);
        Assert.Contains("ResourceLimits", features);
        Assert.Contains("HealthChecks", features);
        Assert.Contains("LogStreaming", features);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSucceed()
    {
        await _provider.InitializeAsync();
        
        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initializing DockerDeploymentProvider")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        Assert.True(_provider.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
    {
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        await _provider.StopAsync();
        
        Assert.False(_provider.IsRunning);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidConfiguration_ShouldReturnValid()
    {
        var configuration = CreateValidDeploymentConfiguration();
        
        var result = await _provider.ValidateConfigurationAsync(configuration);
        
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithMissingImage_ShouldReturnInvalid()
    {
        var configuration = CreateValidDeploymentConfiguration();
        configuration = configuration with { Image = "" };
        
        var result = await _provider.ValidateConfigurationAsync(configuration);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Property == nameof(DeploymentConfiguration.Image));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidReplicas_ShouldReturnInvalid()
    {
        var configuration = CreateValidDeploymentConfiguration();
        configuration = configuration with { Replicas = 0 };
        
        var result = await _provider.ValidateConfigurationAsync(configuration);
        
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Property == nameof(DeploymentConfiguration.Replicas));
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithMultipleReplicas_ShouldIncludeWarning()
    {
        var configuration = CreateValidDeploymentConfiguration();
        configuration = configuration with { Replicas = 3 };
        
        var result = await _provider.ValidateConfigurationAsync(configuration);
        
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Property == nameof(DeploymentConfiguration.Replicas));
    }

    [Fact]
    public async Task DeployAsync_WithValidConfiguration_ShouldSucceed()
    {
        var configuration = CreateValidDeploymentConfiguration();
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        var result = await _provider.DeployAsync(configuration);
        
        Assert.True(result.Success);
        Assert.Equal(configuration.Id, result.DeploymentId);
        Assert.NotEmpty(result.Message);
        Assert.NotEmpty(result.Endpoints);
    }

    [Fact]
    public async Task ScaleAsync_ExistingDeployment_ShouldSucceed()
    {
        var configuration = CreateValidDeploymentConfiguration();
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        // First deploy
        await _provider.DeployAsync(configuration);
        
        // Then scale
        var result = await _provider.ScaleAsync(configuration.Id, 3);
        
        Assert.True(result.Success);
        Assert.Equal(configuration.Id, result.DeploymentId);
        Assert.Equal(1, result.PreviousReplicas);
        Assert.Equal(3, result.TargetReplicas);
        Assert.Equal(3, result.CurrentReplicas);
    }

    [Fact]
    public async Task ScaleAsync_NonExistentDeployment_ShouldFail()
    {
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        var result = await _provider.ScaleAsync("non-existent", 3);
        
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task RemoveAsync_ExistingDeployment_ShouldSucceed()
    {
        var configuration = CreateValidDeploymentConfiguration();
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        // First deploy
        await _provider.DeployAsync(configuration);
        
        // Then remove
        var result = await _provider.RemoveAsync(configuration.Id);
        
        Assert.True(result.Success);
        Assert.Contains("removed successfully", result.Message);
    }

    [Fact]
    public async Task GetStatusAsync_ExistingDeployment_ShouldReturnStatus()
    {
        var configuration = CreateValidDeploymentConfiguration();
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        // First deploy
        await _provider.DeployAsync(configuration);
        
        // Then get status
        var status = await _provider.GetStatusAsync(configuration.Id);
        
        Assert.Equal(configuration.Id, status.DeploymentId);
        Assert.Equal(DeploymentPhase.Complete, status.Phase);
        Assert.Equal(configuration.Replicas, status.DesiredReplicas);
        Assert.Equal(HealthState.Healthy, status.HealthStatus.Status);
    }

    [Fact]
    public async Task GetStatusAsync_NonExistentDeployment_ShouldThrowException()
    {
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _provider.GetStatusAsync("non-existent"));
    }

    [Fact]
    public async Task ListDeploymentsAsync_WithDeployments_ShouldReturnList()
    {
        var configuration1 = CreateValidDeploymentConfiguration("app1");
        var configuration2 = CreateValidDeploymentConfiguration("app2");
        
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        await _provider.DeployAsync(configuration1);
        await _provider.DeployAsync(configuration2);
        
        var deployments = await _provider.ListDeploymentsAsync();
        
        Assert.Equal(2, deployments.Count());
        Assert.Contains(deployments, d => d.Id == "app1");
        Assert.Contains(deployments, d => d.Id == "app2");
    }

    [Fact]
    public async Task GetLogsAsync_ExistingDeployment_ShouldReturnLogs()
    {
        var configuration = CreateValidDeploymentConfiguration();
        await _provider.InitializeAsync();
        await _provider.StartAsync();
        
        await _provider.DeployAsync(configuration);
        
        var logs = await _provider.GetLogsAsync(configuration.Id);
        
        Assert.NotEmpty(logs);
        Assert.All(logs, log => Assert.NotNull(log.Message));
        Assert.All(logs, log => Assert.NotNull(log.Level));
    }

    private static DeploymentConfiguration CreateValidDeploymentConfiguration(string? id = null)
    {
        return new DeploymentConfiguration
        {
            Id = id ?? "test-deployment",
            Name = "test-app",
            Image = "nginx",
            Tag = "latest",
            Replicas = 1,
            Ports = new List<PortMapping>
            {
                new() { ContainerPort = 80, HostPort = 8080, Protocol = "TCP" }
            },
            Environment = new Dictionary<string, string>
            {
                { "ENV", "test" }
            },
            Resources = new ResourceConfiguration
            {
                CpuLimit = 1000,
                MemoryLimit = 1024 * 1024 * 1024 // 1GB
            }
        };
    }
}