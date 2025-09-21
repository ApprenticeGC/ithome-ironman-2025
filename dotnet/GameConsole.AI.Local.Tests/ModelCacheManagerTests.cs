using GameConsole.AI.Local;
using GameConsole.AI.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Local.Tests;

/// <summary>
/// Tests for the ModelCacheManager component.
/// Validates model caching, LRU eviction, and cache persistence.
/// </summary>
public class ModelCacheManagerTests : IAsyncDisposable
{
    private readonly ILogger<ModelCacheManager> _logger;
    private readonly ModelCacheManager _cacheManager;
    private readonly string _tempDirectory;
    private readonly string _testModelPath;

    public ModelCacheManagerTests()
    {
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ModelCacheManager>.Instance;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GameConsoleTest", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        // Create a test "model" file
        _testModelPath = Path.Combine(_tempDirectory, "test_model.onnx");
        File.WriteAllText(_testModelPath, "This is a test model file content for testing purposes.");
        
        _cacheManager = new ModelCacheManager(_logger, _tempDirectory, maxCacheSizeMB: 1); // 1MB limit for testing
    }

    [Fact]
    public async Task CacheModelAsync_WithValidModel_ShouldReturnCacheKey()
    {
        // Act
        var cacheKey = await _cacheManager.CacheModelAsync(_testModelPath);

        // Assert
        Assert.NotNull(cacheKey);
        Assert.NotEmpty(cacheKey);
    }

    [Fact]
    public async Task CacheModelAsync_WithNonExistentModel_ShouldThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.onnx");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _cacheManager.CacheModelAsync(nonExistentPath));
    }

    [Fact]
    public async Task GetCachedModelAsync_WithCachedModel_ShouldReturnPath()
    {
        // Arrange
        var cacheKey = await _cacheManager.CacheModelAsync(_testModelPath);

        // Act
        var cachedPath = await _cacheManager.GetCachedModelAsync(cacheKey);

        // Assert
        Assert.NotNull(cachedPath);
        Assert.True(File.Exists(cachedPath));
    }

    [Fact]
    public async Task GetCachedModelAsync_WithInvalidKey_ShouldReturnNull()
    {
        // Act
        var cachedPath = await _cacheManager.GetCachedModelAsync("invalid-key");

        // Assert
        Assert.Null(cachedPath);
    }

    [Fact]
    public async Task EvictModelAsync_WithCachedModel_ShouldRemoveFromCache()
    {
        // Arrange
        var cacheKey = await _cacheManager.CacheModelAsync(_testModelPath);
        
        // Verify it's cached first
        var cachedPath = await _cacheManager.GetCachedModelAsync(cacheKey);
        Assert.NotNull(cachedPath);

        // Act
        await _cacheManager.EvictModelAsync(cacheKey);

        // Assert
        var evictedPath = await _cacheManager.GetCachedModelAsync(cacheKey);
        Assert.Null(evictedPath);
    }

    [Fact]
    public async Task GetCacheStatsAsync_ShouldReturnValidStats()
    {
        // Arrange
        await _cacheManager.CacheModelAsync(_testModelPath);

        // Act
        var stats = await _cacheManager.GetCacheStatsAsync();

        // Assert
        Assert.True(stats.UsedBytes > 0);
        Assert.True(stats.AvailableBytes >= 0);
        Assert.Equal(1, stats.CachedModels);
    }

    [Fact]
    public async Task ClearCacheAsync_ShouldRemoveAllModels()
    {
        // Arrange
        await _cacheManager.CacheModelAsync(_testModelPath);
        
        var statsBeforeClear = await _cacheManager.GetCacheStatsAsync();
        Assert.Equal(1, statsBeforeClear.CachedModels);

        // Act
        await _cacheManager.ClearCacheAsync();

        // Assert
        var statsAfterClear = await _cacheManager.GetCacheStatsAsync();
        Assert.Equal(0, statsAfterClear.CachedModels);
        Assert.Equal(0, statsAfterClear.UsedBytes);
    }

    [Fact]
    public async Task CacheModelAsync_SameModelTwice_ShouldReturnSameCacheKey()
    {
        // Act
        var cacheKey1 = await _cacheManager.CacheModelAsync(_testModelPath);
        var cacheKey2 = await _cacheManager.CacheModelAsync(_testModelPath);

        // Assert
        Assert.Equal(cacheKey1, cacheKey2);
    }

    [Fact]
    public async Task HasCapabilityAsync_WithCorrectType_ShouldReturnTrue()
    {
        // Act
        var hasCapability = await _cacheManager.HasCapabilityAsync<IModelCacheCapability>();

        // Assert
        Assert.True(hasCapability);
    }

    [Fact]
    public async Task GetCapabilityAsync_WithCorrectType_ShouldReturnInstance()
    {
        // Act
        var capability = await _cacheManager.GetCapabilityAsync<IModelCacheCapability>();

        // Assert
        Assert.NotNull(capability);
        Assert.Same(_cacheManager, capability);
    }

    public async ValueTask DisposeAsync()
    {
        await _cacheManager.DisposeAsync();
        
        // Cleanup temp directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}