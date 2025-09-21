using GameConsole.Deployment.Containers.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameConsole.Deployment.Containers.Tests;

public class ContainerOrchestratorServiceTests
{
    private readonly Mock<ILogger<ContainerOrchestratorService>> _mockLogger;
    private readonly ContainerOrchestratorService _orchestrator;
    private readonly Mock<IDeploymentProvider> _mockDockerProvider;
    private readonly Mock<IDeploymentProvider> _mockKubernetesProvider;

    public ContainerOrchestratorServiceTests()
    {
        _mockLogger = new Mock<ILogger<ContainerOrchestratorService>>();
        _orchestrator = new ContainerOrchestratorService(_mockLogger.Object);
        
        _mockDockerProvider = new Mock<IDeploymentProvider>();
        _mockDockerProvider.SetupGet(x => x.ProviderName).Returns("Docker");
        _mockDockerProvider.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        
        _mockKubernetesProvider = new Mock<IDeploymentProvider>();
        _mockKubernetesProvider.SetupGet(x => x.ProviderName).Returns("Kubernetes");
        _mockKubernetesProvider.Setup(x => x.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public void RegisterProvider_ShouldAddProvider()
    {
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registered deployment provider: Docker")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterProvider_NullProvider_ShouldThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            _orchestrator.RegisterProvider(null!);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task InitializeAsync_ShouldInitializeAllProviders()
    {
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        _orchestrator.RegisterProvider(_mockKubernetesProvider.Object);
        
        await _orchestrator.InitializeAsync();
        
        _mockDockerProvider.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockKubernetesProvider.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldStartAllProviders()
    {
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        _orchestrator.RegisterProvider(_mockKubernetesProvider.Object);
        
        await _orchestrator.InitializeAsync();
        await _orchestrator.StartAsync();
        
        Assert.True(_orchestrator.IsRunning);
        _mockDockerProvider.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockKubernetesProvider.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopAllProviders()
    {
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        await _orchestrator.InitializeAsync();
        await _orchestrator.StartAsync();
        await _orchestrator.StopAsync();
        
        Assert.False(_orchestrator.IsRunning);
        _mockDockerProvider.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeployAsync_WithValidConfiguration_ShouldUseAppropriateProvider()
    {
        var configuration = CreateValidDeploymentConfiguration();
        var expectedResult = new DeploymentResult
        {
            DeploymentId = configuration.Id,
            Success = true,
            Message = "Deployment successful"
        };
        
        _mockDockerProvider.Setup(x => x.ValidateConfigurationAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockDockerProvider.Setup(x => x.DeployAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        await _orchestrator.InitializeAsync();
        await _orchestrator.StartAsync();
        
        var result = await _orchestrator.DeployAsync(configuration);
        
        Assert.True(result.Success);
        Assert.Equal(configuration.Id, result.DeploymentId);
        _mockDockerProvider.Verify(x => x.DeployAsync(configuration, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeployAsync_WithMultipleReplicas_ShouldPreferKubernetes()
    {
        var configuration = CreateValidDeploymentConfiguration();
        configuration = configuration with { Replicas = 3 };
        
        var expectedResult = new DeploymentResult
        {
            DeploymentId = configuration.Id,
            Success = true,
            Message = "Kubernetes deployment successful"
        };
        
        _mockKubernetesProvider.Setup(x => x.ValidateConfigurationAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockKubernetesProvider.Setup(x => x.DeployAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        _orchestrator.RegisterProvider(_mockKubernetesProvider.Object);
        await _orchestrator.InitializeAsync();
        await _orchestrator.StartAsync();
        
        var result = await _orchestrator.DeployAsync(configuration);
        
        Assert.True(result.Success);
        _mockKubernetesProvider.Verify(x => x.DeployAsync(configuration, It.IsAny<CancellationToken>()), Times.Once);
        _mockDockerProvider.Verify(x => x.DeployAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeployAsync_WhenNotRunning_ShouldThrowInvalidOperationException()
    {
        var configuration = CreateValidDeploymentConfiguration();
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => _orchestrator.DeployAsync(configuration));
    }

    [Fact]
    public async Task DeployAsync_WithInvalidConfiguration_ShouldReturnFailure()
    {
        var configuration = CreateValidDeploymentConfiguration();
        var validationResult = ValidationResult.Failure(
            new ValidationError { Property = "Image", Message = "Image is required" });
        
        _mockDockerProvider.Setup(x => x.ValidateConfigurationAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        await _orchestrator.InitializeAsync();
        await _orchestrator.StartAsync();
        
        var result = await _orchestrator.DeployAsync(configuration);
        
        Assert.False(result.Success);
        Assert.Contains("validation failed", result.Message);
    }

    [Fact]
    public async Task ScaleAsync_ExistingDeployment_ShouldSucceed()
    {
        var configuration = CreateValidDeploymentConfiguration();
        var deployResult = new DeploymentResult
        {
            DeploymentId = configuration.Id,
            Success = true,
            Message = "Deployed"
        };
        var scaleResult = new ScalingResult
        {
            DeploymentId = configuration.Id,
            Success = true,
            PreviousReplicas = 1,
            TargetReplicas = 3,
            CurrentReplicas = 3,
            Message = "Scaled successfully"
        };
        
        _mockDockerProvider.Setup(x => x.ValidateConfigurationAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockDockerProvider.Setup(x => x.DeployAsync(It.IsAny<DeploymentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployResult);
        _mockDockerProvider.Setup(x => x.ScaleAsync(configuration.Id, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scaleResult);
        
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        await _orchestrator.InitializeAsync();
        await _orchestrator.StartAsync();
        
        // Deploy first
        await _orchestrator.DeployAsync(configuration);
        
        // Then scale
        var result = await _orchestrator.ScaleAsync(configuration.Id, 3);
        
        Assert.True(result.Success);
        Assert.Equal(3, result.CurrentReplicas);
        _mockDockerProvider.Verify(x => x.ScaleAsync(configuration.Id, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListDeploymentsAsync_ShouldAggregateFromAllProviders()
    {
        var dockerDeployments = new List<DeploymentInfo>
        {
            new() { Id = "docker-app", Name = "docker-app", Image = "nginx", Phase = DeploymentPhase.Complete, DesiredReplicas = 1, ReadyReplicas = 1 }
        };
        
        var k8sDeployments = new List<DeploymentInfo>
        {
            new() { Id = "k8s-app", Name = "k8s-app", Image = "redis", Phase = DeploymentPhase.Complete, DesiredReplicas = 2, ReadyReplicas = 2 }
        };
        
        _mockDockerProvider.Setup(x => x.ListDeploymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dockerDeployments);
        _mockKubernetesProvider.Setup(x => x.ListDeploymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(k8sDeployments);
        
        _orchestrator.RegisterProvider(_mockDockerProvider.Object);
        _orchestrator.RegisterProvider(_mockKubernetesProvider.Object);
        await _orchestrator.InitializeAsync();
        await _orchestrator.StartAsync();
        
        var allDeployments = await _orchestrator.ListDeploymentsAsync();
        
        Assert.Equal(2, allDeployments.Count());
        Assert.Contains(allDeployments, d => d.Id == "docker-app");
        Assert.Contains(allDeployments, d => d.Id == "k8s-app");
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
            }
        };
    }
}