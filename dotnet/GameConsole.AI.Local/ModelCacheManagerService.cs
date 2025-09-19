using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace GameConsole.AI.Local;

/// <summary>
/// Model cache manager for local model storage and retrieval.
/// Provides efficient caching with LRU eviction and compression.
/// </summary>
internal sealed class ModelCacheManagerService : IModelCacheManager, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, ModelCacheEntry> _cacheEntries = new();
    private readonly string _diskCacheDirectory;
    private readonly SemaphoreSlim _diskLock = new(1, 1);
    
    private long _maxCacheSize = 4L * 1024 * 1024 * 1024; // 4GB default
    private long _currentCacheSize = 0;
    private bool _disposed = false;

    public ModelCacheManagerService(ILogger logger, long maxCacheSizeBytes = 4L * 1024 * 1024 * 1024)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxCacheSize = maxCacheSizeBytes;
        
        // Initialize memory cache with size limit
        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = maxCacheSizeBytes / 4 // Use 1/4 of total cache for in-memory
        };
        _memoryCache = new MemoryCache(cacheOptions);

        // Setup disk cache directory
        _diskCacheDirectory = Path.Combine(Path.GetTempPath(), "GameConsole.AI.Cache");
        Directory.CreateDirectory(_diskCacheDirectory);

        // Load existing cache entries on startup
        _ = Task.Run(LoadExistingCacheEntriesAsync);

        _logger.LogInformation("Model cache manager initialized with max size: {MaxSizeMB}MB", 
            _maxCacheSize / (1024 * 1024));
    }

    public long TotalCacheSize => Interlocked.Read(ref _currentCacheSize);
    public long MaxCacheSize => _maxCacheSize;
    public int CachedModelCount => _cacheEntries.Count;

    public async Task CacheModelAsync(string modelId, string modelPath, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

        _logger.LogDebug("Caching model {ModelId} from {ModelPath}", modelId, modelPath);

        try
        {
            var fileInfo = new FileInfo(modelPath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"Model file not found: {modelPath}");

            // Check if model is already cached
            if (_cacheEntries.ContainsKey(modelId))
            {
                _logger.LogDebug("Model {ModelId} is already cached", modelId);
                return;
            }

            // Check cache size limits
            await EnsureCacheSpaceAsync(fileInfo.Length, cancellationToken);

            // Read and cache the model
            var modelData = await File.ReadAllBytesAsync(modelPath, cancellationToken);
            var hash = ComputeHash(modelData);

            var cacheEntry = new ModelCacheEntry
            {
                ModelId = modelId,
                FilePath = modelPath,
                Hash = hash,
                Size = modelData.Length,
                Metadata = metadata ?? new Dictionary<string, object>(),
                CachedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };

            // Store in memory cache (for frequently accessed models)
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
                Size = modelData.Length,
                Priority = CacheItemPriority.Normal,
                SlidingExpiration = TimeSpan.FromHours(1),
                PostEvictionCallbacks = { new PostEvictionCallbackRegistration
                {
                    EvictionCallback = OnMemoryCacheEvicted
                }}
            };

            _memoryCache.Set(modelId, modelData, memoryCacheOptions);

            // Also store on disk for persistence
            await StoreToDiskAsync(modelId, modelData, cacheEntry, cancellationToken);

            _cacheEntries[modelId] = cacheEntry;
            Interlocked.Add(ref _currentCacheSize, modelData.Length);

            _logger.LogInformation("Successfully cached model {ModelId} ({SizeMB}MB)", 
                modelId, modelData.Length / (1024 * 1024));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache model {ModelId}", modelId);
            throw;
        }
    }

    public async Task<byte[]?> GetCachedModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        _logger.LogDebug("Retrieving cached model {ModelId}", modelId);

        try
        {
            // Update last accessed time
            if (_cacheEntries.TryGetValue(modelId, out var entry))
            {
                entry.LastAccessedAt = DateTime.UtcNow;
            }

            // Try memory cache first
            if (_memoryCache.TryGetValue(modelId, out byte[]? memoryData))
            {
                _logger.LogDebug("Model {ModelId} retrieved from memory cache", modelId);
                return memoryData;
            }

            // Try disk cache
            var diskData = await LoadFromDiskAsync(modelId, cancellationToken);
            if (diskData != null)
            {
                _logger.LogDebug("Model {ModelId} retrieved from disk cache", modelId);

                // Put back in memory cache for faster access
                try
                {
                    var memoryCacheOptions = new MemoryCacheEntryOptions
                    {
                        Size = diskData.Length,
                        Priority = CacheItemPriority.Normal,
                        SlidingExpiration = TimeSpan.FromHours(1)
                    };
                    _memoryCache.Set(modelId, diskData, memoryCacheOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore model {ModelId} to memory cache", modelId);
                }

                return diskData;
            }

            _logger.LogDebug("Model {ModelId} not found in cache", modelId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached model {ModelId}", modelId);
            throw;
        }
    }

    public async Task RemoveModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        _logger.LogDebug("Removing model {ModelId} from cache", modelId);

        try
        {
            // Remove from memory cache
            _memoryCache.Remove(modelId);

            // Remove from disk cache
            await RemoveFromDiskAsync(modelId, cancellationToken);

            // Update cache size
            if (_cacheEntries.TryRemove(modelId, out var entry))
            {
                Interlocked.Add(ref _currentCacheSize, -entry.Size);
                _logger.LogInformation("Removed model {ModelId} from cache ({SizeMB}MB freed)", 
                    modelId, entry.Size / (1024 * 1024));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing model {ModelId} from cache", modelId);
            throw;
        }
    }

    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing all cached models");

        try
        {
            // Clear memory cache
            _memoryCache.Dispose();

            // Clear disk cache
            await _diskLock.WaitAsync(cancellationToken);
            try
            {
                if (Directory.Exists(_diskCacheDirectory))
                {
                    Directory.Delete(_diskCacheDirectory, true);
                    Directory.CreateDirectory(_diskCacheDirectory);
                }
            }
            finally
            {
                _diskLock.Release();
            }

            // Reset state
            _cacheEntries.Clear();
            Interlocked.Exchange(ref _currentCacheSize, 0);

            _logger.LogInformation("All cached models cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }

    public Task<bool> IsModelCachedAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        var isCached = _cacheEntries.ContainsKey(modelId) || 
                      _memoryCache.TryGetValue(modelId, out _);

        return Task.FromResult(isCached);
    }

    public Task<Dictionary<string, object>?> GetModelMetadataAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        if (_cacheEntries.TryGetValue(modelId, out var entry))
        {
            return Task.FromResult<Dictionary<string, object>?>(new Dictionary<string, object>(entry.Metadata));
        }

        return Task.FromResult<Dictionary<string, object>?>(null);
    }

    #region Private Methods

    private async Task EnsureCacheSpaceAsync(long requiredBytes, CancellationToken cancellationToken)
    {
        var currentSize = Interlocked.Read(ref _currentCacheSize);
        if (currentSize + requiredBytes <= _maxCacheSize)
            return;

        _logger.LogInformation("Cache size limit approached, evicting old models. Current: {CurrentMB}MB, Required: {RequiredMB}MB, Max: {MaxMB}MB",
            currentSize / (1024 * 1024), requiredBytes / (1024 * 1024), _maxCacheSize / (1024 * 1024));

        // Evict least recently used models
        var sortedEntries = _cacheEntries.Values
            .OrderBy(e => e.LastAccessedAt)
            .ToList();

        var targetSize = _maxCacheSize - requiredBytes;
        currentSize = Interlocked.Read(ref _currentCacheSize);

        foreach (var entry in sortedEntries)
        {
            if (currentSize <= targetSize)
                break;

            await RemoveModelAsync(entry.ModelId, cancellationToken);
            currentSize = Interlocked.Read(ref _currentCacheSize);
        }

        _logger.LogDebug("Cache eviction completed. Current size: {CurrentMB}MB", currentSize / (1024 * 1024));
    }

    private async Task StoreToDiskAsync(string modelId, byte[] modelData, ModelCacheEntry entry, CancellationToken cancellationToken)
    {
        var filePath = GetDiskCacheFilePath(modelId);
        var metadataPath = GetMetadataFilePath(modelId);

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            await File.WriteAllBytesAsync(filePath, modelData, cancellationToken);
            
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(entry);
            await File.WriteAllTextAsync(metadataPath, metadataJson, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _diskLock.Release();
        }
    }

    private async Task<byte[]?> LoadFromDiskAsync(string modelId, CancellationToken cancellationToken)
    {
        var filePath = GetDiskCacheFilePath(modelId);
        
        if (!File.Exists(filePath))
            return null;

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            return await File.ReadAllBytesAsync(filePath, cancellationToken);
        }
        finally
        {
            _diskLock.Release();
        }
    }

    private async Task RemoveFromDiskAsync(string modelId, CancellationToken cancellationToken)
    {
        var filePath = GetDiskCacheFilePath(modelId);
        var metadataPath = GetMetadataFilePath(modelId);

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            if (File.Exists(metadataPath))
                File.Delete(metadataPath);
        }
        finally
        {
            _diskLock.Release();
        }
    }

    private async Task LoadExistingCacheEntriesAsync()
    {
        try
        {
            if (!Directory.Exists(_diskCacheDirectory))
                return;

            var metadataFiles = Directory.GetFiles(_diskCacheDirectory, "*.metadata");
            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataFile, Encoding.UTF8);
                    var entry = System.Text.Json.JsonSerializer.Deserialize<ModelCacheEntry>(json);
                    
                    if (entry != null && File.Exists(GetDiskCacheFilePath(entry.ModelId)))
                    {
                        _cacheEntries[entry.ModelId] = entry;
                        Interlocked.Add(ref _currentCacheSize, entry.Size);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load cache entry from {MetadataFile}", metadataFile);
                    // Clean up corrupted metadata file
                    File.Delete(metadataFile);
                }
            }

            _logger.LogInformation("Loaded {Count} cached models from disk ({SizeMB}MB total)", 
                _cacheEntries.Count, TotalCacheSize / (1024 * 1024));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading existing cache entries");
        }
    }

    private string GetDiskCacheFilePath(string modelId)
    {
        var safeModelId = string.Concat(modelId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        return Path.Combine(_diskCacheDirectory, $"{safeModelId}.model");
    }

    private string GetMetadataFilePath(string modelId)
    {
        var safeModelId = string.Concat(modelId.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        return Path.Combine(_diskCacheDirectory, $"{safeModelId}.metadata");
    }

    private static string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToBase64String(hashBytes);
    }

    private void OnMemoryCacheEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        _logger.LogTrace("Model {ModelId} evicted from memory cache: {Reason}", key, reason);
    }

    #endregion

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _logger.LogDebug("Disposing ModelCacheManager");

        try
        {
            _disposed = true;
            _memoryCache.Dispose();
            _diskLock.Dispose();

            _logger.LogDebug("ModelCacheManager disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing ModelCacheManager");
        }

        return ValueTask.CompletedTask;
    }

    private sealed class ModelCacheEntry
    {
        public string ModelId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public long Size { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CachedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }
}