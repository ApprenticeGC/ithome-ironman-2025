using GameConsole.Core.Abstractions;
using GameConsole.Graphics.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Numerics;

namespace GameConsole.Graphics.Services;

/// <summary>
/// 3D mesh loading and rendering service supporting common formats (OBJ, FBX).
/// Provides efficient mesh batching and GPU buffer management.
/// </summary>
[Service("Mesh Service", "1.0.0", "3D mesh loading and rendering with format support for OBJ, FBX", 
         Categories = new[] { "Graphics", "3D", "Meshes" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class MeshService : BaseGraphicsService, IMeshCapability
{
    private readonly ConcurrentDictionary<string, Mesh> _meshCache = new();
    private readonly object _loadingLock = new();
    private long _totalMeshMemory = 0;
    private const long MaxMeshMemory = 256 * 1024 * 1024; // 256MB limit

    private static readonly string[] SupportedFormats = { ".obj", ".fbx", ".dae", ".gltf", ".glb" };

    public MeshService(ILogger logger) : base(logger)
    {
    }

    #region BaseGraphicsService Overrides

    public override IMeshCapability MeshManager => this;

    public override Task BeginFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for mesh service
        return Task.CompletedTask;
    }

    public override Task EndFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for mesh service
        return Task.CompletedTask;
    }

    public override Task ClearAsync(Vector4 color, CancellationToken cancellationToken = default)
    {
        // Not applicable for mesh service
        return Task.CompletedTask;
    }

    public override Task SetViewportAsync(int x, int y, int width, int height, CancellationToken cancellationToken = default)
    {
        // Not applicable for mesh service
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
        _logger.LogDebug("Initializing mesh loading system");
        _logger.LogDebug("Supported formats: {Formats}", string.Join(", ", SupportedFormats));
        _logger.LogDebug("Max mesh memory: {MaxMemoryMB}MB", MaxMeshMemory / (1024 * 1024));
        
        // Load basic primitive meshes
        LoadBuiltInMeshes();
        
        return Task.CompletedTask;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Mesh loading system started");
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping mesh loading system");
        return Task.CompletedTask;
    }

    protected override ValueTask OnDisposeAsync()
    {
        _logger.LogInformation("Disposing {MeshCount} cached meshes", _meshCache.Count);
        
        // Unload all cached meshes
        foreach (var meshId in _meshCache.Keys.ToList())
        {
            UnloadMeshInternal(meshId);
        }
        
        _meshCache.Clear();
        return ValueTask.CompletedTask;
    }

    #endregion

    #region IMeshCapability Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IMeshCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IMeshCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IMeshCapability))
            return Task.FromResult(this as T);
        return Task.FromResult<T?>(null);
    }

    public async Task<Mesh> LoadMeshAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Mesh path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Mesh file not found: {path}");

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (!SupportedFormats.Contains(extension))
            throw new NotSupportedException($"Mesh format {extension} is not supported. Supported formats: {string.Join(", ", SupportedFormats)}");

        var meshId = Path.GetFileNameWithoutExtension(path);

        // Check cache first
        if (_meshCache.TryGetValue(meshId, out var cachedMesh))
        {
            _logger.LogTrace("Mesh {MeshId} found in cache", meshId);
            return cachedMesh;
        }

        // Simulate mesh loading (outside of lock)
        await Task.Delay(100, cancellationToken); // Simulate I/O and parsing

        // Load mesh with locking to prevent duplicate loads
        lock (_loadingLock)
        {
            // Double-check after acquiring lock
            if (_meshCache.TryGetValue(meshId, out cachedMesh))
                return cachedMesh;

            _logger.LogDebug("Loading mesh from {Path} (format: {Format})", path, extension);

            // Create simulated mesh based on format
            var mesh = extension switch
            {
                ".obj" => SimulateObjMeshLoading(meshId),
                ".fbx" => SimulateFbxMeshLoading(meshId),
                ".gltf" or ".glb" => SimulateGltfMeshLoading(meshId),
                _ => SimulateGenericMeshLoading(meshId)
            };

            // Check memory limits
            var estimatedSize = EstimateMeshSize(mesh);
            if (_totalMeshMemory + estimatedSize > MaxMeshMemory)
            {
                _logger.LogWarning("Mesh memory limit would be exceeded. Current: {Current}MB, Adding: {Adding}MB, Limit: {Limit}MB",
                    _totalMeshMemory / (1024 * 1024), estimatedSize / (1024 * 1024), MaxMeshMemory / (1024 * 1024));
                
                // In a real implementation, would implement LRU eviction
                throw new InvalidOperationException("Mesh memory limit exceeded");
            }

            _meshCache[meshId] = mesh;
            _totalMeshMemory += estimatedSize;

            _logger.LogDebug("Loaded mesh {MeshId} ({VertexCount} vertices, {IndexCount} indices). Total memory: {TotalMemoryMB}MB",
                meshId, mesh.VertexCount, mesh.IndexCount, _totalMeshMemory / (1024 * 1024));

            return mesh;
        }
    }

    public Task RenderMeshAsync(string meshId, Matrix4x4 transform, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (string.IsNullOrWhiteSpace(meshId))
            throw new ArgumentException("Mesh ID cannot be null or empty", nameof(meshId));

        if (!_meshCache.TryGetValue(meshId, out var mesh))
            throw new ArgumentException($"Mesh {meshId} not found. Load the mesh first.", nameof(meshId));

        if (!mesh.IsLoaded)
            throw new InvalidOperationException($"Mesh {meshId} is not loaded");

        _logger.LogTrace("Rendering mesh {MeshId} with transform", meshId);

        // Simulate rendering operations
        // In real implementation, would:
        // 1. Set world matrix uniform
        // 2. Bind vertex/index buffers
        // 3. Issue draw call
        
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void LoadBuiltInMeshes()
    {
        // Add basic primitive meshes
        var primitives = new Dictionary<string, (int vertices, int indices)>
        {
            { "cube", (24, 36) },       // 8 vertices * 3 for normals = 24, 12 triangles * 3 = 36 indices
            { "sphere", (1024, 2048) }, // High poly sphere
            { "plane", (4, 6) },        // Simple quad
            { "cylinder", (64, 96) }    // 32 segments * 2 caps
        };

        foreach (var (name, (vertexCount, indexCount)) in primitives)
        {
            var mesh = new Mesh(name, vertexCount, indexCount, true);
            _meshCache[name] = mesh;
            _totalMeshMemory += EstimateMeshSize(mesh);
        }

        _logger.LogDebug("Loaded {Count} built-in primitive meshes", primitives.Count);
    }

    private void UnloadMeshInternal(string meshId)
    {
        if (_meshCache.TryRemove(meshId, out var mesh))
        {
            var estimatedSize = EstimateMeshSize(mesh);
            _totalMeshMemory = Math.Max(0, _totalMeshMemory - estimatedSize);
            
            _logger.LogDebug("Unloaded mesh {MeshId}. Total memory: {TotalMemoryMB}MB",
                meshId, _totalMeshMemory / (1024 * 1024));
        }
    }

    private static Mesh SimulateObjMeshLoading(string meshId)
    {
        // Simulate OBJ parsing (typically produces moderate poly counts)
        return new Mesh(meshId, 1500, 4500, true);
    }

    private static Mesh SimulateFbxMeshLoading(string meshId)
    {
        // Simulate FBX parsing (can produce high poly counts)
        return new Mesh(meshId, 5000, 15000, true);
    }

    private static Mesh SimulateGltfMeshLoading(string meshId)
    {
        // Simulate glTF parsing (optimized format)
        return new Mesh(meshId, 2000, 6000, true);
    }

    private static Mesh SimulateGenericMeshLoading(string meshId)
    {
        // Generic fallback
        return new Mesh(meshId, 1000, 3000, true);
    }

    private static long EstimateMeshSize(Mesh mesh)
    {
        // Estimate memory usage: vertex buffer + index buffer
        // Assume 32 bytes per vertex (position, normal, UV, tangent)
        // Assume 4 bytes per index (32-bit indices)
        return (mesh.VertexCount * 32) + (mesh.IndexCount * 4);
    }

    #endregion
}