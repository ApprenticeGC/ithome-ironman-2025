namespace GameConsole.Core.Abstractions;

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
    /// Gets the type of request.
    /// </summary>
    string RequestType { get; }

    /// <summary>
    /// Gets the request payload data.
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// Gets additional context or metadata for the request.
    /// </summary>
    IReadOnlyDictionary<string, object> Context { get; }

    /// <summary>
    /// Gets the timestamp when the request was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }
}

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
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the response data if the request was successful.
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// Gets the error message if the request failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets additional response metadata.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the timestamp when the response was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the duration it took to process the request.
    /// </summary>
    TimeSpan ProcessingDuration { get; }
}

/// <summary>
/// Basic implementation of IAIAgentRequest for common use cases.
/// </summary>
public sealed class AIAgentRequest : IAIAgentRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRequest"/> class.
    /// </summary>
    /// <param name="requestType">The type of request.</param>
    /// <param name="data">The request payload data.</param>
    /// <param name="context">Additional context or metadata.</param>
    public AIAgentRequest(string requestType, object? data = null, IReadOnlyDictionary<string, object>? context = null)
    {
        RequestId = Guid.NewGuid().ToString();
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        Data = data;
        Context = context ?? new Dictionary<string, object>();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string RequestId { get; }

    /// <inheritdoc />
    public string RequestType { get; }

    /// <inheritdoc />
    public object? Data { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Context { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; }
}

/// <summary>
/// Basic implementation of IAIAgentResponse for common use cases.
/// </summary>
public sealed class AIAgentResponse : IAIAgentResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentResponse"/> class for successful responses.
    /// </summary>
    /// <param name="requestId">The unique identifier for the original request.</param>
    /// <param name="data">The response data.</param>
    /// <param name="metadata">Additional response metadata.</param>
    /// <param name="processingDuration">The duration it took to process the request.</param>
    public AIAgentResponse(string requestId, object? data = null, IReadOnlyDictionary<string, object>? metadata = null, TimeSpan processingDuration = default)
    {
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        IsSuccess = true;
        Data = data;
        ErrorMessage = null;
        Metadata = metadata ?? new Dictionary<string, object>();
        CreatedAt = DateTimeOffset.UtcNow;
        ProcessingDuration = processingDuration;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentResponse"/> class for error responses.
    /// </summary>
    /// <param name="requestId">The unique identifier for the original request.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="metadata">Additional response metadata.</param>
    /// <param name="processingDuration">The duration it took to process the request.</param>
    public static AIAgentResponse FromError(string requestId, string errorMessage, IReadOnlyDictionary<string, object>? metadata = null, TimeSpan processingDuration = default)
    {
        return new AIAgentResponse(requestId, errorMessage, metadata, processingDuration, isError: true);
    }

    private AIAgentResponse(string requestId, string errorMessage, IReadOnlyDictionary<string, object>? metadata, TimeSpan processingDuration, bool isError)
    {
        RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
        IsSuccess = false;
        Data = null;
        ErrorMessage = errorMessage;
        Metadata = metadata ?? new Dictionary<string, object>();
        CreatedAt = DateTimeOffset.UtcNow;
        ProcessingDuration = processingDuration;
    }

    /// <inheritdoc />
    public string RequestId { get; }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public object? Data { get; }

    /// <inheritdoc />
    public string? ErrorMessage { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; }

    /// <inheritdoc />
    public TimeSpan ProcessingDuration { get; }
}