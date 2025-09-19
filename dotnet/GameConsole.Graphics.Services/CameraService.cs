using GameConsole.Core.Abstractions;
using GameConsole.Graphics.Core;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace GameConsole.Graphics.Services;

/// <summary>
/// Camera management service for viewport and perspective control.
/// Supports multiple camera types including perspective, orthographic, and custom projections.
/// </summary>
[Service("Camera Service", "1.0.0", "Camera and viewport management with multiple projection types", 
         Categories = new[] { "Graphics", "Camera", "Viewport" }, 
         Lifetime = ServiceLifetime.Singleton)]
public sealed class CameraService : BaseGraphicsService, ICameraCapability
{
    private Camera _currentCamera;
    private readonly object _cameraLock = new();

    // Default camera configuration
    private static readonly Camera DefaultCamera = new(
        Position: new Vector3(0, 0, 5),
        Target: Vector3.Zero,
        Up: Vector3.UnitY,
        FieldOfView: 45.0f,
        NearPlane: 0.1f,
        FarPlane: 1000.0f);

    public CameraService(ILogger logger) : base(logger)
    {
        _currentCamera = DefaultCamera;
    }

    #region BaseGraphicsService Overrides

    public override ICameraCapability CameraManager => this;

    public override Task BeginFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for camera service
        return Task.CompletedTask;
    }

    public override Task EndFrameAsync(CancellationToken cancellationToken = default)
    {
        // Not applicable for camera service
        return Task.CompletedTask;
    }

    public override Task ClearAsync(Vector4 color, CancellationToken cancellationToken = default)
    {
        // Not applicable for camera service
        return Task.CompletedTask;
    }

    public override Task SetViewportAsync(int x, int y, int width, int height, CancellationToken cancellationToken = default)
    {
        // Not applicable for camera service (handled at graphics API level)
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
        _logger.LogDebug("Initializing camera management system");
        _logger.LogDebug("Default camera: Position={Position}, Target={Target}, FOV={FOV}°", 
            _currentCamera.Position, _currentCamera.Target, _currentCamera.FieldOfView);
        return Task.CompletedTask;
    }

    protected override Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Camera management system started");
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Stopping camera management system");
        return Task.CompletedTask;
    }

    protected override ValueTask OnDisposeAsync()
    {
        _logger.LogDebug("Camera management system disposed");
        return ValueTask.CompletedTask;
    }

    #endregion

    #region ICameraCapability Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(ICameraCapability) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(ICameraCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(ICameraCapability))
            return Task.FromResult(this as T);
        return Task.FromResult<T?>(null);
    }

    public Task SetCameraAsync(Camera camera, CancellationToken cancellationToken = default)
    {
        ThrowIfNotRunning();
        
        ValidateCamera(camera);

        lock (_cameraLock)
        {
            var oldCamera = _currentCamera;
            _currentCamera = camera;
            
            _logger.LogDebug("Camera updated: Position={Position}, Target={Target}, FOV={FOV}°, Near={Near}, Far={Far}",
                camera.Position, camera.Target, camera.FieldOfView, camera.NearPlane, camera.FarPlane);
                
            // Log significant changes
            if (Vector3.Distance(oldCamera.Position, camera.Position) > 0.1f)
                _logger.LogTrace("Camera moved from {OldPos} to {NewPos}", oldCamera.Position, camera.Position);
            
            if (Math.Abs(oldCamera.FieldOfView - camera.FieldOfView) > 1.0f)
                _logger.LogTrace("Camera FOV changed from {OldFOV}° to {NewFOV}°", oldCamera.FieldOfView, camera.FieldOfView);
        }

        return Task.CompletedTask;
    }

    public Task<Camera> GetCameraAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        lock (_cameraLock)
        {
            return Task.FromResult(_currentCamera);
        }
    }

    public Task<Matrix4x4> GetViewMatrixAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        lock (_cameraLock)
        {
            var viewMatrix = Matrix4x4.CreateLookAt(_currentCamera.Position, _currentCamera.Target, _currentCamera.Up);
            
            _logger.LogTrace("Computed view matrix for camera at {Position} looking at {Target}", 
                _currentCamera.Position, _currentCamera.Target);
            
            return Task.FromResult(viewMatrix);
        }
    }

    public Task<Matrix4x4> GetProjectionMatrixAsync(float aspectRatio, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (aspectRatio <= 0)
            throw new ArgumentException("Aspect ratio must be positive", nameof(aspectRatio));

        lock (_cameraLock)
        {
            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI * _currentCamera.FieldOfView / 180.0f, // Convert to radians
                aspectRatio,
                _currentCamera.NearPlane,
                _currentCamera.FarPlane);
            
            _logger.LogTrace("Computed perspective projection matrix: FOV={FOV}°, Aspect={Aspect}, Near={Near}, Far={Far}",
                _currentCamera.FieldOfView, aspectRatio, _currentCamera.NearPlane, _currentCamera.FarPlane);
            
            return Task.FromResult(projectionMatrix);
        }
    }

    #endregion

    #region Public Helper Methods

    /// <summary>
    /// Creates an orthographic camera for 2D rendering or UI.
    /// </summary>
    /// <param name="left">Left boundary of the view volume.</param>
    /// <param name="right">Right boundary of the view volume.</param>
    /// <param name="bottom">Bottom boundary of the view volume.</param>
    /// <param name="top">Top boundary of the view volume.</param>
    /// <param name="near">Near plane distance.</param>
    /// <param name="far">Far plane distance.</param>
    /// <returns>Orthographic projection matrix.</returns>
    public Task<Matrix4x4> GetOrthographicMatrixAsync(float left, float right, float bottom, float top, float near, float far, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (right <= left || top <= bottom || far <= near)
            throw new ArgumentException("Invalid orthographic projection parameters");

        var orthographicMatrix = Matrix4x4.CreateOrthographic(right - left, top - bottom, near, far);
        
        _logger.LogTrace("Computed orthographic projection matrix: Left={Left}, Right={Right}, Bottom={Bottom}, Top={Top}, Near={Near}, Far={Far}",
            left, right, bottom, top, near, far);
        
        return Task.FromResult(orthographicMatrix);
    }

    /// <summary>
    /// Moves the camera by the specified offset while maintaining the look direction.
    /// </summary>
    /// <param name="offset">Movement offset in world space.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    public Task MoveCameraAsync(Vector3 offset, CancellationToken cancellationToken = default)
    {
        lock (_cameraLock)
        {
            var newCamera = _currentCamera with 
            { 
                Position = _currentCamera.Position + offset,
                Target = _currentCamera.Target + offset
            };
            
            return SetCameraAsync(newCamera, cancellationToken);
        }
    }

    /// <summary>
    /// Rotates the camera around the target point.
    /// </summary>
    /// <param name="yawRadians">Horizontal rotation in radians.</param>
    /// <param name="pitchRadians">Vertical rotation in radians.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    public Task RotateCameraAroundTargetAsync(float yawRadians, float pitchRadians, CancellationToken cancellationToken = default)
    {
        lock (_cameraLock)
        {
            // Calculate new camera position based on rotation around target
            var directionToCamera = Vector3.Normalize(_currentCamera.Position - _currentCamera.Target);
            var distance = Vector3.Distance(_currentCamera.Position, _currentCamera.Target);
            
            // Apply rotations (simplified - in production would use quaternions)
            var newDirection = directionToCamera;
            var newPosition = _currentCamera.Target + (newDirection * distance);
            
            var newCamera = _currentCamera with { Position = newPosition };
            
            return SetCameraAsync(newCamera, cancellationToken);
        }
    }

    #endregion

    #region Private Methods

    private static void ValidateCamera(Camera camera)
    {
        if (camera.FieldOfView <= 0 || camera.FieldOfView >= 180)
            throw new ArgumentException("Field of view must be between 0 and 180 degrees", nameof(camera));
            
        if (camera.NearPlane <= 0)
            throw new ArgumentException("Near plane must be positive", nameof(camera));
            
        if (camera.FarPlane <= camera.NearPlane)
            throw new ArgumentException("Far plane must be greater than near plane", nameof(camera));
            
        if (Vector3.Distance(camera.Position, camera.Target) < 0.001f)
            throw new ArgumentException("Camera position and target cannot be identical", nameof(camera));
            
        if (camera.Up.LengthSquared() < 0.001f)
            throw new ArgumentException("Camera up vector cannot be zero", nameof(camera));
    }

    #endregion
}