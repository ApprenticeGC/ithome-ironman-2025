using GameConsole.Core.Abstractions;
using GameConsole.Graphics.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Graphics.Services;

/// <summary>
/// Shader compilation and management service supporting HLSL/GLSL at runtime.
/// Provides cross-platform shader compilation with caching and hot-reload support.
/// </summary>
[Service("Shader Service", "1.0.0", "Shader compilation and management with HLSL/GLSL support", 
         Categories = new[] { "Graphics", "Shaders", "Compilation" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class ShaderService : BaseGraphicsService, IShaderCapability
{
    private readonly ConcurrentDictionary<string, Shader> _shaderCache = new();
    private readonly ConcurrentDictionary<string, string> _boundShaders = new(); // Track bound shaders by type
    private readonly object _compilationLock = new();

    public ShaderService(ILogger logger) : base(logger)
    {
    }

    #region BaseGraphicsService Overrides

    public override IShaderCapability ShaderManager => this;

    public override Task BeginFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for shader service
        return Task.CompletedTask;
    }

    public override Task EndFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for shader service  
        return Task.CompletedTask;
    }

    public override Task ClearAsync(System.Numerics.Vector4 color, CancellationToken cancellationToken = default)
    {
        // Not applicable for shader service
        return Task.CompletedTask;
    }

    public override Task SetViewportAsync(int x, int y, int width, int height, CancellationToken cancellationToken = default)
    {
        // Not applicable for shader service
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
        _logger.LogDebug("Initializing shader compilation system");
        
        // Load built-in shaders
        LoadBuiltInShaders();
        
        return Task.CompletedTask;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Shader compilation system started");
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping shader compilation system");
        _boundShaders.Clear();
        return Task.CompletedTask;
    }

    protected override ValueTask OnDisposeAsync()
    {
        _logger.LogInformation("Disposing {ShaderCount} cached shaders", _shaderCache.Count);
        
        _shaderCache.Clear();
        _boundShaders.Clear();
        
        return ValueTask.CompletedTask;
    }

    #endregion

    #region IShaderCapability Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IShaderCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IShaderCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IShaderCapability))
            return Task.FromResult(this as T);
        return Task.FromResult<T?>(null);
    }

    public async Task<Shader> CompileShaderAsync(string source, ShaderType type, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Shader source cannot be null or empty", nameof(source));

        var shaderId = GenerateShaderIdFromSource(source, type);

        // Check cache first
        if (_shaderCache.TryGetValue(shaderId, out var cachedShader))
        {
            _logger.LogTrace("Shader {ShaderId} found in cache", shaderId);
            return cachedShader;
        }

        // Simulate compilation time (outside of lock)
        await Task.Delay(50, cancellationToken);

        // Compile shader with locking to prevent duplicate compilation
        lock (_compilationLock)
        {
            // Double-check after acquiring lock
            if (_shaderCache.TryGetValue(shaderId, out cachedShader))
                return cachedShader;

            _logger.LogDebug("Compiling {ShaderType} shader {ShaderId}", type, shaderId);

            // Simulate compilation (in real implementation, would use graphics API)
            var compiled = SimulateShaderCompilation(source, type);
            if (!compiled)
            {
                throw new InvalidOperationException($"Failed to compile {type} shader");
            }

            var shader = new Shader(
                Id: shaderId,
                Type: type,
                SourcePath: "", // Direct source compilation
                IsCompiled: true);

            _shaderCache[shaderId] = shader;

            _logger.LogDebug("Compiled {ShaderType} shader {ShaderId}", type, shaderId);
            return shader;
        }
    }

    public async Task<Shader> LoadShaderAsync(string path, ShaderType type, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Shader path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Shader file not found: {path}");

        var shaderId = $"{Path.GetFileNameWithoutExtension(path)}_{type}";

        // Check cache first
        if (_shaderCache.TryGetValue(shaderId, out var cachedShader))
        {
            _logger.LogTrace("Shader {ShaderId} found in cache", shaderId);
            return cachedShader;
        }

        _logger.LogDebug("Loading and compiling shader from {Path}", path);

        // Load source code
        var source = await File.ReadAllTextAsync(path, cancellationToken);
        
        // Compile using source compilation
        var shader = await CompileShaderAsync(source, type, cancellationToken);
        
        // Update the shader with file path information
        var fileShader = shader with { SourcePath = path };
        _shaderCache[shaderId] = fileShader;

        _logger.LogDebug("Loaded and compiled shader {ShaderId} from {Path}", shaderId, path);
        return fileShader;
    }

    public Task BindShaderAsync(string shaderId, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (string.IsNullOrWhiteSpace(shaderId))
            throw new ArgumentException("Shader ID cannot be null or empty", nameof(shaderId));

        if (!_shaderCache.TryGetValue(shaderId, out var shader))
            throw new ArgumentException($"Shader {shaderId} not found", nameof(shaderId));

        if (!shader.IsCompiled)
            throw new InvalidOperationException($"Shader {shaderId} is not compiled");

        // Bind shader (simulate by storing current bound shader)
        _boundShaders[shader.Type.ToString()] = shaderId;
        
        _logger.LogTrace("Bound {ShaderType} shader {ShaderId}", shader.Type, shaderId);
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void LoadBuiltInShaders()
    {
        // Add basic built-in shaders (remove unused variables)
        _shaderCache["builtin_vertex"] = new Shader("builtin_vertex", ShaderType.Vertex, "", true);
        _shaderCache["builtin_fragment"] = new Shader("builtin_fragment", ShaderType.Fragment, "", true);

        _logger.LogDebug("Loaded {Count} built-in shaders", 2);
    }

    private static string GenerateShaderIdFromSource(string source, ShaderType type)
    {
        // Generate a simple hash-based ID (in production, would use proper hashing)
        var hash = source.GetHashCode();
        return $"shader_{type}_{Math.Abs(hash)}";
    }

    private bool SimulateShaderCompilation(string source, ShaderType type)
    {
        // Simulate compilation logic (always succeeds for demo)
        var hasMainFunction = source.Contains("main");
        var hasReturnStatement = source.Contains("return");
        
        return hasMainFunction || hasReturnStatement || source.Length > 10;
    }

    #endregion
}