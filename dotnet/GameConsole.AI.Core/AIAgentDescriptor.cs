using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of AI agent descriptor.
/// </summary>
public class AIAgentDescriptor : IAIAgentDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDescriptor"/> class.
    /// </summary>
    /// <param name="metadata">The metadata for the AI agent.</param>
    /// <param name="agentType">The type information for creating instances of this AI agent.</param>
    /// <param name="assembly">The assembly that contains this AI agent.</param>
    /// <param name="allowMultipleInstances">Whether this agent can be instantiated multiple times.</param>
    public AIAgentDescriptor(
        IAIAgentMetadata metadata,
        Type agentType,
        Assembly assembly,
        bool allowMultipleInstances = true)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        AllowMultipleInstances = allowMultipleInstances;
        RegisteredAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public IAIAgentMetadata Metadata { get; }

    /// <inheritdoc />
    public Type AgentType { get; }

    /// <inheritdoc />
    public Assembly Assembly { get; }

    /// <inheritdoc />
    public bool AllowMultipleInstances { get; }

    /// <inheritdoc />
    public DateTimeOffset RegisteredAt { get; }

    /// <inheritdoc />
    public IAIAgent CreateInstance(IServiceProvider serviceProvider)
    {
        if (!IsValid())
        {
            throw new InvalidOperationException($"Cannot create instance of invalid agent descriptor: {Metadata.Id}");
        }

        try
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, AgentType);
            if (instance is IAIAgent agent)
            {
                return agent;
            }
            
            throw new InvalidOperationException($"Created instance is not an IAIAgent: {AgentType.FullName}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of AI agent {Metadata.Id}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public bool IsValid()
    {
        // Check that the agent type implements IAIAgent
        if (!typeof(IAIAgent).IsAssignableFrom(AgentType))
        {
            return false;
        }

        // Check that the agent type is not abstract and has a public constructor
        if (AgentType.IsAbstract || AgentType.IsInterface)
        {
            return false;
        }

        // Check that the metadata has required fields
        if (string.IsNullOrEmpty(Metadata.Id) || string.IsNullOrEmpty(Metadata.Name) || string.IsNullOrEmpty(Metadata.AgentType))
        {
            return false;
        }

        return true;
    }
}