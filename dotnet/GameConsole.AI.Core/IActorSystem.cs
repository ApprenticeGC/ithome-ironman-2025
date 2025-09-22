using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Configuration options for the actor system.
/// </summary>
public class ActorSystemOptions
{
    /// <summary>
    /// Gets or sets the name of the actor system.
    /// </summary>
    public string SystemName { get; set; } = "GameConsole-ActorSystem";
    
    /// <summary>
    /// Gets or sets the maximum number of messages that can be queued per actor.
    /// </summary>
    public int MaxMailboxSize { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the timeout for actor initialization.
    /// </summary>
    public TimeSpan ActorInitializationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Gets or sets the timeout for actor shutdown.
    /// </summary>
    public TimeSpan ActorShutdownTimeout { get; set; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Gets or sets whether clustering is enabled.
    /// </summary>
    public bool ClusteringEnabled { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the cluster configuration.
    /// </summary>
    public ClusterOptions? ClusterOptions { get; set; }
}

/// <summary>
/// Configuration options for actor clustering.
/// </summary>
public class ClusterOptions
{
    /// <summary>
    /// Gets or sets the cluster name.
    /// </summary>
    public string ClusterName { get; set; } = "GameConsole-Cluster";
    
    /// <summary>
    /// Gets or sets the list of seed nodes for cluster discovery.
    /// </summary>
    public List<string> SeedNodes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the port for cluster communication.
    /// </summary>
    public int Port { get; set; } = 8080;
    
    /// <summary>
    /// Gets or sets the hostname for this cluster node.
    /// </summary>
    public string Hostname { get; set; } = "localhost";
    
    /// <summary>
    /// Gets or sets the heartbeat interval for cluster health monitoring.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Gets or sets the timeout for cluster member detection.
    /// </summary>
    public TimeSpan MemberTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Statistics about the actor system performance.
/// </summary>
public class ActorSystemStatistics
{
    /// <summary>
    /// Gets or sets the total number of active actors.
    /// </summary>
    public int TotalActors { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of messages processed.
    /// </summary>
    public long MessagesProcessed { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of messages currently queued.
    /// </summary>
    public int QueuedMessages { get; set; }
    
    /// <summary>
    /// Gets or sets the number of failed message deliveries.
    /// </summary>
    public long FailedDeliveries { get; set; }
    
    /// <summary>
    /// Gets or sets the average message processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTime { get; set; }
    
    /// <summary>
    /// Gets or sets the number of active cluster members.
    /// </summary>
    public int ClusterMembers { get; set; }
    
    /// <summary>
    /// Gets or sets the uptime of the actor system.
    /// </summary>
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Event arguments for actor lifecycle events.
/// </summary>
public class ActorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the address of the actor.
    /// </summary>
    public ActorAddress Address { get; }
    
    /// <summary>
    /// Gets optional additional data about the event.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the ActorEventArgs class.
    /// </summary>
    /// <param name="address">The address of the actor.</param>
    /// <param name="data">Optional additional data about the event.</param>
    public ActorEventArgs(ActorAddress address, object? data = null)
    {
        Address = address;
        Data = data;
    }
}

/// <summary>
/// Tier 2: Actor system service interface for managing actor lifecycle and messaging.
/// Handles actor creation, message routing, and system-wide coordination
/// in a distributed, fault-tolerant manner.
/// </summary>
public interface IActorSystem : IService, ICapabilityProvider
{
    /// <summary>
    /// Event raised when a new actor is created.
    /// </summary>
    event EventHandler<ActorEventArgs>? ActorCreated;
    
    /// <summary>
    /// Event raised when an actor is terminated.
    /// </summary>
    event EventHandler<ActorEventArgs>? ActorTerminated;
    
    /// <summary>
    /// Event raised when an actor encounters an unhandled error.
    /// </summary>
    event EventHandler<ActorEventArgs>? ActorError;

    /// <summary>
    /// Gets the configuration options for this actor system.
    /// </summary>
    ActorSystemOptions Options { get; }
    
    /// <summary>
    /// Gets the name of this actor system.
    /// </summary>
    string SystemName { get; }
    
    /// <summary>
    /// Gets whether clustering is enabled and active.
    /// </summary>
    bool IsClusteringEnabled { get; }

    /// <summary>
    /// Creates a new actor with the specified name and factory function.
    /// </summary>
    /// <param name="name">The name of the actor.</param>
    /// <param name="factory">Factory function to create the actor instance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the actor address.</returns>
    Task<ActorAddress> SpawnAsync(string name, Func<IActorContext, IActor> factory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new actor as a child of the specified parent.
    /// </summary>
    /// <param name="parent">The address of the parent actor.</param>
    /// <param name="name">The name of the child actor.</param>
    /// <param name="factory">Factory function to create the actor instance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the child actor address.</returns>
    Task<ActorAddress> SpawnChildAsync(ActorAddress parent, string name, Func<IActorContext, IActor> factory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a message to the specified actor address.
    /// </summary>
    /// <param name="target">The target actor address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="sender">The sender actor address, if any.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async send operation.</returns>
    Task SendAsync(ActorAddress target, IActorMessage message, ActorAddress? sender = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Terminates the specified actor and all its children.
    /// </summary>
    /// <param name="address">The address of the actor to terminate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async termination operation.</returns>
    Task TerminateActorAsync(ActorAddress address, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resolves an actor address to verify it exists in the system.
    /// </summary>
    /// <param name="address">The address to resolve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the actor exists.</returns>
    Task<bool> ResolveActorAsync(ActorAddress address, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all child addresses of the specified parent actor.
    /// </summary>
    /// <param name="parent">The address of the parent actor.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns child addresses.</returns>
    Task<IEnumerable<ActorAddress>> GetChildrenAsync(ActorAddress parent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets performance and health statistics for the actor system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns system statistics.</returns>
    Task<ActorSystemStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active actor addresses in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns actor addresses.</returns>
    Task<IEnumerable<ActorAddress>> GetAllActorsAsync(CancellationToken cancellationToken = default);
}