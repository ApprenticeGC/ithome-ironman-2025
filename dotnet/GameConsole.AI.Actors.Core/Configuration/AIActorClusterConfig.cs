namespace GameConsole.AI.Actors.Core.Configuration;

/// <summary>
/// Configuration settings for the AI Actor Cluster system.
/// </summary>
public class AIActorClusterConfig
{
    /// <summary>
    /// Gets or sets the cluster system name.
    /// </summary>
    public string SystemName { get; set; } = "GameConsole-AI";
    
    /// <summary>
    /// Gets or sets the hostname for this cluster node.
    /// </summary>
    public string Hostname { get; set; } = "127.0.0.1";
    
    /// <summary>
    /// Gets or sets the port for this cluster node.
    /// </summary>
    public int Port { get; set; } = 8080;
    
    /// <summary>
    /// Gets or sets the list of seed nodes for cluster discovery.
    /// </summary>
    public List<string> SeedNodes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the roles for this cluster node.
    /// </summary>
    public List<string> Roles { get; set; } = new() { "ai-agent" };
    
    /// <summary>
    /// Gets or sets the minimum number of nodes required for the cluster to be considered up.
    /// </summary>
    public int MinimumClusterSize { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the sharding configuration.
    /// </summary>
    public ShardingConfig Sharding { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the supervision strategy configuration.
    /// </summary>
    public SupervisionConfig Supervision { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the load balancing configuration.
    /// </summary>
    public LoadBalancingConfig LoadBalancing { get; set; } = new();
    
    /// <summary>
    /// Gets or sets whether to enable cluster metrics.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the timeout for cluster operations.
    /// </summary>
    public TimeSpan ClusterTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Configuration for cluster sharding.
/// </summary>
public class ShardingConfig
{
    /// <summary>
    /// Gets or sets the number of shards per agent type.
    /// </summary>
    public int ShardsPerAgentType { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets the maximum number of agents per shard.
    /// </summary>
    public int MaxAgentsPerShard { get; set; } = 100;
    
    /// <summary>
    /// Gets or sets the passivation timeout for inactive agents.
    /// </summary>
    public TimeSpan PassivationTimeout { get; set; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// Gets or sets whether to enable automatic shard rebalancing.
    /// </summary>
    public bool EnableRebalancing { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the interval for rebalancing checks.
    /// </summary>
    public TimeSpan RebalanceInterval { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>
/// Configuration for supervision strategies.
/// </summary>
public class SupervisionConfig
{
    /// <summary>
    /// Gets or sets the maximum number of retries for failed actors.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets the time window for retry counting.
    /// </summary>
    public TimeSpan RetryTimeWindow { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Gets or sets the delay between restart attempts.
    /// </summary>
    public TimeSpan RestartDelay { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// Gets or sets whether to escalate failures to the parent supervisor.
    /// </summary>
    public bool EscalateOnFailure { get; set; } = true;
}

/// <summary>
/// Configuration for load balancing strategies.
/// </summary>
public class LoadBalancingConfig
{
    /// <summary>
    /// Gets or sets the load balancing strategy to use.
    /// </summary>
    public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;
    
    /// <summary>
    /// Gets or sets the weight factor for node capacity calculations.
    /// </summary>
    public double CapacityWeight { get; set; } = 1.0;
    
    /// <summary>
    /// Gets or sets the health check interval for backend services.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>
    /// Gets or sets the circuit breaker configuration.
    /// </summary>
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();
}

/// <summary>
/// Load balancing strategies supported by the cluster.
/// </summary>
public enum LoadBalancingStrategy
{
    /// <summary>
    /// Round-robin distribution of requests.
    /// </summary>
    RoundRobin,
    
    /// <summary>
    /// Least-connections based routing.
    /// </summary>
    LeastConnections,
    
    /// <summary>
    /// Weighted distribution based on node capacity.
    /// </summary>
    Weighted,
    
    /// <summary>
    /// Random distribution of requests.
    /// </summary>
    Random
}

/// <summary>
/// Configuration for circuit breaker protection.
/// </summary>
public class CircuitBreakerConfig
{
    /// <summary>
    /// Gets or sets the maximum number of failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets the time to wait before attempting to close the circuit.
    /// </summary>
    public TimeSpan ResetTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Gets or sets the timeout for individual calls.
    /// </summary>
    public TimeSpan CallTimeout { get; set; } = TimeSpan.FromSeconds(10);
}