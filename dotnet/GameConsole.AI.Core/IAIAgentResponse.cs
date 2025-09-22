using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{

/// <summary>
/// Represents a response from an AI agent.
/// This is the base interface for all responses that can be returned by AI agents.
/// </summary>
public interface IAIAgentResponse
{
    /// <summary>
    /// Gets the unique identifier that matches the corresponding request.
    /// </summary>
    string RequestId { get; }

    /// <summary>
    /// Gets the timestamp when the response was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets a value indicating whether the request was processed successfully.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message if the request failed.
    /// This is null if IsSuccess is true.
    /// </summary>
    string ErrorMessage { get; }

    /// <summary>
    /// Gets the time taken to process the request.
    /// </summary>
    TimeSpan ProcessingTime { get; }

    /// <summary>
    /// Gets the response data and metadata.
    /// </summary>
    IReadOnlyDictionary<string, object> Data { get; }
}
}