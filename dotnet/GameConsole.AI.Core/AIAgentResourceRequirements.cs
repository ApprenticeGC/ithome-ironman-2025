namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of AI agent resource requirements.
/// </summary>
public class AIAgentResourceRequirements : IAIAgentResourceRequirements
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentResourceRequirements"/> class.
    /// </summary>
    /// <param name="minMemoryMB">Minimum memory required in MB.</param>
    /// <param name="recommendedMemoryMB">Recommended memory in MB.</param>
    /// <param name="requiresGPU">Whether GPU acceleration is required.</param>
    /// <param name="requiresNetwork">Whether network connectivity is required.</param>
    /// <param name="maxConcurrentInstances">Maximum concurrent instances supported.</param>
    public AIAgentResourceRequirements(
        int minMemoryMB = 64,
        int recommendedMemoryMB = 256,
        bool requiresGPU = false,
        bool requiresNetwork = false,
        int maxConcurrentInstances = 1)
    {
        MinMemoryMB = minMemoryMB;
        RecommendedMemoryMB = recommendedMemoryMB;
        RequiresGPU = requiresGPU;
        RequiresNetwork = requiresNetwork;
        MaxConcurrentInstances = maxConcurrentInstances;
    }

    /// <inheritdoc />
    public int MinMemoryMB { get; }

    /// <inheritdoc />
    public int RecommendedMemoryMB { get; }

    /// <inheritdoc />
    public bool RequiresGPU { get; }

    /// <inheritdoc />
    public bool RequiresNetwork { get; }

    /// <inheritdoc />
    public int MaxConcurrentInstances { get; }
}