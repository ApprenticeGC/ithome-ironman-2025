namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Defines deployment strategies for different environments and risk levels.
/// </summary>
public enum DeploymentStrategy
{
    /// <summary>
    /// Standard rolling deployment across all instances.
    /// </summary>
    Rolling,

    /// <summary>
    /// Blue-green deployment with environment swap.
    /// </summary>
    BlueGreen,

    /// <summary>
    /// Canary deployment to a small subset before full rollout.
    /// </summary>
    Canary,

    /// <summary>
    /// A/B testing deployment for feature experimentation.
    /// </summary>
    ABTesting,

    /// <summary>
    /// Immediate deployment to all instances simultaneously.
    /// </summary>
    Immediate,

    /// <summary>
    /// Gradual deployment with configurable percentage rollout.
    /// </summary>
    Gradual,

    /// <summary>
    /// Recreation deployment that terminates old instances before creating new ones.
    /// </summary>
    Recreate
}