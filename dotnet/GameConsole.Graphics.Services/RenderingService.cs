using GameConsole.Core.Abstractions;
using GameConsole.Graphics.Core;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace GameConsole.Graphics.Services;

/// <summary>
/// Core rendering service handling 2D and 3D drawing operations.
/// Provides DirectX 12/Vulkan abstraction with efficient draw call batching.
/// </summary>
[Service("Rendering Service", "1.0.0", "Core rendering service for 2D and 3D operations", 
         Categories = new[] { "Graphics", "Rendering" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class RenderingService : BaseGraphicsService
{
    private GraphicsBackend _currentBackend = GraphicsBackend.DirectX12;
    private int _currentFrameIndex = 0;
    private readonly object _frameLock = new();
    private bool _inFrame = false;

    // Resource managers as capabilities
    private readonly TextureManagerService _textureManager;
    private readonly ShaderService _shaderService;
    private readonly MeshService _meshService;
    private readonly CameraService _cameraService;

    public RenderingService(ILogger<RenderingService> logger) : base(logger)
    {
        _textureManager = new TextureManagerService(logger);
        _shaderService = new ShaderService(logger);
        _meshService = new MeshService(logger);
        _cameraService = new CameraService(logger);
    }

    #region Capability Properties

    public override ITextureManagerCapability TextureManager => _textureManager;
    public override IShaderCapability ShaderManager => _shaderService;
    public override IMeshCapability MeshManager => _meshService;
    public override ICameraCapability CameraManager => _cameraService;

    #endregion

    #region Service Lifecycle

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing graphics backend: {Backend}", _currentBackend);

        // Initialize resource managers
        await _textureManager.InitializeAsync(cancellationToken);
        await _shaderService.InitializeAsync(cancellationToken);
        await _meshService.InitializeAsync(cancellationToken);
        await _cameraService.InitializeAsync(cancellationToken);

        _logger.LogDebug("Graphics backend initialized");
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting graphics rendering");

        // Start resource managers
        await _textureManager.StartAsync(cancellationToken);
        await _shaderService.StartAsync(cancellationToken);
        await _meshService.StartAsync(cancellationToken);
        await _cameraService.StartAsync(cancellationToken);

        _logger.LogDebug("Graphics rendering started");
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping graphics rendering");

        // Stop resource managers
        await _cameraService.StopAsync(cancellationToken);
        await _meshService.StopAsync(cancellationToken);
        await _shaderService.StopAsync(cancellationToken);
        await _textureManager.StopAsync(cancellationToken);

        _logger.LogDebug("Graphics rendering stopped");
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await _cameraService.DisposeAsync();
        await _meshService.DisposeAsync();
        await _shaderService.DisposeAsync();
        await _textureManager.DisposeAsync();
    }

    #endregion

    #region Rendering Operations

    public override Task BeginFrameAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        lock (_frameLock)
        {
            if (_inFrame)
                throw new InvalidOperationException("Frame already in progress. Call EndFrameAsync first.");
                
            _inFrame = true;
            _currentFrameIndex++;
        }

        _logger.LogTrace("Begin frame {FrameIndex}", _currentFrameIndex);

        // Simulate frame begin operations
        return Task.CompletedTask;
    }

    public override Task EndFrameAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        lock (_frameLock)
        {
            if (!_inFrame)
                throw new InvalidOperationException("No frame in progress. Call BeginFrameAsync first.");
                
            _inFrame = false;
        }

        _logger.LogTrace("End frame {FrameIndex}", _currentFrameIndex);

        // Simulate present/swap operations
        return Task.CompletedTask;
    }

    public override Task ClearAsync(Vector4 color, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (!_inFrame)
            throw new InvalidOperationException("Clear can only be called between BeginFrame and EndFrame");

        _logger.LogTrace("Clear render target with color: {Color}", color);

        // Simulate clear operations
        return Task.CompletedTask;
    }

    public override Task SetViewportAsync(int x, int y, int width, int height, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Viewport dimensions must be positive");

        _logger.LogTrace("Set viewport: ({X}, {Y}, {Width}, {Height})", x, y, width, height);

        // Simulate viewport setting
        return Task.CompletedTask;
    }

    public override Task<GraphicsBackend> GetBackendAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.FromResult(_currentBackend);
    }

    #endregion
}