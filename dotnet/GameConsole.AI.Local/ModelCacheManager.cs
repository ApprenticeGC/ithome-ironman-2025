using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace GameConsole.AI.Local;

/// <summary>
/// Implementation of model cache manager for local model storage and retrieval.
/// </summary>
public class ModelCacheManager : IModelCacheManager
{
    private readonly ILogger<ModelCacheManager> _logger;
    private readonly ConcurrentDictionary<string, CachedModelInfo> _cacheIndex = new();
    private readonly ModelCacheConfiguration _configuration;
    private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the ModelCacheManager class.
    /// </summary>
    /// <param name="logger">Logger for the cache manager.</param>
    /// <param name="configuration">Cache configuration settings.</param>
    public ModelCacheManager(ILogger<ModelCacheManager> logger, ModelCacheConfiguration? configuration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? new ModelCacheConfiguration();
        
        InitializeCacheDirectory();
        _ = Task.Run(LoadCacheIndexAsync);
    }

    /// <inheritdoc />
    public async Task CacheModelAsync(string modelId, Stream modelData, ModelMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modelId))
            throw new ArgumentNullException(nameof(modelId));
        if (modelData == null)
            throw new ArgumentNullException(nameof(modelData));
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        await _cacheSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Caching model {ModelId} ({SizeMB} MB)", modelId, metadata.SizeBytes / (1024 * 1024));

            // Check cache size limits
            await EnforceCacheLimitsAsync(metadata.SizeBytes, cancellationToken);

            var modelPath = GetModelPath(modelId);
            var metadataPath = GetMetadataPath(modelId);

            // Save model data
            await using (var fileStream = File.Create(modelPath))
            {
                if (_configuration.EnableCompression)
                {
                    await using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
                    await modelData.CopyToAsync(gzipStream, cancellationToken);
                }
                else
                {
                    await modelData.CopyToAsync(fileStream, cancellationToken);
                }
            }

            // Save metadata
            var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);

            // Update cache index
            var fileInfo = new FileInfo(modelPath);
            var cachedInfo = new CachedModelInfo
            {
                ModelId = modelId,
                Name = metadata.Name,
                SizeBytes = fileInfo.Length,
                CachedAt = DateTimeOffset.UtcNow,
                LastAccessedAt = DateTimeOffset.UtcNow,
                AccessCount = 0,
                Format = metadata.Format,
                IsLoaded = false
            };

            _cacheIndex.AddOrUpdate(modelId, cachedInfo, (_, _) => cachedInfo);
            await SaveCacheIndexAsync(cancellationToken);

            _logger.LogInformation("Model {ModelId} cached successfully", modelId);
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CachedModel?> RetrieveModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modelId))
            return null;

        if (!_cacheIndex.TryGetValue(modelId, out var cachedInfo))
        {
            _logger.LogDebug("Model {ModelId} not found in cache", modelId);
            return null;
        }

        var modelPath = GetModelPath(modelId);
        var metadataPath = GetMetadataPath(modelId);

        if (!File.Exists(modelPath) || !File.Exists(metadataPath))
        {
            _logger.LogWarning("Model {ModelId} files missing, removing from cache index", modelId);
            _cacheIndex.TryRemove(modelId, out _);
            return null;
        }

        try
        {
            // Load metadata
            var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var metadata = JsonSerializer.Deserialize<ModelMetadata>(metadataJson) ?? new ModelMetadata();

            // Create stream for model data
            var fileStream = File.OpenRead(modelPath);
            Stream modelStream = fileStream;

            if (_configuration.EnableCompression)
            {
                modelStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }

            // Update access statistics
            cachedInfo.LastAccessedAt = DateTimeOffset.UtcNow;
            cachedInfo.AccessCount++;

            _logger.LogDebug("Retrieved model {ModelId} from cache", modelId);

            return new CachedModel
            {
                ModelId = modelId,
                Metadata = metadata,
                ModelData = modelStream,
                CachedAt = cachedInfo.CachedAt,
                LastAccessedAt = cachedInfo.LastAccessedAt,
                AccessCount = cachedInfo.AccessCount,
                IsCompressed = _configuration.EnableCompression,
                FilePath = modelPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model {ModelId} from cache", modelId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task InvalidateModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modelId))
            return;

        await _cacheSemaphore.WaitAsync(cancellationToken);
        try
        {
            var modelPath = GetModelPath(modelId);
            var metadataPath = GetMetadataPath(modelId);

            if (File.Exists(modelPath))
            {
                File.Delete(modelPath);
            }

            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            _cacheIndex.TryRemove(modelId, out _);
            await SaveCacheIndexAsync(cancellationToken);

            _logger.LogInformation("Model {ModelId} invalidated and removed from cache", modelId);
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var cacheDirectory = new DirectoryInfo(_configuration.CacheDirectory);
        var totalSize = 0L;
        var modelCount = 0;

        if (cacheDirectory.Exists)
        {
            var files = cacheDirectory.GetFiles("*.model", SearchOption.TopDirectoryOnly);
            totalSize = files.Sum(f => f.Length);
            modelCount = files.Length;
        }

        var totalRequests = _cacheIndex.Values.Sum(c => c.AccessCount);
        var availableSpace = GetAvailableDiskSpace();

        return Task.FromResult(new CacheStatistics
        {
            TotalCachedModels = modelCount,
            TotalCacheSizeBytes = totalSize,
            AvailableCacheSpaceBytes = availableSpace,
            CacheHitRate = CalculateCacheHitRate(),
            TotalCacheRequests = totalRequests,
            CacheHits = totalRequests, // Simplified - in real implementation, track separately
            CacheMisses = 0, // Simplified
            CacheEvictions = 0, // Would track in real implementation
            CollectedAt = DateTimeOffset.UtcNow
        });
    }

    /// <inheritdoc />
    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cacheSemaphore.WaitAsync(cancellationToken);
        try
        {
            var cacheDirectory = new DirectoryInfo(_configuration.CacheDirectory);
            if (cacheDirectory.Exists)
            {
                foreach (var file in cacheDirectory.GetFiles())
                {
                    file.Delete();
                }
            }

            _cacheIndex.Clear();
            await SaveCacheIndexAsync(cancellationToken);

            _logger.LogInformation("Cache cleared successfully");
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task PerformMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        await _cacheSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Performing cache maintenance");

            // Remove orphaned files
            await RemoveOrphanedFilesAsync(cancellationToken);

            // Enforce cache size limits
            await EnforceCacheLimitsAsync(0, cancellationToken);

            // Clean up old entries based on eviction policy
            await ApplyEvictionPolicyAsync(cancellationToken);

            _logger.LogInformation("Cache maintenance completed");
        }
        finally
        {
            _cacheSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public Task<bool> IsModelCachedAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modelId))
            return Task.FromResult(false);

        if (!_cacheIndex.ContainsKey(modelId))
            return Task.FromResult(false);

        var modelPath = GetModelPath(modelId);
        return Task.FromResult(File.Exists(modelPath));
    }

    /// <inheritdoc />
    public Task<IEnumerable<CachedModelInfo>> GetCachedModelsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cacheIndex.Values.AsEnumerable());
    }

    private void InitializeCacheDirectory()
    {
        if (!Directory.Exists(_configuration.CacheDirectory))
        {
            Directory.CreateDirectory(_configuration.CacheDirectory);
            _logger.LogInformation("Created cache directory: {Directory}", _configuration.CacheDirectory);
        }
    }

    private string GetModelPath(string modelId) => 
        Path.Combine(_configuration.CacheDirectory, $"{modelId}.model");

    private string GetMetadataPath(string modelId) => 
        Path.Combine(_configuration.CacheDirectory, $"{modelId}.metadata");

    private string GetIndexPath() => 
        Path.Combine(_configuration.CacheDirectory, "cache-index.json");

    private async Task LoadCacheIndexAsync()
    {
        var indexPath = GetIndexPath();
        if (!File.Exists(indexPath))
            return;

        try
        {
            var indexJson = await File.ReadAllTextAsync(indexPath);
            var index = JsonSerializer.Deserialize<Dictionary<string, CachedModelInfo>>(indexJson);
            
            if (index != null)
            {
                foreach (var kvp in index)
                {
                    _cacheIndex.TryAdd(kvp.Key, kvp.Value);
                }
            }

            _logger.LogInformation("Loaded cache index with {Count} entries", _cacheIndex.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cache index, starting with empty cache");
        }
    }

    private async Task SaveCacheIndexAsync(CancellationToken cancellationToken)
    {
        try
        {
            var indexPath = GetIndexPath();
            var indexData = _cacheIndex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var indexJson = JsonSerializer.Serialize(indexData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(indexPath, indexJson, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cache index");
        }
    }

    private async Task EnforceCacheLimitsAsync(long additionalSize, CancellationToken cancellationToken)
    {
        var stats = await GetCacheStatisticsAsync(cancellationToken);
        var maxCacheSize = _configuration.MaxCacheSizeMB * 1024 * 1024;
        var currentSize = stats.TotalCacheSizeBytes;

        if (currentSize + additionalSize > maxCacheSize)
        {
            _logger.LogInformation("Cache size limit exceeded, performing cleanup");
            await ApplyEvictionPolicyAsync(cancellationToken);
        }
    }

    private async Task ApplyEvictionPolicyAsync(CancellationToken cancellationToken)
    {
        var modelsToEvict = new List<string>();

        switch (_configuration.EvictionPolicy)
        {
            case CacheEvictionPolicy.LeastRecentlyUsed:
                modelsToEvict = _cacheIndex.Values
                    .OrderBy(c => c.LastAccessedAt)
                    .Take(_cacheIndex.Count / 4) // Evict 25% of models
                    .Select(c => c.ModelId)
                    .ToList();
                break;

            case CacheEvictionPolicy.LeastFrequentlyUsed:
                modelsToEvict = _cacheIndex.Values
                    .OrderBy(c => c.AccessCount)
                    .Take(_cacheIndex.Count / 4)
                    .Select(c => c.ModelId)
                    .ToList();
                break;

            case CacheEvictionPolicy.LargestFirst:
                modelsToEvict = _cacheIndex.Values
                    .OrderByDescending(c => c.SizeBytes)
                    .Take(_cacheIndex.Count / 4)
                    .Select(c => c.ModelId)
                    .ToList();
                break;
        }

        foreach (var modelId in modelsToEvict)
        {
            await InvalidateModelAsync(modelId, cancellationToken);
        }
    }

    private async Task RemoveOrphanedFilesAsync(CancellationToken cancellationToken)
    {
        var cacheDirectory = new DirectoryInfo(_configuration.CacheDirectory);
        if (!cacheDirectory.Exists)
            return;

        var modelFiles = cacheDirectory.GetFiles("*.model");
        foreach (var file in modelFiles)
        {
            var modelId = Path.GetFileNameWithoutExtension(file.Name);
            if (!_cacheIndex.ContainsKey(modelId))
            {
                file.Delete();
                var metadataFile = Path.Combine(_configuration.CacheDirectory, $"{modelId}.metadata");
                if (File.Exists(metadataFile))
                {
                    File.Delete(metadataFile);
                }
                _logger.LogDebug("Removed orphaned model file: {ModelId}", modelId);
            }
        }

        await Task.CompletedTask;
    }

    private double CalculateCacheHitRate()
    {
        var totalRequests = _cacheIndex.Values.Sum(c => c.AccessCount);
        return totalRequests > 0 ? 100.0 : 0.0; // Simplified calculation
    }

    private long GetAvailableDiskSpace()
    {
        try
        {
            var driveInfo = new DriveInfo(_configuration.CacheDirectory);
            return driveInfo.AvailableFreeSpace;
        }
        catch
        {
            return long.MaxValue; // Fallback
        }
    }
}