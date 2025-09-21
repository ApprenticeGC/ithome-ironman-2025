using GameConsole.Deployment.Containers.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameConsole.Deployment.Containers.Tests;

public class ContainerHealthMonitorTests
{
    private readonly Mock<ILogger<ContainerHealthMonitor>> _mockLogger;
    private readonly ContainerHealthMonitor _monitor;

    public ContainerHealthMonitorTests()
    {
        _mockLogger = new Mock<ILogger<ContainerHealthMonitor>>();
        _monitor = new ContainerHealthMonitor(_mockLogger.Object);
    }

    [Fact]
    public void ProviderName_ShouldReturnExpectedName()
    {
        Assert.Equal("ContainerHealthMonitor", _monitor.ProviderName);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSucceed()
    {
        await _monitor.InitializeAsync();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initializing ContainerHealthMonitor")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        await _monitor.InitializeAsync();
        await _monitor.StartAsync();
        
        Assert.True(_monitor.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
    {
        await _monitor.InitializeAsync();
        await _monitor.StartAsync();
        await _monitor.StopAsync();
        
        Assert.False(_monitor.IsRunning);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthResult()
    {
        const string deploymentId = "test-deployment";
        await _monitor.InitializeAsync();
        await _monitor.StartAsync();
        
        var result = await _monitor.CheckHealthAsync(deploymentId);
        
        Assert.NotNull(result);
        Assert.Equal(deploymentId, result.Data["DeploymentId"]);
        Assert.Equal("ContainerHealthMonitor", result.Data["Provider"]);
        Assert.True(result.ResponseTime >= TimeSpan.Zero);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public async Task ConfigureHealthCheckAsync_ShouldComplete()
    {
        const string deploymentId = "test-deployment";
        var configuration = new HealthCheckConfiguration
        {
            Command = "curl -f http://localhost/health",
            Interval = TimeSpan.FromSeconds(10),
            Timeout = TimeSpan.FromSeconds(5),
            FailureThreshold = 3,
            SuccessThreshold = 1
        };
        
        await _monitor.InitializeAsync();
        await _monitor.StartAsync();
        
        await _monitor.ConfigureHealthCheckAsync(deploymentId, configuration);
        
        // Should complete without throwing
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuring health check")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MonitorHealthAsync_ShouldReturnObservable()
    {
        const string deploymentId = "test-deployment";
        await _monitor.InitializeAsync();
        await _monitor.StartAsync();
        
        var observable = _monitor.MonitorHealthAsync(deploymentId);
        
        Assert.NotNull(observable);
        
        // Test that we can subscribe to the observable
        var healthUpdates = new List<HealthStatus>();
        using var subscription = observable.Subscribe(healthUpdates.Add);
        
        // Wait a bit to see if we get health updates
        await Task.Delay(100);
        
        // Cleanup
        await _monitor.StopAsync();
    }

    [Fact]
    public async Task MonitorHealthAsync_SameDploymentTwice_ShouldReturnSameObservable()
    {
        const string deploymentId = "test-deployment";
        await _monitor.InitializeAsync();
        await _monitor.StartAsync();
        
        var observable1 = _monitor.MonitorHealthAsync(deploymentId);
        var observable2 = _monitor.MonitorHealthAsync(deploymentId);
        
        Assert.Same(observable1, observable2);
        
        await _monitor.StopAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        const string deploymentId = "test-deployment";
        await _monitor.InitializeAsync();
        await _monitor.StartAsync();
        
        // Start monitoring
        var observable = _monitor.MonitorHealthAsync(deploymentId);
        var healthUpdates = new List<HealthStatus>();
        using var subscription = observable.Subscribe(healthUpdates.Add);
        
        // Dispose should complete without throwing
        await _monitor.DisposeAsync();
        
        Assert.False(_monitor.IsRunning);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ContainerHealthMonitor disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void HealthState_AllValues_ShouldBeValid()
    {
        var states = Enum.GetValues<HealthState>();
        
        // Ensure all enum values are defined
        Assert.Contains(HealthState.Unknown, states);
        Assert.Contains(HealthState.Healthy, states);
        Assert.Contains(HealthState.Unhealthy, states);
        Assert.Contains(HealthState.Starting, states);
        Assert.Contains(HealthState.Degraded, states);
    }

    [Fact]
    public void DeploymentPhase_AllValues_ShouldBeValid()
    {
        var phases = Enum.GetValues<DeploymentPhase>();
        
        // Ensure all enum values are defined
        Assert.Contains(DeploymentPhase.Pending, phases);
        Assert.Contains(DeploymentPhase.Progressing, phases);
        Assert.Contains(DeploymentPhase.Complete, phases);
        Assert.Contains(DeploymentPhase.Failed, phases);
        Assert.Contains(DeploymentPhase.Scaling, phases);
        Assert.Contains(DeploymentPhase.Terminating, phases);
    }
}