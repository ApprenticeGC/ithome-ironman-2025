namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for AI agents in the GameConsole system.
/// Extends IService to provide lifecycle management for AI agent components.
/// </summary>
public interface IAiAgent : IService
{
    /// <summary>
    /// Gets the unique identifier for this AI agent.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the display name of the AI agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of the AI agent's capabilities.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the capabilities provided by this AI agent.
    /// </summary>
    IReadOnlyList<string> Capabilities { get; }

    /// <summary>
    /// Gets the current status of the AI agent.
    /// </summary>
    AiAgentStatus Status { get; }

    /// <summary>
    /// Processes a request asynchronously.
    /// </summary>
    /// <param name="request">The agent request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The agent response.</returns>
    Task<AiAgentResponse> ProcessAsync(AiAgentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the status of an AI agent.
/// </summary>
public enum AiAgentStatus
{
    /// <summary>
    /// Agent is inactive and not ready to process requests.
    /// </summary>
    Inactive,

    /// <summary>
    /// Agent is initializing and preparing for operation.
    /// </summary>
    Initializing,

    /// <summary>
    /// Agent is ready and available to process requests.
    /// </summary>
    Ready,

    /// <summary>
    /// Agent is currently processing a request.
    /// </summary>
    Processing,

    /// <summary>
    /// Agent has encountered an error and may need attention.
    /// </summary>
    Error
}

/// <summary>
/// Represents a request sent to an AI agent.
/// </summary>
public sealed class AiAgentRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentRequest"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this request.</param>
    /// <param name="capability">The capability being requested.</param>
    /// <param name="data">The request data.</param>
    public AiAgentRequest(string id, string capability, object? data = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Capability = capability ?? throw new ArgumentNullException(nameof(capability));
        Data = data;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier for this request.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the capability being requested.
    /// </summary>
    public string Capability { get; }

    /// <summary>
    /// Gets the request data.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Gets the timestamp when the request was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Represents a response from an AI agent.
/// </summary>
public sealed class AiAgentResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentResponse"/> class.
    /// </summary>
    /// <param name="requestId">The ID of the request this response corresponds to.</param>
    /// <param name="success">Whether the request was processed successfully.</param>
    /// <param name="result">The result data, if successful.</param>
    /// <param name="error">The error message, if unsuccessful.</param>
    public AiAgentResponse(string requestId, bool success, object? result = null, string? error = null)
    {
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        Success = success;
        Result = result;
        Error = error;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the ID of the request this response corresponds to.
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// Gets a value indicating whether the request was processed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the result data, if the request was successful.
    /// </summary>
    public object? Result { get; }

    /// <summary>
    /// Gets the error message, if the request was unsuccessful.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the timestamp when the response was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="result">The result data.</param>
    /// <returns>A successful agent response.</returns>
    public static AiAgentResponse CreateSuccess(string requestId, object? result = null)
        => new(requestId, true, result);

    /// <summary>
    /// Creates an error response.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="error">The error message.</param>
    /// <returns>An error agent response.</returns>
    public static AiAgentResponse CreateError(string requestId, string error)
        => new(requestId, false, null, error);
}