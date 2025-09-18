using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Local;

/// <summary>
/// Tier 1: Local AI runtime service interface for local AI execution and deployment.
/// Manages the lifecycle of local AI inference, resource allocation, and model orchestration
/// across different AI frameworks with optimization and fallback capabilities.
/// </summary>
public interface ILocalAIRuntime : IService
{
    /// <summary>
    /// Configures the runtime with deployment settings and initializes resource management.
    /// </summary>
    /// <param name="configuration">Runtime configuration settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration operation.</returns>
    Task ConfigureRuntimeAsync(LocalAIConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current capabilities of the local AI runtime including supported frameworks and resource limits.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns runtime capabilities.</returns>
    Task<AIRuntimeCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes AI inference with automatic resource management and optimization.
    /// </summary>
    /// <param name="request">Inference request with model and input data.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns inference results.</returns>
    Task<AIInferenceResult> ExecuteInferenceAsync(AIInferenceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current runtime performance metrics and resource usage statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns performance metrics.</returns>
    Task<AIPerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes resource allocation based on current usage patterns and performance metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async optimization operation.</returns>
    Task OptimizeResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of supported AI frameworks and their current status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns supported frameworks.</returns>
    Task<IEnumerable<SupportedFramework>> GetSupportedFrameworksAsync(CancellationToken cancellationToken = default);
}