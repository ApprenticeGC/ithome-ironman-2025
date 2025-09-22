namespace GameConsole.AI.Core;

/// <summary>
/// Base interface for AI agent requests.
/// </summary>
public interface IAIAgentRequest
{
    /// <summary>
    /// Gets the unique identifier for this request.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the timestamp when the request was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the priority of the request (1-10, where 10 is highest priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the timeout for processing this request.
    /// </summary>
    TimeSpan Timeout { get; }

    /// <summary>
    /// Gets additional context or metadata for the request.
    /// </summary>
    IReadOnlyDictionary<string, object> Context { get; }
}

/// <summary>
/// Base interface for AI agent responses.
/// </summary>
public interface IAIAgentResponse
{
    /// <summary>
    /// Gets the request ID this response corresponds to.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the timestamp when the response was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the status of the response.
    /// </summary>
    AIResponseStatus Status { get; }

    /// <summary>
    /// Gets the processing time taken to generate this response.
    /// </summary>
    TimeSpan ProcessingTime { get; }

    /// <summary>
    /// Gets any error information if the response status indicates an error.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets additional metadata about the response.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Represents the status of an AI agent response.
/// </summary>
public enum AIResponseStatus
{
    /// <summary>
    /// The response was processed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The request was processed with warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// The request failed due to an error.
    /// </summary>
    Error,

    /// <summary>
    /// The request timed out during processing.
    /// </summary>
    Timeout,

    /// <summary>
    /// The request was cancelled before completion.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The agent was not capable of processing the request.
    /// </summary>
    NotSupported
}