namespace GameConsole.AI.Models;

/// <summary>
/// Contains information about an AI model used by an agent.
/// </summary>
public class AIModelInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIModelInfo"/> class.
    /// </summary>
    /// <param name="name">The name of the model.</param>
    /// <param name="version">The version of the model.</param>
    /// <param name="framework">The AI framework used by this model.</param>
    public AIModelInfo(string name, Version version, AIFrameworkType framework)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Framework = framework;
    }

    /// <summary>
    /// Gets the name of the AI model.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the AI model.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Gets the AI framework used by this model.
    /// </summary>
    public AIFrameworkType Framework { get; }

    /// <summary>
    /// Gets or sets the model size in bytes.
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Gets or sets the path or URI to the model file.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Gets or sets the model's license information.
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Gets or sets additional model-specific properties.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the model's accuracy or performance metrics.
    /// </summary>
    public IDictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
}