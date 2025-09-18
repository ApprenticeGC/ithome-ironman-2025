namespace GameConsole.AI.Local;

/// <summary>
/// Metadata for AI models in the cache.
/// </summary>
public class ModelMetadata
{
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model format.
    /// </summary>
    public ModelFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the model size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model tags for categorization.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional model properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the checksum for integrity verification.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
}

/// <summary>
/// Cached model data and metadata.
/// </summary>
public class CachedModel
{
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model metadata.
    /// </summary>
    public ModelMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the model data stream.
    /// </summary>
    public Stream ModelData { get; set; } = Stream.Null;

    /// <summary>
    /// Gets or sets the cached timestamp.
    /// </summary>
    public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last accessed timestamp.
    /// </summary>
    public DateTimeOffset LastAccessedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the access count.
    /// </summary>
    public long AccessCount { get; set; }

    /// <summary>
    /// Gets or sets whether the model is compressed.
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// Gets or sets the cache file path.
    /// </summary>
    public string? FilePath { get; set; }
}

/// <summary>
/// Information about cached models.
/// </summary>
public class CachedModelInfo
{
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the cached timestamp.
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }

    /// <summary>
    /// Gets or sets the last accessed timestamp.
    /// </summary>
    public DateTimeOffset LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the access count.
    /// </summary>
    public long AccessCount { get; set; }

    /// <summary>
    /// Gets or sets the model format.
    /// </summary>
    public ModelFormat Format { get; set; }

    /// <summary>
    /// Gets or sets whether the model is currently loaded.
    /// </summary>
    public bool IsLoaded { get; set; }
}

/// <summary>
/// Cache statistics and usage information.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Gets or sets the total number of cached models.
    /// </summary>
    public int TotalCachedModels { get; set; }

    /// <summary>
    /// Gets or sets the total cache size in bytes.
    /// </summary>
    public long TotalCacheSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the available cache space in bytes.
    /// </summary>
    public long AvailableCacheSpaceBytes { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate percentage.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the total number of cache requests.
    /// </summary>
    public long TotalCacheRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits.
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses.
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// Gets or sets the number of cache evictions.
    /// </summary>
    public long CacheEvictions { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when statistics were collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the cache usage percentage.
    /// </summary>
    public double CacheUsagePercent => AvailableCacheSpaceBytes > 0 ? 
        (double)TotalCacheSizeBytes / (TotalCacheSizeBytes + AvailableCacheSpaceBytes) * 100 : 0;
}