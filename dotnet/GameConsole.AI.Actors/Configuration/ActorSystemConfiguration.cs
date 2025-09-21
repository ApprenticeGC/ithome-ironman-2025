using Microsoft.Extensions.Configuration;

namespace GameConsole.AI.Actors.Configuration;

/// <summary>
/// Configuration for the AI Actor System.
/// </summary>
public class ActorSystemConfiguration
{
    /// <summary>
    /// Name of the actor system.
    /// </summary>
    public string SystemName { get; set; } = "GameConsole-AI";

    /// <summary>
    /// Actor system configuration section.
    /// </summary>
    public ActorSystemConfig ActorSystem { get; set; } = new();

    /// <summary>
    /// Clustering configuration.
    /// </summary>
    public ClusterConfig Clustering { get; set; } = new();

    /// <summary>
    /// Mailbox configuration for AI workloads.
    /// </summary>
    public MailboxConfig Mailbox { get; set; } = new();

    /// <summary>
    /// Supervision strategy configuration.
    /// </summary>
    public SupervisionConfig Supervision { get; set; } = new();

    /// <summary>
    /// Logging configuration.
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();
}

/// <summary>
/// Actor system configuration.
/// </summary>
public class ActorSystemConfig
{
    /// <summary>
    /// Maximum number of dispatcher threads.
    /// </summary>
    public int MaxDispatcherThreads { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Default dispatcher throughput.
    /// </summary>
    public int DefaultThroughput { get; set; } = 5;

    /// <summary>
    /// Actor creation timeout in milliseconds.
    /// </summary>
    public int ActorCreationTimeoutMs { get; set; } = 30000;
}

/// <summary>
/// Clustering configuration.
/// </summary>
public class ClusterConfig
{
    /// <summary>
    /// Whether clustering is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Cluster roles this node should have.
    /// </summary>
    public string[] Roles { get; set; } = { "ai-worker" };

    /// <summary>
    /// Minimum cluster size required.
    /// </summary>
    public int MinimumClusterSize { get; set; } = 1;

    /// <summary>
    /// Seed nodes for cluster discovery.
    /// </summary>
    public string[] SeedNodes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Port for the actor system.
    /// </summary>
    public int Port { get; set; } = 2552;

    /// <summary>
    /// Hostname for the actor system.
    /// </summary>
    public string Hostname { get; set; } = "localhost";
}

/// <summary>
/// Mailbox configuration for AI message prioritization.
/// </summary>
public class MailboxConfig
{
    /// <summary>
    /// Default mailbox type for AI actors.
    /// </summary>
    public string DefaultMailboxType { get; set; } = "akka.dispatch.UnboundedDequeBasedMailbox";

    /// <summary>
    /// High priority mailbox for critical AI operations.
    /// </summary>
    public string HighPriorityMailboxType { get; set; } = "akka.dispatch.UnboundedPriorityMailbox";

    /// <summary>
    /// Stash capacity for actors that need to stash messages.
    /// </summary>
    public int StashCapacity { get; set; } = 1000;
}

/// <summary>
/// Supervision strategy configuration.
/// </summary>
public class SupervisionConfig
{
    /// <summary>
    /// Default restart strategy for AI actors.
    /// </summary>
    public string DefaultStrategy { get; set; } = "RestartOnFailure";

    /// <summary>
    /// Maximum number of retries within time range.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Time range in seconds for retry counting.
    /// </summary>
    public int TimeRangeSeconds { get; set; } = 60;

    /// <summary>
    /// Escalation timeout in seconds.
    /// </summary>
    public int EscalationTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Logging configuration for actor system.
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// Minimum log level for actor system.
    /// </summary>
    public string LogLevel { get; set; } = "INFO";

    /// <summary>
    /// Whether to log actor lifecycle events.
    /// </summary>
    public bool LogActorLifecycle { get; set; } = true;

    /// <summary>
    /// Whether to log message handling details.
    /// </summary>
    public bool LogMessageHandling { get; set; } = false;

    /// <summary>
    /// Whether to log dead letters.
    /// </summary>
    public bool LogDeadLetters { get; set; } = true;
}