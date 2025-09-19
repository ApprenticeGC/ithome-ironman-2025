using GameConsole.Deployment.Containers;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GameConsole.Deployment.Containers.Tests;

/// <summary>
/// Tests for the ContainerHealthMonitor service.
/// </summary>
[TestFixture]
public class ContainerHealthMonitorTests
{
    private ContainerHealthMonitor? _healthMonitor;
    private ILogger<ContainerHealthMonitor>? _logger;

    [SetUp]
    public void Setup()
    {
        _logger = new LoggerFactory().CreateLogger<ContainerHealthMonitor>();
        _healthMonitor = new ContainerHealthMonitor(_logger);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_healthMonitor != null)
        {
            await _healthMonitor.DisposeAsync();
        }
    }

    [Test]
    public async Task InitializeAsync_ShouldInitializeSuccessfully()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);

        // Act
        await _healthMonitor!.InitializeAsync();

        // Assert
        // No exception should be thrown
        Assert.Pass("Initialization completed successfully");
    }

    [Test]
    public async Task StartAsync_ShouldStartServiceSuccessfully()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);
        await _healthMonitor!.InitializeAsync();

        // Act
        await _healthMonitor.StartAsync();

        // Assert
        Assert.That(_healthMonitor.IsRunning, Is.True);
    }

    [Test]
    public async Task StopAsync_ShouldStopServiceSuccessfully()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);
        await _healthMonitor!.InitializeAsync();
        await _healthMonitor.StartAsync();
        Assert.That(_healthMonitor.IsRunning, Is.True);

        // Act
        await _healthMonitor.StopAsync();

        // Assert
        Assert.That(_healthMonitor.IsRunning, Is.False);
    }

    [Test]
    public async Task GetCapabilitiesAsync_ShouldReturnExpectedCapabilities()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);

        // Act
        var capabilities = await _healthMonitor!.GetCapabilitiesAsync();

        // Assert
        Assert.That(capabilities, Is.Not.Null);
        Assert.That(capabilities, Contains.Item(typeof(IContainerHealthMonitor)));
        Assert.That(capabilities, Contains.Item(typeof(IService)));
    }

    [Test]
    public async Task HasCapabilityAsync_ShouldReturnTrueForSupportedCapabilities()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);

        // Act & Assert
        Assert.That(await _healthMonitor!.HasCapabilityAsync<IContainerHealthMonitor>(), Is.True);
        Assert.That(await _healthMonitor.HasCapabilityAsync<IService>(), Is.True);
    }

    [Test]
    public async Task GetCapabilityAsync_ShouldReturnSelfForSupportedCapabilities()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);

        // Act
        var healthMonitorCapability = await _healthMonitor!.GetCapabilityAsync<IContainerHealthMonitor>();

        // Assert
        Assert.That(healthMonitorCapability, Is.SameAs(_healthMonitor));
    }

    [Test]
    public async Task StartMonitoringAsync_ShouldAcceptValidConfiguration()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);
        await _healthMonitor!.InitializeAsync();
        await _healthMonitor.StartAsync();

        var healthCheckConfig = new HealthCheckConfiguration
        {
            Path = "/health",
            Port = 8080,
            Interval = TimeSpan.FromSeconds(30),
            Timeout = TimeSpan.FromSeconds(5)
        };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => 
            await _healthMonitor.StartMonitoringAsync("test-service", healthCheckConfig));
    }

    [Test]
    public async Task GetHealthStatusesAsync_ShouldReturnEmptyInitially()
    {
        // Arrange
        Assert.That(_healthMonitor, Is.Not.Null);
        await _healthMonitor!.InitializeAsync();
        await _healthMonitor.StartAsync();

        // Act
        var statuses = await _healthMonitor.GetHealthStatusesAsync();

        // Assert
        Assert.That(statuses, Is.Not.Null);
        Assert.That(statuses, Is.Empty);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContainerHealthMonitor(null!));
    }
}