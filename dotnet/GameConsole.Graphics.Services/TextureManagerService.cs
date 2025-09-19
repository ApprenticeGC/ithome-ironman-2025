using GameConsole.Core.Abstractions;
using GameConsole.Graphics.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Graphics.Services;

/// <summary>
/// Texture management service for loading, caching, and GPU resource management.
/// Provides efficient texture streaming and memory optimization.
/// </summary>
[Service("Texture Manager Service", "1.0.0", "Texture loading and caching service with GPU resource management", 
         Categories = new[] { "Graphics", "Resources", "Textures" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class TextureManagerService : BaseGraphicsService, ITextureManagerCapability
{
    private readonly ConcurrentDictionary<string, Texture2D> _textureCache = new();
    private readonly object _loadingLock = new();
    private long _totalTextureMemory = 0;
    private const long MaxTextureMemory = 512 * 1024 * 1024; // 512MB limit

    public TextureManagerService(ILogger logger) : base(logger)
    {
    }

    #region BaseGraphicsService Overrides

    public override ITextureManagerCapability TextureManager => this;

    public override Task BeginFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for texture manager
        return Task.CompletedTask;
    }

    public override Task EndFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for texture manager
        return Task.CompletedTask;
    }

    public override Task ClearAsync(System.Numerics.Vector4 color, CancellationToken cancellationToken = default)
    {
        // Not applicable for texture manager
        return Task.CompletedTask;
    }

    public override Task SetViewportAsync(int x, int y, int width, int height, CancellationToken cancellationToken = default)
    {
        // Not applicable for texture manager
        return Task.CompletedTask;
    }

    public override Task<GraphicsBackend> GetBackendAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GraphicsBackend.DirectX12);
    }

    #endregion

    #region Service Lifecycle

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing texture management system");
        _logger.LogDebug("Max texture memory: {MaxMemoryMB}MB", MaxTextureMemory / (1024 * 1024));
        return Task.CompletedTask;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Texture management system started");
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping texture management system");
        return Task.CompletedTask;
    }

    protected override ValueTask OnDisposeAsync()
    {
        _logger.LogInformation("Disposing {TextureCount} cached textures", _textureCache.Count);
        
        // Unload all cached textures
        foreach (var textureId in _textureCache.Keys.ToList())
        {
            UnloadTextureInternal(textureId);
        }
        
        _textureCache.Clear();
        return ValueTask.CompletedTask;
    }

    #endregion

    #region ITextureManagerCapability Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(ITextureManagerCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(ITextureManagerCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(ITextureManagerCapability))
            return Task.FromResult(this as T);
        return Task.FromResult<T?>(null);
    }

    public async Task<Texture2D> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Texture path cannot be null or empty", nameof(path));

        var textureId = Path.GetFileNameWithoutExtension(path);

        // Check cache first
        if (_textureCache.TryGetValue(textureId, out var cachedTexture))
        {
            _logger.LogTrace("Texture {TextureId} found in cache", textureId);
            return cachedTexture;
        }

        // Simulate texture loading (outside of lock)
        await Task.Delay(10, cancellationToken); // Simulate I/O
        
        // Load texture with locking to prevent duplicate loads
        lock (_loadingLock)
        {
            // Double-check after acquiring lock
            if (_textureCache.TryGetValue(textureId, out cachedTexture))
                return cachedTexture;

            _logger.LogDebug("Loading texture from {Path}", path);

            // Create simulated texture (in real implementation, would load from file)
            var texture = new Texture2D(
                Id: textureId,
                Width: 1024, // Simulated dimensions
                Height: 1024,
                Format: TextureFormat.RGBA8,
                IsLoaded: true);

            // Check memory limits
            var estimatedSize = EstimateTextureSize(texture);
            if (_totalTextureMemory + estimatedSize > MaxTextureMemory)
            {
                _logger.LogWarning("Texture memory limit would be exceeded. Current: {Current}MB, Adding: {Adding}MB, Limit: {Limit}MB",
                    _totalTextureMemory / (1024 * 1024), estimatedSize / (1024 * 1024), MaxTextureMemory / (1024 * 1024));
                
                // In a real implementation, would implement LRU eviction
                throw new InvalidOperationException("Texture memory limit exceeded");
            }

            _textureCache[textureId] = texture;
            _totalTextureMemory += estimatedSize;

            _logger.LogDebug("Loaded texture {TextureId} ({Width}x{Height}, {Format}). Total memory: {TotalMemoryMB}MB",
                textureId, texture.Width, texture.Height, texture.Format, _totalTextureMemory / (1024 * 1024));

            return texture;
        }
    }

    public Task UnloadTextureAsync(string textureId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(textureId))
            throw new ArgumentException("Texture ID cannot be null or empty", nameof(textureId));

        UnloadTextureInternal(textureId);
        return Task.CompletedTask;
    }

    public Task<Texture2D?> GetTextureAsync(string textureId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(textureId))
            return Task.FromResult<Texture2D?>(null);

        _textureCache.TryGetValue(textureId, out var texture);
        return Task.FromResult<Texture2D?>(texture);
    }

    #endregion

    #region Private Methods

    private void UnloadTextureInternal(string textureId)
    {
        if (_textureCache.TryRemove(textureId, out var texture))
        {
            var estimatedSize = EstimateTextureSize(texture);
            _totalTextureMemory = Math.Max(0, _totalTextureMemory - estimatedSize);
            
            _logger.LogDebug("Unloaded texture {TextureId}. Total memory: {TotalMemoryMB}MB",
                textureId, _totalTextureMemory / (1024 * 1024));
        }
    }

    private static long EstimateTextureSize(Texture2D texture)
    {
        var bytesPerPixel = texture.Format switch
        {
            TextureFormat.RGBA8 => 4,
            TextureFormat.RGB8 => 3,
            TextureFormat.BGRA8 => 4,
            TextureFormat.DXT1 => 1, // Compressed
            TextureFormat.DXT5 => 1, // Compressed
            TextureFormat.BC7 => 1,  // Compressed
            _ => 4
        };

        return texture.Width * texture.Height * bytesPerPixel;
    }

    #endregion
}