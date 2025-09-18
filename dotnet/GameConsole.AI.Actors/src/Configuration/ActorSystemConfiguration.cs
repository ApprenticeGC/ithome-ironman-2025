using Akka.Configuration;

namespace GameConsole.AI.Actors.Configuration;

/// <summary>
/// Configuration for the AI Actor System including clustering, persistence, and mailbox settings
/// </summary>
public class ActorSystemConfiguration
{
    /// <summary>
    /// Gets or sets the actor system name
    /// </summary>
    public string SystemName { get; set; } = "GameConsole-AI";

    /// <summary>
    /// Gets or sets the cluster configuration
    /// </summary>
    public ClusterConfiguration Cluster { get; set; } = new();

    /// <summary>
    /// Gets or sets the persistence configuration
    /// </summary>
    public PersistenceConfiguration Persistence { get; set; } = new();

    /// <summary>
    /// Gets or sets the mailbox configuration
    /// </summary>
    public MailboxConfiguration Mailbox { get; set; } = new();

    /// <summary>
    /// Gets or sets the logging configuration
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets custom configuration values
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();

    /// <summary>
    /// Builds the Akka.NET configuration from the current settings
    /// </summary>
    /// <returns>HOCON configuration for Akka.NET</returns>
    public Config BuildConfiguration()
    {
        var configBuilder = new List<string>();

        // Build seed nodes and roles strings
        var seedNodesStr = string.Join(", ", Cluster.SeedNodes.Select(n => $"\"{n}\""));
        var rolesStr = string.Join(", ", Cluster.Roles.Select(r => $"\"{r}\""));

        // Base actor system configuration
        var baseConfig = @$"
akka {{
    actor {{
        provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
        
        # AI-specific mailbox configuration
        default-mailbox {{
            mailbox-type = ""{Mailbox.DefaultMailboxType}""
            mailbox-capacity = {Mailbox.DefaultCapacity}
        }}
        
        # Custom mailbox for AI priority messages
        ai-priority-mailbox {{
            mailbox-type = ""{Mailbox.PriorityMailboxType}""
            mailbox-capacity = {Mailbox.PriorityCapacity}
        }}
        
        # Serialization configuration
        serializers {{
            
        }}
        serialization-bindings {{
            
        }}
    }}
    
    # Logging configuration
    loggers = [""{Logging.LoggerType}""]
    loglevel = ""{Logging.LogLevel}""
    stdout-loglevel = ""{Logging.StdoutLogLevel}""
    
    # Remote configuration for clustering
    remote {{
        dot-netty.tcp {{
            hostname = ""{Cluster.Hostname}""
            port = {Cluster.Port}
            public-hostname = ""{Cluster.PublicHostname}""
            public-port = {Cluster.PublicPort}
        }}
    }}
    
    # Cluster configuration
    cluster {{
        seed-nodes = [{seedNodesStr}]
        roles = [{rolesStr}]
        
        # Cluster sharding for AI agents
        sharding {{
            state-store-mode = ddata
            remember-entities = {Cluster.RememberEntities.ToString().ToLower()}
            passivate-idle-entity-after = {Cluster.PassivateIdleAfter}
        }}
        
        # Split brain resolver
        split-brain-resolver {{
            active-strategy = {Cluster.SplitBrainResolver.Strategy}
            stable-after = {Cluster.SplitBrainResolver.StableAfter}
        }}
    }}
}}";

        configBuilder.Add(baseConfig);

        // Persistence configuration if enabled
        if (Persistence.Enabled)
        {
            var persistenceConfig = @$"
akka.persistence {{
    journal {{
        plugin = ""{Persistence.JournalPlugin}""
        {Persistence.JournalPlugin} {{
            connection-string = ""{Persistence.ConnectionString}""
            auto-initialize = {Persistence.AutoInitialize.ToString().ToLower()}
        }}
    }}
    
    snapshot-store {{
        plugin = ""{Persistence.SnapshotPlugin}""
        {Persistence.SnapshotPlugin} {{
            connection-string = ""{Persistence.ConnectionString}""
            auto-initialize = {Persistence.AutoInitialize.ToString().ToLower()}
        }}
    }}
}}";
            configBuilder.Add(persistenceConfig);
        }

        // Add custom settings
        foreach (var setting in CustomSettings)
        {
            configBuilder.Add($"{setting.Key} = {setting.Value}");
        }

        var hoconConfig = string.Join("\n", configBuilder);
        return ConfigurationFactory.ParseString(hoconConfig);
    }
}

