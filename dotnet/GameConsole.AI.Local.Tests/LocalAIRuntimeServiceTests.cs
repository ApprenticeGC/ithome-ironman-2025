using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Local.Tests;

public class LocalAIRuntimeServiceTests
{
    private readonly ILogger<LocalAIRuntimeService> _logger = new NullLogger<LocalAIRuntimeService>();

    [Fact]
    public void Constructor_WithValidLogger_ShouldSucceed()
    {
        // Act
        var service = new LocalAIRuntimeService(_logger);

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalAIRuntimeService(null!));
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetupComponents()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.ResourceManager);
        Assert.NotNull(service.ModelCache);
        Assert.NotNull(service.InferenceEngine);
        Assert.NotEqual(ExecutionProvider.Auto, service.CurrentExecutionProvider);
    }

    [Fact]
    public async Task StartAsync_AfterInitialize_ShouldSetRunningFlag()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        // Act
        await service.StartAsync();

        // Assert
        Assert.True(service.IsRunning);

        // Cleanup
        await service.StopAsync();
    }

    [Fact]
    public async Task StopAsync_WhenRunning_ShouldClearRunningFlag()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act
        await service.StopAsync();

        // Assert
        Assert.False(service.IsRunning);
    }

    [Fact]
    public async Task SetResourceConstraintsAsync_WithValidConstraints_ShouldSucceed()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        var constraints = new ResourceConstraints
        {
            MaxMemoryBytes = 1024 * 1024 * 1024, // 1GB
            MaxCpuUtilizationPercent = 70.0,
            MaxGpuUtilizationPercent = 80.0,
            MaxConcurrentOperations = 2,
            InferenceTimeout = TimeSpan.FromSeconds(10)
        };

        // Act & Assert
        await service.SetResourceConstraintsAsync(constraints);
        // No exception should be thrown
    }

    [Fact]
    public async Task SetResourceConstraintsAsync_WithNullConstraints_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SetResourceConstraintsAsync(null!));
    }

    [Fact]
    public async Task GetAvailableProvidersAsync_ShouldReturnNonEmptyCollection()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        // Act
        var providers = await service.GetAvailableProvidersAsync();

        // Assert
        Assert.NotEmpty(providers);
        Assert.Contains(ExecutionProvider.Cpu, providers);
    }

    [Fact]
    public void CurrentMetrics_ShouldReturnValidMetrics()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);

        // Act
        var metrics = service.CurrentMetrics;

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.RecordedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task LoadModelAsync_WithInvalidPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.LoadModelAsync("nonexistent.onnx", "test-model"));
    }

    [Fact]
    public async Task LoadModelAsync_WithEmptyModelId_ShouldThrowArgumentException()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.LoadModelAsync("test.onnx", ""));
    }

    [Fact]
    public async Task UnloadModelAsync_WithNonexistentModel_ShouldNotThrow()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        // Act & Assert
        await service.UnloadModelAsync("nonexistent-model");
        // Should not throw exception
    }

    [Fact]
    public async Task InferAsync_WithUnloadedModel_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        var input = new Dictionary<string, object>
        {
            ["test"] = new float[] { 1.0f, 2.0f, 3.0f }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InferAsync("nonexistent-model", input));
    }

    [Fact]
    public async Task InferBatchAsync_WithNullInputs_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.InferBatchAsync("test-model", null!));
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var service = new LocalAIRuntimeService(_logger);
        await service.InitializeAsync();
        await service.StartAsync();

        // Act
        await service.DisposeAsync();

        // Assert
        Assert.False(service.IsRunning);
    }
}