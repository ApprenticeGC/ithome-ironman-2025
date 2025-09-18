using GameConsole.AI.Local;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Local.Tests;

public class LocalAIRuntimeTests
{
    [Fact]
    public void LocalAIRuntime_CanBeCreated()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LocalAIRuntime>>();
        var resourceManager = Substitute.For<IAIResourceManager>();
        var cacheManager = Substitute.For<IModelCacheManager>();
        var inferenceEngine = Substitute.For<ILocalInferenceEngine>();

        // Act
        var runtime = new LocalAIRuntime(logger, resourceManager, cacheManager, inferenceEngine);

        // Assert
        runtime.Should().NotBeNull();
        runtime.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task LocalAIRuntime_InitializeAsync_CallsResourceManagerAndCacheManager()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LocalAIRuntime>>();
        var resourceManager = Substitute.For<IAIResourceManager>();
        var cacheManager = Substitute.For<IModelCacheManager>();
        var inferenceEngine = Substitute.For<ILocalInferenceEngine>();

        var capabilities = new ResourceCapabilities
        {
            TotalCpuCores = 4,
            TotalMemoryMB = 8192,
            HasGpu = false
        };

        var cacheStats = new CacheStatistics
        {
            TotalCachedModels = 0,
            TotalCacheSizeBytes = 0
        };

        resourceManager.GetResourceCapabilitiesAsync(Arg.Any<CancellationToken>())
            .Returns(capabilities);
        cacheManager.PerformMaintenanceAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        cacheManager.GetCacheStatisticsAsync(Arg.Any<CancellationToken>())
            .Returns(cacheStats);

        var runtime = new LocalAIRuntime(logger, resourceManager, cacheManager, inferenceEngine);

        // Act
        await runtime.InitializeAsync();

        // Assert
        await resourceManager.Received(1).GetResourceCapabilitiesAsync(Arg.Any<CancellationToken>());
        await cacheManager.Received(1).PerformMaintenanceAsync(Arg.Any<CancellationToken>());
        await cacheManager.Received(1).GetCacheStatisticsAsync(Arg.Any<CancellationToken>());
    }
}

public class AIResourceManagerTests
{
    [Fact]
    public void AIResourceManager_CanBeCreated()
    {
        // Arrange
        var logger = Substitute.For<ILogger<AIResourceManager>>();

        // Act
        var manager = new AIResourceManager(logger);

        // Assert
        manager.Should().NotBeNull();
    }

    [Fact]
    public async Task AIResourceManager_GetResourceCapabilitiesAsync_ReturnsCapabilities()
    {
        // Arrange
        var logger = Substitute.For<ILogger<AIResourceManager>>();
        var manager = new AIResourceManager(logger);

        // Act
        var capabilities = await manager.GetResourceCapabilitiesAsync();

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.TotalCpuCores.Should().BeGreaterThan(0);
        capabilities.TotalMemoryMB.Should().BeGreaterThan(0);
        capabilities.SupportedProviders.Should().Contain(ExecutionProvider.CPU);
    }
}

public class ModelCacheManagerTests
{
    [Fact]
    public void ModelCacheManager_CanBeCreated()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ModelCacheManager>>();

        // Act
        var manager = new ModelCacheManager(logger);

        // Assert
        manager.Should().NotBeNull();
    }

    [Fact]
    public async Task ModelCacheManager_IsModelCachedAsync_ReturnsFalseForNonExistentModel()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ModelCacheManager>>();
        var manager = new ModelCacheManager(logger);

        // Act
        var isCached = await manager.IsModelCachedAsync("non-existent-model");

        // Assert
        isCached.Should().BeFalse();
    }

    [Fact]
    public async Task ModelCacheManager_GetCacheStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ModelCacheManager>>();
        var manager = new ModelCacheManager(logger);

        // Act
        var stats = await manager.GetCacheStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalCachedModels.Should().BeGreaterThanOrEqualTo(0);
        stats.CollectedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }
}

public class LocalInferenceEngineTests
{
    [Fact]
    public void LocalInferenceEngine_CanBeCreated()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LocalInferenceEngine>>();

        // Act
        var engine = new LocalInferenceEngine(logger);

        // Assert
        engine.Should().NotBeNull();
    }

    [Fact]
    public async Task LocalInferenceEngine_IsModelLoadedAsync_ReturnsFalseForNonLoadedModel()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LocalInferenceEngine>>();
        var engine = new LocalInferenceEngine(logger);

        // Act
        var isLoaded = await engine.IsModelLoadedAsync("non-loaded-model");

        // Assert
        isLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task LocalInferenceEngine_GetPerformanceMetricsAsync_ReturnsMetrics()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LocalInferenceEngine>>();
        var engine = new LocalInferenceEngine(logger);

        // Act
        var metrics = await engine.GetPerformanceMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalInferences.Should().BeGreaterThanOrEqualTo(0);
        metrics.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void LocalInferenceEngine_ImplementsIDisposable()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LocalInferenceEngine>>();

        // Act & Assert
        using var engine = new LocalInferenceEngine(logger);
        engine.Should().NotBeNull();
    }
}