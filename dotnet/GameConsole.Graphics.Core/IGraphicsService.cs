using GameConsole.Core.Abstractions;
using System.Numerics;

namespace GameConsole.Graphics.Core;

/// <summary>
/// Main graphics service interface for rendering operations.
/// Provides a unified abstraction layer over different rendering backends (OpenGL, DirectX, Vulkan).
/// </summary>
public interface IGraphicsService : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the currently active rendering backend.
    /// </summary>
    IRenderingBackend Backend { get; }

    /// <summary>
    /// Gets the current render context for this graphics service.
    /// </summary>
    IRenderContext RenderContext { get; }

    /// <summary>
    /// Creates a new render command buffer for batching rendering operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a new command buffer.</returns>
    Task<IRenderCommandBuffer> CreateCommandBufferAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a command buffer for execution on the GPU.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async execution operation.</returns>
    Task SubmitCommandBufferAsync(IRenderCommandBuffer commandBuffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Presents the current frame to the display.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async present operation.</returns>
    Task PresentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for all pending GPU operations to complete.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async wait operation.</returns>
    Task WaitForIdleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the graphics profiler for performance monitoring and debugging.
    /// </summary>
    IGraphicsProfiler Profiler { get; }
}