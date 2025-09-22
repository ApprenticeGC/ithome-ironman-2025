namespace GameConsole.AI.Clustering.Configuration;

/// <summary>
/// Configuration for AI cluster management
/// </summary>
public class AIClusterConfig
{
    /// <summary>
    /// Name of the actor system
    /// </summary>
    public string ActorSystemName { get; set; } = "GameConsole-AI-Cluster";

    /// <summary>
    /// Port for cluster communication
    /// </summary>
    public int ClusterPort { get; set; } = 8080;

    /// <summary>
    /// Seed nodes for cluster bootstrapping
    /// </summary>
    public List<string> SeedNodes { get; set; } = new();

    /// <summary>
    /// Node capabilities for this instance
    /// </summary>
    public List<string> NodeCapabilities { get; set; } = new();

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum cluster size
    /// </summary>
    public int MaxClusterSize { get; set; } = 10;

    /// <summary>
    /// Minimum cluster size for high availability
    /// </summary>
    public int MinClusterSize { get; set; } = 2;

    /// <summary>
    /// Node discovery method (static, consul, kubernetes, etc.)
    /// </summary>
    public string DiscoveryMethod { get; set; } = "static";

    /// <summary>
    /// Timeout for cluster operations in seconds
    /// </summary>
    public int ClusterOperationTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Enable cluster sharding
    /// </summary>
    public bool EnableSharding { get; set; } = true;

    /// <summary>
    /// Number of shards for cluster sharding
    /// </summary>
    public int NumberOfShards { get; set; } = 100;

    /// <summary>
    /// Split brain resolver strategy
    /// </summary>
    public string SplitBrainResolverStrategy { get; set; } = "keep-majority";

    /// <summary>
    /// Load balancing strategy
    /// </summary>
    public string LoadBalancingStrategy { get; set; } = "round-robin";
}

/// <summary>
/// Node capabilities that can be provided by cluster nodes
/// </summary>
public static class NodeCapabilities
{
    public const string DialogueAgent = "dialogue-agent";
    public const string CodeGenerationAgent = "code-generation-agent";
    public const string AssetAnalysisAgent = "asset-analysis-agent";
    public const string WorkflowOrchestration = "workflow-orchestration";
    public const string ContextManagement = "context-management";
    public const string BackendManagement = "backend-management";
}

/// <summary>
/// Load balancing strategies
/// </summary>
public static class LoadBalancingStrategies
{
    public const string RoundRobin = "round-robin";
    public const string LeastConnections = "least-connections";
    public const string WeightedRoundRobin = "weighted-round-robin";
    public const string ConsistentHashing = "consistent-hashing";
}