namespace GameConsole.AI.Core;

/// <summary>
/// Represents a request to an AI agent.
/// </summary>
public interface IAIAgentRequest
{
    /// <summary>
    /// Gets the unique identifier for this request.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the type of request (e.g., "query", "command", "analysis").
    /// </summary>
    string RequestType { get; }

    /// <summary>
    /// Gets the content of the request.
    /// </summary>
    object Content { get; }

    /// <summary>
    /// Gets additional context and parameters for the request.
    /// </summary>
    IReadOnlyDictionary<string, object> Context { get; }

    /// <summary>
    /// Gets the timestamp when the request was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}