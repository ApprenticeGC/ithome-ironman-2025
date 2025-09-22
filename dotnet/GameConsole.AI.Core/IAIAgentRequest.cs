using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{

/// <summary>
/// Represents a request to an AI agent.
/// This is the base interface for all requests that can be sent to AI agents.
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
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the type of request.
    /// </summary>
    string RequestType { get; }

    /// <summary>
    /// Gets additional metadata and parameters for the request.
    /// </summary>
    IReadOnlyDictionary<string, object> Parameters { get; }
}
}