/// <summary>
/// Cluster-specific configuration settings
/// </summary>
public class ClusterConfiguration
{
    /// <summary>
    /// Gets or sets the hostname for this node
    /// </summary>
    public string Hostname { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the port for this node
    /// </summary>
    public int Port { get; set; } = 4053;

    /// <summary>
    /// Gets or sets the public hostname (for NAT scenarios)
    /// </summary>
    public string PublicHostname { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the public port (for NAT scenarios)
    /// </summary>
    public int PublicPort { get; set; } = 4053;

    /// <summary>
    /// Gets or sets the seed nodes for cluster formation
    /// </summary>
    public List<string> SeedNodes { get; set; } = new() { "akka.tcp://GameConsole-AI@localhost:4053" };

    /// <summary>
    /// Gets or sets the roles for this cluster node
    /// </summary>
    public List<string> Roles { get; set; } = new() { "ai-worker" };

    /// <summary>
    /// Gets or sets whether to remember entities after passivation
    /// </summary>
    public bool RememberEntities { get; set; } = true;

    /// <summary>
    /// Gets or sets the time after which idle entities are passivated
    /// </summary>
    public string PassivateIdleAfter { get; set; } = "10m";

    /// <summary>
    /// Gets or sets the split brain resolver configuration
    /// </summary>
    public SplitBrainResolverConfiguration SplitBrainResolver { get; set; } = new();
}

/// <summary>
/// Split brain resolver configuration
/// </summary>
public class SplitBrainResolverConfiguration
{
    /// <summary>
    /// Gets or sets the strategy for resolving split brain scenarios
    /// </summary>
    public string Strategy { get; set; } = "keep-majority";

    /// <summary>
    /// Gets or sets the time to wait for cluster stability
    /// </summary>
    public string StableAfter { get; set; } = "20s";
}

/// <summary>
/// Persistence configuration settings
/// </summary>
public class PersistenceConfiguration
{
    /// <summary>
    /// Gets or sets whether persistence is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the journal plugin to use
    /// </summary>
    public string JournalPlugin { get; set; } = "akka.persistence.journal.inmem";

    /// <summary>
    /// Gets or sets the snapshot store plugin to use
    /// </summary>
    public string SnapshotPlugin { get; set; } = "akka.persistence.snapshot-store.inmem";

    /// <summary>
    /// Gets or sets the connection string for persistence store
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Gets or sets whether to auto-initialize the persistence store
    /// </summary>
    public bool AutoInitialize { get; set; } = true;
}

/// <summary>
/// Mailbox configuration settings
/// </summary>
public class MailboxConfiguration
{
    /// <summary>
    /// Gets or sets the default mailbox type
    /// </summary>
    public string DefaultMailboxType { get; set; } = "Akka.Dispatch.UnboundedMailbox, Akka";

    /// <summary>
    /// Gets or sets the default mailbox capacity
    /// </summary>
    public int DefaultCapacity { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the priority mailbox type for AI messages
    /// </summary>
    public string PriorityMailboxType { get; set; } = "Akka.Dispatch.UnboundedPriorityMailbox, Akka";

    /// <summary>
    /// Gets or sets the priority mailbox capacity
    /// </summary>
    public int PriorityCapacity { get; set; } = 2000;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Gets or sets the logger type
    /// </summary>
    public string LoggerType { get; set; } = "Akka.Event.StandardOutLogger, Akka";

    /// <summary>
    /// Gets or sets the log level
    /// </summary>
    public string LogLevel { get; set; } = "INFO";

    /// <summary>
    /// Gets or sets the stdout log level
    /// </summary>
    public string StdoutLogLevel { get; set; } = "INFO";
}