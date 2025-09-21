namespace GameConsole.AI.Core;

/// <summary>
/// Represents an AI model available for use.
/// </summary>
public record AIModel(
    string Id,
    string Name,
    string Provider,
    AIModelCapabilities Capabilities,
    int? MaxTokens = null,
    decimal? CostPerToken = null);

/// <summary>
/// Defines the capabilities of an AI model.
/// </summary>
[Flags]
public enum AIModelCapabilities
{
    /// <summary>No special capabilities.</summary>
    None = 0,
    
    /// <summary>Text completion capabilities.</summary>
    TextCompletion = 1,
    
    /// <summary>Chat/conversation capabilities.</summary>
    Chat = 2,
    
    /// <summary>Code generation capabilities.</summary>
    CodeGeneration = 4,
    
    /// <summary>Streaming response capabilities.</summary>
    Streaming = 8,
    
    /// <summary>Function calling capabilities.</summary>
    FunctionCalling = 16,
    
    /// <summary>Image understanding capabilities.</summary>
    Vision = 32,
    
    /// <summary>Audio processing capabilities.</summary>
    Audio = 64
}

/// <summary>
/// Represents a request to an AI service.
/// </summary>
public record AIRequest
{
    /// <summary>Gets or sets the model ID to use for the request.</summary>
    public required string ModelId { get; init; }
    
    /// <summary>Gets or sets the input prompt or messages.</summary>
    public required string Prompt { get; init; }
    
    /// <summary>Gets or sets the maximum tokens to generate.</summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>Gets or sets the temperature for response randomness (0.0-1.0).</summary>
    public float? Temperature { get; init; }
    
    /// <summary>Gets or sets whether to stream the response.</summary>
    public bool Stream { get; init; } = false;
    
    /// <summary>Gets or sets additional parameters for the request.</summary>
    public Dictionary<string, object>? Parameters { get; init; }
    
    /// <summary>Gets or sets the request timeout.</summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Represents a response from an AI service.
/// </summary>
public record AIResponse
{
    /// <summary>Gets or sets the generated content.</summary>
    public required string Content { get; init; }
    
    /// <summary>Gets or sets the model used for generation.</summary>
    public required string ModelId { get; init; }
    
    /// <summary>Gets or sets token usage statistics.</summary>
    public AITokenUsage? TokenUsage { get; init; }
    
    /// <summary>Gets or sets the response timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>Gets or sets additional response metadata.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents a chunk of a streaming AI response.
/// </summary>
public record AIResponseChunk
{
    /// <summary>Gets or sets the content chunk.</summary>
    public required string Content { get; init; }
    
    /// <summary>Gets or sets whether this is the final chunk.</summary>
    public bool IsFinal { get; init; }
    
    /// <summary>Gets or sets the chunk index.</summary>
    public int Index { get; init; }
}

/// <summary>
/// Represents token usage statistics for an AI request.
/// </summary>
public record AITokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal? EstimatedCost = null);

/// <summary>
/// Represents the health status of an AI service.
/// </summary>
public record AIHealthStatus
{
    /// <summary>Gets or sets whether the service is healthy.</summary>
    public required bool IsHealthy { get; init; }
    
    /// <summary>Gets or sets the service status description.</summary>
    public string? Status { get; init; }
    
    /// <summary>Gets or sets the response time in milliseconds.</summary>
    public double? ResponseTimeMs { get; init; }
    
    /// <summary>Gets or sets the timestamp of the health check.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>Gets or sets any error information if unhealthy.</summary>
    public string? Error { get; init; }
}