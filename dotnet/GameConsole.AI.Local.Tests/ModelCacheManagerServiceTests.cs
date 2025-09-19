using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Local.Tests;

public class ModelCacheManagerServiceTests
{
    private readonly ModelCacheManagerService _cacheManager;
    private readonly string _testModelId = "test-model-123";

    public ModelCacheManagerServiceTests()
    {
        _cacheManager = new ModelCacheManagerService(new NullLogger<ModelCacheManagerService>());
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Assert
        Assert.Equal(0, _cacheManager.CachedModelCount);
        Assert.Equal(0, _cacheManager.TotalCacheSize);
        Assert.True(_cacheManager.MaxCacheSize > 0);
    }

    [Fact]
    public async Task IsModelCachedAsync_WithNonexistentModel_ShouldReturnFalse()
    {
        // Act
        var isCached = await _cacheManager.IsModelCachedAsync(_testModelId);

        // Assert
        Assert.False(isCached);
    }

    [Fact]
    public async Task IsModelCachedAsync_WithEmptyModelId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.IsModelCachedAsync(""));
    }

    [Fact]
    public async Task IsModelCachedAsync_WithNullModelId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.IsModelCachedAsync(null!));
    }

    [Fact]
    public async Task GetModelMetadataAsync_WithNonexistentModel_ShouldReturnNull()
    {
        // Act
        var metadata = await _cacheManager.GetModelMetadataAsync(_testModelId);

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public async Task GetCachedModelAsync_WithNonexistentModel_ShouldReturnNull()
    {
        // Act
        var modelData = await _cacheManager.GetCachedModelAsync(_testModelId);

        // Assert
        Assert.Null(modelData);
    }

    [Fact]
    public async Task RemoveModelAsync_WithNonexistentModel_ShouldNotThrow()
    {
        // Act & Assert
        await _cacheManager.RemoveModelAsync(_testModelId);
        // Should complete without throwing
    }

    [Fact]
    public async Task ClearCacheAsync_ShouldResetCacheState()
    {
        // Act
        await _cacheManager.ClearCacheAsync();

        // Assert
        Assert.Equal(0, _cacheManager.CachedModelCount);
        Assert.Equal(0, _cacheManager.TotalCacheSize);
    }

    [Fact]
    public async Task CacheModelAsync_WithInvalidPath_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _cacheManager.CacheModelAsync(_testModelId, "nonexistent.onnx"));
    }

    [Fact]
    public async Task CacheModelAsync_WithEmptyModelId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.CacheModelAsync("", "test.onnx"));
    }

    [Fact]
    public async Task CacheModelAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.CacheModelAsync(_testModelId, ""));
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Act
        await _cacheManager.DisposeAsync();

        // Assert
        // Should complete without throwing
    }

    [Fact]
    public void Properties_ShouldReturnConsistentValues()
    {
        // Act
        var count = _cacheManager.CachedModelCount;
        var totalSize = _cacheManager.TotalCacheSize;
        var maxSize = _cacheManager.MaxCacheSize;

        // Assert
        Assert.True(count >= 0);
        Assert.True(totalSize >= 0);
        Assert.True(maxSize > 0);
        Assert.True(totalSize <= maxSize);
    }

    [Fact]
    public async Task GetModelMetadataAsync_WithNullModelId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.GetModelMetadataAsync(null!));
    }

    [Fact]
    public async Task GetCachedModelAsync_WithNullModelId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.GetCachedModelAsync(null!));
    }

    [Fact]
    public async Task RemoveModelAsync_WithNullModelId_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _cacheManager.RemoveModelAsync(null!));
    }
}