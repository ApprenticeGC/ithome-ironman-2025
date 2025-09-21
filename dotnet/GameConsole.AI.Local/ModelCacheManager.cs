using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace GameConsole.AI.Local;

/// <summary>
/// Model Cache Manager for efficient local model storage and retrieval.
/// Implements LRU caching with automatic eviction and storage optimization.
/// </summary>
[Service("AI", "Cache", "Local")]
public class ModelCacheManager : IModelCacheCapability, IAsyncDisposable
{
    private readonly ILogger<ModelCacheManager> _logger;
    private readonly ConcurrentDictionary<string, CachedModel> _cache = new();
    private readonly string _cacheDirectory;
    private readonly long _maxCacheSizeBytes;
    private readonly object _cacheLock = new();
    private long _currentCacheSizeBytes;
    private bool _disposed;

    public ModelCacheManager(ILogger<ModelCacheManager> logger, string? cacheDirectory = null, long maxCacheSizeMB = 2048)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheDirectory = cacheDirectory ?? Path.Combine(Path.GetTempPath(), "GameConsole", "AI", "ModelCache");
        _maxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024;

        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
        
        // Load existing cache index
        LoadCacheIndex();
        
        _logger.LogInformation("Initialized ModelCacheManager at {CacheDirectory} with {MaxSizeMB}MB limit", 
            _cacheDirectory, maxCacheSizeMB);
    }

    #region IModelCacheCapability Implementation

    public async Task<string> CacheModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ModelCacheManager));
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");

        var cacheKey = GenerateCacheKey(modelPath);
        
        // Check if already cached
        if (_cache.ContainsKey(cacheKey))
        {
            UpdateAccessTime(cacheKey);
            _logger.LogDebug("Model already cached: {CacheKey}", cacheKey);
            return cacheKey;
        }

        var modelInfo = new FileInfo(modelPath);
        var cachedPath = Path.Combine(_cacheDirectory, cacheKey + Path.GetExtension(modelPath));

        // Ensure we have space for the model
        await EnsureCacheSpaceAsync(modelInfo.Length, cancellationToken);

        // Copy model to cache
        _logger.LogInformation("Caching model: {ModelPath} -> {CacheKey}", modelPath, cacheKey);
        using (var sourceStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read))
        using (var destStream = new FileStream(cachedPath, FileMode.Create, FileAccess.Write))
        {
            await sourceStream.CopyToAsync(destStream, cancellationToken);
        }

        var cachedModel = new CachedModel(
            cacheKey,
            modelPath,
            cachedPath,
            modelInfo.Length,
            DateTime.UtcNow,
            DateTime.UtcNow,
            1
        );

        lock (_cacheLock)
        {
            _cache[cacheKey] = cachedModel;
            _currentCacheSizeBytes += modelInfo.Length;
        }

        await SaveCacheIndexAsync();
        
        _logger.LogInformation("Cached model: {CacheKey}, Size: {SizeMB}MB", 
            cacheKey, modelInfo.Length / (1024.0 * 1024.0));
        
        return cacheKey;
    }

    public async Task<string?> GetCachedModelAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ModelCacheManager));

        if (_cache.TryGetValue(cacheKey, out var cachedModel))
        {
            if (File.Exists(cachedModel.CachedPath))
            {
                UpdateAccessTime(cacheKey);
                _logger.LogDebug("Cache hit for model: {CacheKey}", cacheKey);
                return cachedModel.CachedPath;
            }
            else
            {
                // Cache entry exists but file is missing - remove from cache
                await EvictModelAsync(cacheKey, cancellationToken);
                _logger.LogWarning("Cached model file missing, evicted: {CacheKey}", cacheKey);
            }
        }

        _logger.LogDebug("Cache miss for model: {CacheKey}", cacheKey);
        return null;
    }

    public async Task EvictModelAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ModelCacheManager));

        if (_cache.TryRemove(cacheKey, out var cachedModel))
        {
            try
            {
                if (File.Exists(cachedModel.CachedPath))
                {
                    File.Delete(cachedModel.CachedPath);
                }

                lock (_cacheLock)
                {
                    _currentCacheSizeBytes -= cachedModel.SizeBytes;
                }

                await SaveCacheIndexAsync();
                
                _logger.LogInformation("Evicted model from cache: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evict model: {CacheKey}", cacheKey);
                
                // Re-add to cache if deletion failed
                _cache[cacheKey] = cachedModel;
                throw;
            }
        }
    }

    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ModelCacheManager));

        _logger.LogInformation("Clearing all cached models");

        var cacheKeys = _cache.Keys.ToList();
        foreach (var cacheKey in cacheKeys)
        {
            await EvictModelAsync(cacheKey, cancellationToken);
        }

        _logger.LogInformation("Cleared all cached models");
    }

    public async Task<(long UsedBytes, long AvailableBytes, int CachedModels)> GetCacheStatsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ModelCacheManager));

        return await Task.FromResult((
            UsedBytes: _currentCacheSizeBytes,
            AvailableBytes: _maxCacheSizeBytes - _currentCacheSizeBytes,
            CachedModels: _cache.Count
        ));
    }

    #endregion

    #region ICapabilityProvider Implementation

    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new[] { typeof(IModelCacheCapability) });
    }

    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(typeof(T) == typeof(IModelCacheCapability));
    }

    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IModelCacheCapability))
            return await Task.FromResult(this as T);
        
        return await Task.FromResult<T?>(null);
    }

    #endregion

    #region Private Helpers

    private static string GenerateCacheKey(string modelPath)
    {
        var modelBytes = Encoding.UTF8.GetBytes(modelPath);
        var hashBytes = SHA256.HashData(modelBytes);
        return Convert.ToHexString(hashBytes)[..16]; // Use first 16 characters
    }

    private void UpdateAccessTime(string cacheKey)
    {
        if (_cache.TryGetValue(cacheKey, out var cachedModel))
        {
            var updatedModel = cachedModel with 
            { 
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = cachedModel.AccessCount + 1 
            };
            _cache[cacheKey] = updatedModel;
        }
    }

    private async Task EnsureCacheSpaceAsync(long requiredBytes, CancellationToken cancellationToken)
    {
        var availableSpace = _maxCacheSizeBytes - _currentCacheSizeBytes;
        if (availableSpace >= requiredBytes)
            return;

        _logger.LogInformation("Insufficient cache space, need to evict {RequiredMB}MB", 
            (requiredBytes - availableSpace) / (1024.0 * 1024.0));

        // Use LRU eviction strategy
        var modelsToEvict = _cache.Values
            .OrderBy(m => m.LastAccessedAt)
            .TakeWhile(m => _currentCacheSizeBytes + requiredBytes > _maxCacheSizeBytes)
            .ToList();

        foreach (var model in modelsToEvict)
        {
            await EvictModelAsync(model.CacheKey, cancellationToken);
        }
    }

    private void LoadCacheIndex()
    {
        var indexFile = Path.Combine(_cacheDirectory, "cache_index.json");
        if (!File.Exists(indexFile))
            return;

        try
        {
            var indexJson = File.ReadAllText(indexFile);
            var cachedModels = System.Text.Json.JsonSerializer.Deserialize<CachedModel[]>(indexJson);
            
            if (cachedModels != null)
            {
                foreach (var model in cachedModels)
                {
                    if (File.Exists(model.CachedPath))
                    {
                        _cache[model.CacheKey] = model;
                        _currentCacheSizeBytes += model.SizeBytes;
                    }
                }
            }

            _logger.LogInformation("Loaded cache index: {ModelCount} models, {SizeMB}MB", 
                _cache.Count, _currentCacheSizeBytes / (1024.0 * 1024.0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cache index, starting with empty cache");
        }
    }

    private async Task SaveCacheIndexAsync()
    {
        var indexFile = Path.Combine(_cacheDirectory, "cache_index.json");
        
        try
        {
            var cachedModels = _cache.Values.ToArray();
            var indexJson = System.Text.Json.JsonSerializer.Serialize(cachedModels, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(indexFile, indexJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cache index");
        }
    }

    #endregion

    #region IAsyncDisposable Implementation

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing ModelCacheManager");
            await SaveCacheIndexAsync();
            _disposed = true;
            _logger.LogDebug("Disposed ModelCacheManager");
        }
        GC.SuppressFinalize(this);
    }

    #endregion

    private record CachedModel(
        string CacheKey,
        string OriginalPath,
        string CachedPath,
        long SizeBytes,
        DateTime CachedAt,
        DateTime LastAccessedAt,
        int AccessCount
    );
}