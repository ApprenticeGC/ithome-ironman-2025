namespace GameConsole.AI.Core;

/// <summary>
/// Tier 1: Base AI service interface for all AI-related operations.
/// Defines the fundamental contract for AI service implementations.
/// </summary>
public interface IAIService : GameConsole.Core.Abstractions.IService, GameConsole.Core.Abstractions.ICapabilityProvider
{
    /// <summary>
    /// Gets the available AI models for this service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns available models.</returns>
    Task<IEnumerable<AIModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a completion for the given prompt using the specified model.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI response.</returns>
    Task<AIResponse> GenerateCompletionAsync(AIRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming completion for the given prompt using the specified model.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable of AI response chunks for streaming.</returns>
    IAsyncEnumerable<AIResponseChunk> GenerateStreamingCompletionAsync(AIRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets health status of the AI service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns health status.</returns>
    Task<AIHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default);
}