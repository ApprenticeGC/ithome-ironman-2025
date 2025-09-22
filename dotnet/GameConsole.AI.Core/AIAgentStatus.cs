using System;

namespace GameConsole.AI.Core
{

/// <summary>
/// Represents the operational status of an AI agent.
/// </summary>
public enum AIAgentStatus
{
    /// <summary>
    /// The agent is not yet initialized or has been disposed.
    /// </summary>
    NotInitialized,

    /// <summary>
    /// The agent is initializing and not ready to process requests.
    /// </summary>
    Initializing,

    /// <summary>
    /// The agent is ready and available to process requests.
    /// </summary>
    Ready,

    /// <summary>
    /// The agent is currently processing a request but may accept additional requests.
    /// </summary>
    Busy,

    /// <summary>
    /// The agent has reached capacity and cannot accept new requests.
    /// </summary>
    Overloaded,

    /// <summary>
    /// The agent is temporarily unavailable due to maintenance or other issues.
    /// </summary>
    Maintenance,

    /// <summary>
    /// The agent has encountered an error and is not operational.
    /// </summary>
    Error,

    /// <summary>
    /// The agent is shutting down gracefully.
    /// </summary>
    Stopping,

    /// <summary>
    /// The agent has been stopped and is no longer processing requests.
    /// </summary>
    Stopped
}
}