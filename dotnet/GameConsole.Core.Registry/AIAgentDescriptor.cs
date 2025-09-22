using GameConsole.Core.Abstractions;
using System.Reflection;

namespace GameConsole.Core.Registry;

/// <summary>
/// Describes an AI agent with its type, implementation, metadata, and dependency information.
/// Used by the discovery and registration system to manage AI agent lifecycle.
/// </summary>
public sealed class AIAgentDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDescriptor"/> class.
    /// </summary>
    /// <param name="agentType">The AI agent interface or base type.</param>
    /// <param name="implementationType">The concrete implementation type.</param>
    /// <param name="metadata">The AI agent metadata.</param>
    /// <param name="lifetime">The service lifetime for the agent.</param>
    public AIAgentDescriptor(Type agentType, Type implementationType, IAIAgentMetadata metadata, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Lifetime = lifetime;

        if (!agentType.IsAssignableFrom(implementationType))
        {
            throw new ArgumentException($"Implementation type {implementationType.Name} does not implement agent type {agentType.Name}");
        }

        if (!typeof(IAIAgent).IsAssignableFrom(implementationType))
        {
            throw new ArgumentException($"Implementation type {implementationType.Name} must implement IAIAgent interface");
        }
    }

    /// <summary>
    /// Gets the AI agent interface or base type.
    /// </summary>
    public Type AgentType { get; }

    /// <summary>
    /// Gets the concrete implementation type.
    /// </summary>
    public Type ImplementationType { get; }

    /// <summary>
    /// Gets the AI agent metadata.
    /// </summary>
    public IAIAgentMetadata Metadata { get; }

    /// <summary>
    /// Gets the service lifetime for the agent.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets a value indicating whether this agent can be created (all dependencies are satisfied).
    /// </summary>
    public bool CanCreate { get; internal set; } = true;

    /// <summary>
    /// Gets the list of unsatisfied dependencies preventing agent creation.
    /// </summary>
    public IReadOnlyList<string> UnsatisfiedDependencies { get; internal set; } = Array.Empty<string>();

    /// <summary>
    /// Creates a factory delegate for instantiating the AI agent.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <returns>A factory delegate that creates AI agent instances.</returns>
    public Func<IServiceProvider, IAIAgent> CreateFactory(IServiceProvider serviceProvider)
    {
        return provider => 
        {
            var instance = CreateInstance(provider);
            return instance;
        };
    }

    /// <summary>
    /// Creates a service descriptor for registering the AI agent in the DI container.
    /// </summary>
    /// <returns>A service descriptor for the AI agent.</returns>
    public ServiceDescriptor ToServiceDescriptor()
    {
        return new ServiceDescriptor(AgentType, ImplementationType, Lifetime);
    }

    /// <summary>
    /// Creates an AI agent instance using reflection and dependency injection.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <returns>The created AI agent instance.</returns>
    private IAIAgent CreateInstance(IServiceProvider serviceProvider)
    {
        var constructors = ImplementationType.GetConstructors();
        var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0) 
                         ?? constructors.OrderBy(c => c.GetParameters().Length).First();

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var service = serviceProvider.GetService(parameters[i].ParameterType);
            if (service == null)
            {
                throw new InvalidOperationException($"Unable to resolve service for type {parameters[i].ParameterType} required by AI agent {Metadata.Name}");
            }
            args[i] = service;
        }

        var instance = Activator.CreateInstance(ImplementationType, args);
        if (instance is not IAIAgent agent)
        {
            throw new InvalidOperationException($"Created instance of {ImplementationType} is not an IAIAgent");
        }

        return agent;
    }

    /// <summary>
    /// Creates an AI agent descriptor from a type decorated with <see cref="AIAgentAttribute"/>.
    /// </summary>
    /// <param name="implementationType">The implementation type with AIAgentAttribute.</param>
    /// <returns>An AI agent descriptor, or null if the type is not properly decorated.</returns>
    public static AIAgentDescriptor? FromAttributedType(Type implementationType)
    {
        var attribute = implementationType.GetCustomAttribute(typeof(AIAgentAttribute), false) as AIAgentAttribute;
        if (attribute == null)
            return null;

        if (!typeof(IAIAgent).IsAssignableFrom(implementationType))
            return null;

        var agentType = implementationType.GetInterfaces()
            .FirstOrDefault(i => i != typeof(IAIAgent) && typeof(IAIAgent).IsAssignableFrom(i))
            ?? typeof(IAIAgent);

        var metadata = new AIAgentMetadataFromAttribute(attribute, implementationType);
        return new AIAgentDescriptor(agentType, implementationType, metadata);
    }

    public override string ToString()
    {
        return $"AIAgent: {Metadata.Name} ({Metadata.Id}) v{Metadata.Version} - {ImplementationType.Name}";
    }
}

/// <summary>
/// Implementation of <see cref="IAIAgentMetadata"/> that extracts metadata from <see cref="AIAgentAttribute"/>.
/// </summary>
internal class AIAgentMetadataFromAttribute : IAIAgentMetadata
{
    public AIAgentMetadataFromAttribute(AIAgentAttribute attribute, Type implementationType)
    {
        Id = attribute.Id;
        Name = attribute.Name;
        Version = new Version(attribute.Version);
        Description = attribute.Description;
        Author = attribute.Author;
        Dependencies = attribute.Dependencies.ToList().AsReadOnly();

        // Resolve capability types from string names
        var providedCapabilities = new List<Type>();
        foreach (var capabilityName in attribute.ProvidedCapabilities)
        {
            var type = Type.GetType(capabilityName) ?? implementationType.Assembly.GetType(capabilityName);
            if (type != null)
                providedCapabilities.Add(type);
        }
        ProvidedCapabilities = providedCapabilities.AsReadOnly();

        var requiredCapabilities = new List<Type>();
        foreach (var capabilityName in attribute.RequiredCapabilities)
        {
            var type = Type.GetType(capabilityName) ?? implementationType.Assembly.GetType(capabilityName);
            if (type != null)
                requiredCapabilities.Add(type);
        }
        RequiredCapabilities = requiredCapabilities.AsReadOnly();

        // Build properties from attribute
        var properties = new Dictionary<string, object>
        {
            ["Tags"] = attribute.Tags,
            ["ExecutionTimeoutMs"] = attribute.ExecutionTimeoutMs,
            ["MaxMemoryBytes"] = attribute.MaxMemoryBytes
        };
        Properties = properties.AsReadOnly();
    }

    public string Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public string Description { get; }
    public string Author { get; }
    public IReadOnlyList<string> Dependencies { get; }
    public IReadOnlyList<Type> ProvidedCapabilities { get; }
    public IReadOnlyList<Type> RequiredCapabilities { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }
}