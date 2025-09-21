using GameConsole.AI.Local;
using GameConsole.AI.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Local.Tests;

/// <summary>
/// Tests for the LocalAIRuntime service.
/// Validates integration of all AI components and service lifecycle.
/// </summary>
public class LocalAIRuntimeTests : IAsyncDisposable
{
    private readonly ILogger<LocalAIRuntime> _logger;
    private readonly LocalAIRuntime _runtime;
    private readonly string _tempDirectory;
    private readonly string _testModelPath;

    public LocalAIRuntimeTests()
    {
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalAIRuntime>.Instance;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GameConsoleTest", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        // Create a mock ONNX model file (just for testing file operations)
        _testModelPath = Path.Combine(_tempDirectory, "test_model.onnx");
        File.WriteAllText(_testModelPath, "Mock ONNX model content for testing");
        
        _runtime = new LocalAIRuntime(_logger);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        // Act & Assert
        await _runtime.InitializeAsync();
        
        // Should not throw exception
        Assert.False(_runtime.IsRunning); // Not started yet
    }

    [Fact]
    public async Task StartAsync_AfterInitialize_ShouldSetRunningState()
    {
        // Arrange
        await _runtime.InitializeAsync();

        // Act
        await _runtime.StartAsync();

        // Assert
        Assert.True(_runtime.IsRunning);
    }

    [Fact]
    public async Task StopAsync_AfterStart_ShouldClearRunningState()
    {
        // Arrange
        await _runtime.InitializeAsync();
        await _runtime.StartAsync();
        Assert.True(_runtime.IsRunning);

        // Act
        await _runtime.StopAsync();

        // Assert
        Assert.False(_runtime.IsRunning);
    }

    [Fact]
    public async Task ListModelsAsync_InitiallyEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        await _runtime.InitializeAsync();
        await _runtime.StartAsync();

        // Act
        var models = await _runtime.ListModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Empty(models);
    }

    [Fact]
    public async Task GetModelInfoAsync_WithNonExistentModel_ShouldReturnNull()
    {
        // Arrange
        await _runtime.InitializeAsync();
        await _runtime.StartAsync();

        // Act
        var modelInfo = await _runtime.GetModelInfoAsync("non-existent-model");

        // Assert
        Assert.Null(modelInfo);
    }

    [Fact]
    public async Task GetResourceStatsAsync_ShouldReturnValidStats()
    {
        // Arrange
        await _runtime.InitializeAsync();
        await _runtime.StartAsync();

        // Act
        var stats = await _runtime.GetResourceStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.MemoryUsedMB >= 0);
        Assert.True(stats.MemoryAvailableMB >= 0);
        Assert.True(stats.CpuUsagePercent >= 0);
        Assert.True(stats.ActiveInferences >= 0);
        Assert.True(stats.QueuedInferences >= 0);
    }

    [Fact]
    public async Task InferAsync_WithoutLoadedModel_ShouldReturnFailure()
    {
        // Arrange
        await _runtime.InitializeAsync();
        await _runtime.StartAsync();

        var request = new InferenceRequest(
            RequestId: "test-request",
            ModelId: "non-existent-model",
            Inputs: new Dictionary<string, object> { ["input"] = new float[] { 1.0f, 2.0f, 3.0f } }
        );

        // Act
        var result = await _runtime.InferAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("test-request", result.RequestId);
        Assert.Contains("Model not loaded", result.ErrorMessage);
    }

    [Fact]
    public async Task InferBatchAsync_WithEmptyBatch_ShouldReturnEmptyResults()
    {
        // Arrange
        await _runtime.InitializeAsync();
        await _runtime.StartAsync();

        var requests = Array.Empty<InferenceRequest>();

        // Act
        var results = await _runtime.InferBatchAsync(requests);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public void ResourceManager_ShouldReturnValidCapability()
    {
        // Act
        var resourceManager = _runtime.ResourceManager;

        // Assert
        Assert.NotNull(resourceManager);
        Assert.IsAssignableFrom<IResourceManagerCapability>(resourceManager);
    }

    [Fact]
    public void ModelCache_ShouldReturnValidCapability()
    {
        // Act
        var modelCache = _runtime.ModelCache;

        // Assert
        Assert.NotNull(modelCache);
        Assert.IsAssignableFrom<IModelCacheCapability>(modelCache);
    }

    [Fact]
    public void InferenceEngine_ShouldReturnValidCapability()
    {
        // Act
        var inferenceEngine = _runtime.InferenceEngine;

        // Assert
        Assert.NotNull(inferenceEngine);
        Assert.IsAssignableFrom<ILocalInferenceCapability>(inferenceEngine);
    }

    [Fact]
    public async Task ServiceLifecycle_FullCycle_ShouldWorkCorrectly()
    {
        // Test complete service lifecycle

        // Initialize
        await _runtime.InitializeAsync();
        Assert.False(_runtime.IsRunning);

        // Start
        await _runtime.StartAsync();
        Assert.True(_runtime.IsRunning);

        // Use service
        var stats = await _runtime.GetResourceStatsAsync();
        Assert.NotNull(stats);

        var models = await _runtime.ListModelsAsync();
        Assert.NotNull(models);

        // Stop
        await _runtime.StopAsync();
        Assert.False(_runtime.IsRunning);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _runtime.DisposeAsync();
        }
        catch
        {
            // Ignore dispose errors in tests
        }
        
        // Cleanup temp directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}