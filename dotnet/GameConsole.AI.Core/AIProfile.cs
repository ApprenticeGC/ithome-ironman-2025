namespace GameConsole.AI.Services;

/// <summary>
/// AI profile configuration for different AI task types and deployment scenarios.
/// </summary>
public class AIProfile
{
    /// <summary>
    /// Gets or sets the task kind that defines the AI's operational context.
    /// </summary>
    public TaskKind TaskKind { get; set; }

    /// <summary>
    /// Gets or sets the base URL for the AI service endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";

    /// <summary>
    /// Gets or sets the AI model to use for processing.
    /// </summary>
    public string Model { get; set; } = "llama3.1:8b";

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    public int MaxTokens { get; set; } = 256;

    /// <summary>
    /// Gets or sets the temperature for response randomness (0.0-1.0).
    /// </summary>
    public float Temperature { get; set; } = 0.6f;

    /// <summary>
    /// Gets or sets the maximum number of parallel processing operations.
    /// </summary>
    public int MaxParallel { get; set; } = 2;

    /// <summary>
    /// Gets or sets the timeout for AI operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the array of enabled tools for the AI agent.
    /// </summary>
    public string[] EnabledTools { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether to use a remote gateway for AI operations.
    /// </summary>
    public bool UseRemoteGateway { get; set; } = false;

    /// <summary>
    /// Gets or sets the gateway host for remote AI operations.
    /// </summary>
    public string? GatewayHost { get; set; }

    /// <summary>
    /// Gets or sets the gateway port for remote AI operations.
    /// </summary>
    public int GatewayPort { get; set; } = 4053;
}

/// <summary>
/// Defines different AI task kinds with specific operational characteristics.
/// </summary>
public enum TaskKind
{
    /// <summary>
    /// Editor authoring tasks - Long context, rich tools, higher latency acceptable.
    /// </summary>
    EditorAuthoring,

    /// <summary>
    /// Editor analysis tasks - Asset QA, import validation, batch processing.
    /// </summary>
    EditorAnalysis,

    /// <summary>
    /// Runtime director tasks - Tight budget, low latency, procedural content generation steering.
    /// </summary>
    RuntimeDirector,

    /// <summary>
    /// Runtime codex tasks - Player help, lore queries, safe subset only.
    /// </summary>
    RuntimeCodex
}