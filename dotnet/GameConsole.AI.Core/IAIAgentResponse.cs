namespace GameConsole.AI.Core;

/// <summary>
/// Represents a response from an AI agent.
/// </summary>
public interface IAIAgentResponse
{
    /// <summary>
    /// Gets the unique identifier for the original request.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets a value indicating whether the request was processed successfully.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the response content.
    /// </summary>
    object? Content { get; }

    /// <summary>
    /// Gets the error message if the request failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets additional metadata about the response.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the timestamp when the response was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the processing time for the request.
    /// </summary>
    TimeSpan ProcessingTime { get; }
}