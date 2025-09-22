using GameConsole.AI.Core;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests to verify that AI agent interfaces maintain their contracts correctly.
/// These tests ensure that interface definitions remain consistent and complete.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IAIAgent_Should_Extend_IPlugin()
    {
        // Arrange & Act
        var aiAgentType = typeof(IAIAgent);
        
        // Assert
        Assert.True(typeof(GameConsole.Plugins.Core.IPlugin).IsAssignableFrom(aiAgentType));
    }

    [Fact]
    public void IAIAgentMetadata_Should_Extend_IPluginMetadata()
    {
        // Arrange & Act
        var aiAgentMetadataType = typeof(IAIAgentMetadata);
        
        // Assert
        Assert.True(typeof(GameConsole.Plugins.Core.IPluginMetadata).IsAssignableFrom(aiAgentMetadataType));
    }

    [Fact]
    public void IAIAgent_Should_Have_Required_Methods()
    {
        // Arrange
        var aiAgentType = typeof(IAIAgent);

        // Act & Assert
        Assert.NotNull(aiAgentType.GetMethod("ProcessRequestAsync"));
        Assert.NotNull(aiAgentType.GetMethod("TrainAsync"));
        Assert.NotNull(aiAgentType.GetMethod("GetStatusAsync"));
        Assert.NotNull(aiAgentType.GetMethod("RunDiagnosticsAsync"));
    }

    [Fact]
    public void IAIAgentMetadata_Should_Have_Required_Properties()
    {
        // Arrange
        var aiAgentMetadataType = typeof(IAIAgentMetadata);

        // Act & Assert
        Assert.NotNull(aiAgentMetadataType.GetProperty("AgentType"));
        Assert.NotNull(aiAgentMetadataType.GetProperty("Capabilities"));
        Assert.NotNull(aiAgentMetadataType.GetProperty("ResourceRequirements"));
        Assert.NotNull(aiAgentMetadataType.GetProperty("SupportedProtocols"));
        Assert.NotNull(aiAgentMetadataType.GetProperty("SupportsLearning"));
    }

    [Fact]
    public void IAIAgentRegistry_Should_Have_Discovery_And_Registration_Methods()
    {
        // Arrange
        var registryType = typeof(IAIAgentRegistry);

        // Act & Assert
        Assert.NotNull(registryType.GetMethod("DiscoverAgentsAsync"));
        Assert.NotNull(registryType.GetMethod("DiscoverAllAgentsAsync"));
        Assert.NotNull(registryType.GetMethod("RegisterAgentAsync"));
        Assert.NotNull(registryType.GetMethod("UnregisterAgentAsync"));
        Assert.NotNull(registryType.GetMethod("GetRegisteredAgents"));
        Assert.NotNull(registryType.GetMethod("FindAgentsByCapabilities"));
        Assert.NotNull(registryType.GetMethod("FindAgentsByType"));
        Assert.NotNull(registryType.GetMethod("GetAgent"));
        Assert.NotNull(registryType.GetMethod("IsAgentRegistered"));
        Assert.NotNull(registryType.GetMethod("CreateAgentAsync"));
    }

    [Fact]
    public void AIAgentAttribute_Should_Extend_PluginAttribute()
    {
        // Arrange & Act
        var aiAgentAttributeType = typeof(AIAgentAttribute);
        
        // Assert
        Assert.True(typeof(GameConsole.Plugins.Core.PluginAttribute).IsAssignableFrom(aiAgentAttributeType));
    }

    [Fact]
    public void AIAgentAttribute_Should_Have_AI_Specific_Properties()
    {
        // Arrange
        var aiAgentAttributeType = typeof(AIAgentAttribute);

        // Act & Assert
        Assert.NotNull(aiAgentAttributeType.GetProperty("AgentType"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("Capabilities"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("SupportedProtocols"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("SupportsLearning"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("MinMemoryMB"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("RecommendedMemoryMB"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("RequiresGPU"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("RequiresNetwork"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("MaxConcurrentInstances"));
        Assert.NotNull(aiAgentAttributeType.GetProperty("AllowMultipleInstances"));
    }
}