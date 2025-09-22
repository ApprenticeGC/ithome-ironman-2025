using System;

namespace GameConsole.AI.Core.Configuration;

/// <summary>
/// Configuration profile for AI agent behavior and resource allocation.
/// Different profiles provide different capabilities for Editor vs Runtime scenarios.
/// </summary>
public class AIProfile
{
    /// <summary>
    /// Gets or sets the task kind that determines AI behavior and resource allocation.
    /// </summary>
    public TaskKind TaskKind { get; set; }

    /// <summary>
    /// Gets or sets the base URL for the AI service endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";

    /// <summary>
    /// Gets or sets the AI model to use for inference.
    /// </summary>
    public string Model { get; set; } = "llama3.1:8b";

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    public int MaxTokens { get; set; } = 256;

    /// <summary>
    /// Gets or sets the temperature for response randomness (0.0 to 1.0).
    /// </summary>
    public float Temperature { get; set; } = 0.6f;

    /// <summary>
    /// Gets or sets the maximum number of parallel AI requests.
    /// </summary>
    public int MaxParallel { get; set; } = 2;

    /// <summary>
    /// Gets or sets the timeout for AI requests.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the list of enabled tool names for this profile.
    /// </summary>
    public string[] EnabledTools { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to use a remote gateway for AI requests.
    /// </summary>
    public bool UseRemoteGateway { get; set; } = false;

    /// <summary>
    /// Gets or sets the gateway host when using remote gateway.
    /// </summary>
    public string? GatewayHost { get; set; }

    /// <summary>
    /// Gets or sets the gateway port when using remote gateway.
    /// </summary>
    public int GatewayPort { get; set; } = 4053;
}

/// <summary>
/// Defines the type of AI task and corresponding resource allocation strategy.
/// </summary>
public enum TaskKind
{
    /// <summary>
    /// Long context, rich tools, higher latency OK - for content authoring.
    /// </summary>
    EditorAuthoring,

    /// <summary>
    /// Asset QA, import validation, batch processing.
    /// </summary>
    EditorAnalysis,

    /// <summary>
    /// Tight budget, low latency, PCG steering.
    /// </summary>
    RuntimeDirector,

    /// <summary>
    /// Player help, lore queries, safe subset only.
    /// </summary>
    RuntimeCodex
}