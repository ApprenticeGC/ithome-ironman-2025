using System.Reflection;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Discovery;

/// <summary>
/// Implementation of AI agent discovery using reflection.
/// Scans directories and assemblies to find types implementing IAIAgent.
/// </summary>
public class AIAgentDiscovery : IAIAgentDiscovery
{
    private readonly ILogger<AIAgentDiscovery> _logger;

    public AIAgentDiscovery(ILogger<AIAgentDiscovery> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentMetadata>> DiscoverAgentsAsync(string searchPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(searchPath);

        _logger.LogInformation("Discovering AI agents in directory: {SearchPath}", searchPath);

        if (!Directory.Exists(searchPath))
        {
            _logger.LogWarning("Search path does not exist: {SearchPath}", searchPath);
            return Array.Empty<AgentMetadata>();
        }

        var discoveredAgents = new List<AgentMetadata>();
        var assemblyFiles = Directory.GetFiles(searchPath, "*.dll", SearchOption.AllDirectories);

        foreach (var assemblyFile in assemblyFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var agentsInAssembly = await DiscoverAgentsInAssemblyAsync(assemblyFile, cancellationToken);
                discoveredAgents.AddRange(agentsInAssembly);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover agents in assembly: {AssemblyFile}", assemblyFile);
            }
        }

        _logger.LogInformation("Discovered {Count} AI agents in directory: {SearchPath}", discoveredAgents.Count, searchPath);
        return discoveredAgents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentMetadata>> DiscoverAgentsInAssemblyAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyPath);

        _logger.LogDebug("Scanning assembly for AI agents: {AssemblyPath}", assemblyPath);

        try
        {
            // Load assembly for reflection only
            var assembly = Assembly.LoadFrom(assemblyPath);
            var discoveredAgents = new List<AgentMetadata>();

            await Task.Run(() =>
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (IsValidAgentType(type))
                    {
                        try
                        {
                            var metadata = ExtractAgentMetadata(type);
                            discoveredAgents.Add(metadata);
                            _logger.LogDebug("Discovered agent: {AgentName} ({AgentId})", metadata.Name, metadata.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to extract metadata from agent type: {TypeName}", type.FullName);
                        }
                    }
                }
            }, cancellationToken);

            return discoveredAgents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly: {AssemblyPath}", assemblyPath);
            return Array.Empty<AgentMetadata>();
        }
    }

    /// <inheritdoc />
    public bool IsValidAgentType(Type agentType)
    {
        if (agentType == null || agentType.IsAbstract || agentType.IsInterface)
            return false;

        // Check if type implements IAIAgent
        return typeof(IAIAgent).IsAssignableFrom(agentType);
    }

    /// <inheritdoc />
    public AgentMetadata ExtractAgentMetadata(Type agentType)
    {
        ArgumentNullException.ThrowIfNull(agentType);

        var id = agentType.FullName ?? agentType.Name;
        var name = agentType.Name;
        string? description = null;
        string? version = null;
        var capabilities = new List<Type>();
        var tags = new List<string>();
        var priority = 0;
        var resourceRequirements = new AgentResourceRequirements();

        // Try to get metadata from attributes
        var metadataAttribute = agentType.GetCustomAttribute<AIAgentMetadataAttribute>();
        if (metadataAttribute != null)
        {
            id = metadataAttribute.Id ?? id;
            name = metadataAttribute.Name ?? name;
            description = metadataAttribute.Description;
            version = metadataAttribute.Version;
            priority = metadataAttribute.Priority;
            if (metadataAttribute.Tags != null)
            {
                tags.AddRange(metadataAttribute.Tags);
            }
        }

        // Extract capabilities from implemented interfaces
        var interfaces = agentType.GetInterfaces();
        foreach (var iface in interfaces)
        {
            if (iface != typeof(IAIAgent) && typeof(IAIAgent).IsAssignableFrom(iface))
            {
                capabilities.Add(iface);
            }
        }

        // Try to get resource requirements from attribute
        var resourceAttribute = agentType.GetCustomAttribute<AIAgentResourceRequirementsAttribute>();
        if (resourceAttribute != null)
        {
            resourceRequirements = new AgentResourceRequirements
            {
                MinMemoryBytes = resourceAttribute.MinMemoryBytes,
                MaxMemoryBytes = resourceAttribute.MaxMemoryBytes,
                RequiredCpuCores = resourceAttribute.RequiredCpuCores,
                RequiresGpu = resourceAttribute.RequiresGpu,
                NetworkAccess = resourceAttribute.NetworkAccess,
                InitializationTimeoutMs = resourceAttribute.InitializationTimeoutMs
            };
        }

        return new AgentMetadata
        {
            Id = id,
            Name = name,
            Description = description,
            Version = version,
            AgentType = agentType,
            Capabilities = capabilities.AsReadOnly(),
            Tags = tags.AsReadOnly(),
            Priority = priority,
            ResourceRequirements = resourceRequirements
        };
    }
}