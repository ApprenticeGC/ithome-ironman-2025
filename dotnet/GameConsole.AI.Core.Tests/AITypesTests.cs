using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.AI.Core;
using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for AI agent metadata and core types.
/// </summary>
public class AITypesTests
{
    [Fact]
    public void AgentMetadata_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = "test-agent-001";
        var name = "Test Agent";
        var version = "1.0.0";
        var description = "A test AI agent";

        // Act
        var metadata = new AgentMetadata(id, name, version, description);

        // Assert
        Assert.Equal(id, metadata.Id);
        Assert.Equal(name, metadata.Name);
        Assert.Equal(version, metadata.Version);
        Assert.Equal(description, metadata.Description);
        Assert.Empty(metadata.Capabilities);
        Assert.Empty(metadata.Tags);
        Assert.Equal(0, metadata.Priority);
        Assert.True(metadata.IsEnabled);
    }

    [Fact]
    public void AgentMetadata_Constructor_ThrowsOnNullParameters()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AgentMetadata(null!, "name", "1.0.0", "desc"));
        Assert.Throws<ArgumentNullException>(() => new AgentMetadata("id", null!, "1.0.0", "desc"));
        Assert.Throws<ArgumentNullException>(() => new AgentMetadata("id", "name", null!, "desc"));
        Assert.Throws<ArgumentNullException>(() => new AgentMetadata("id", "name", "1.0.0", null!));
    }

    [Fact]
    public void AgentCapability_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var name = "TestCapability";
        var type = typeof(ITestCapability);
        var description = "A test capability";

        // Act
        var capability = new AgentCapability(name, type, description);

        // Assert
        Assert.Equal(name, capability.Name);
        Assert.Equal(type, capability.Type);
        Assert.Equal(description, capability.Description);
        Assert.NotNull(capability.Parameters);
        Assert.Empty(capability.Parameters);
    }

    [Fact]
    public void AgentCapability_Constructor_ThrowsOnNullParameters()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AgentCapability(null!, typeof(ITestCapability), "desc"));
        Assert.Throws<ArgumentNullException>(() => new AgentCapability("name", null!, "desc"));
        Assert.Throws<ArgumentNullException>(() => new AgentCapability("name", typeof(ITestCapability), null!));
    }

    [Fact]
    public void AgentDiscoveryResult_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var agents = new List<AgentMetadata>
        {
            new("agent1", "Agent 1", "1.0.0", "Test agent 1"),
            new("agent2", "Agent 2", "1.0.0", "Test agent 2")
        };
        var totalCount = 10;
        var isPartial = true;

        // Act
        var result = new AgentDiscoveryResult(agents, totalCount, isPartial);

        // Assert
        Assert.Equal(agents, result.Agents);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(isPartial, result.IsPartialResult);
    }

    [Fact]
    public void AgentDiscoveryResult_Constructor_ThrowsOnNullAgents()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AgentDiscoveryResult(null!, 0, false));
    }

    [Fact]
    public void AgentDiscoveryCriteria_DefaultValues_AreCorrect()
    {
        // Act
        var criteria = new AgentDiscoveryCriteria();

        // Assert
        Assert.Null(criteria.CapabilityType);
        Assert.Null(criteria.Tags);
        Assert.Null(criteria.MinimumPriority);
        Assert.True(criteria.EnabledOnly);
        Assert.Null(criteria.MaxResults);
    }

    // Test capability interface for testing purposes
    private interface ITestCapability : ICapabilityProvider
    {
        Task<string> TestMethodAsync(CancellationToken cancellationToken = default);
    }
}

/// <summary>
/// Tests for AI service interfaces to ensure they follow correct patterns.
/// </summary>
public class AIServiceInterfaceTests
{
    [Fact]
    public void IService_InheritsFromCorrectBaseInterface()
    {
        // Arrange
        var serviceType = typeof(GameConsole.AI.Services.IService);
        var baseServiceType = typeof(GameConsole.Core.Abstractions.IService);

        // Act & Assert
        Assert.True(baseServiceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public void IAgentDiscoveryCapability_InheritsFromICapabilityProvider()
    {
        // Arrange
        var capabilityType = typeof(IAgentDiscoveryCapability);
        var baseCapabilityType = typeof(ICapabilityProvider);

        // Act & Assert
        Assert.True(baseCapabilityType.IsAssignableFrom(capabilityType));
    }

    [Fact]
    public void IAgentRegistrationCapability_InheritsFromICapabilityProvider()
    {
        // Arrange
        var capabilityType = typeof(IAgentRegistrationCapability);
        var baseCapabilityType = typeof(ICapabilityProvider);

        // Act & Assert
        Assert.True(baseCapabilityType.IsAssignableFrom(capabilityType));
    }

    [Fact]
    public void EventArgs_Classes_InheritFromEventArgs()
    {
        // Act & Assert
        Assert.True(typeof(EventArgs).IsAssignableFrom(typeof(AgentRegisteredEventArgs)));
        Assert.True(typeof(EventArgs).IsAssignableFrom(typeof(AgentUnregisteredEventArgs)));
        Assert.True(typeof(EventArgs).IsAssignableFrom(typeof(AgentStatusChangedEventArgs)));
    }
}

/// <summary>
/// Integration tests to verify the complete AI agent type system works together.
/// </summary>
public class AIIntegrationTests
{
    [Fact]
    public void AgentMetadata_CanBeConfiguredWithCapabilities()
    {
        // Arrange
        var agent = new AgentMetadata("test-agent", "Test Agent", "1.0.0", "Test description");
        var capabilities = new[]
        {
            new AgentCapability("TextGeneration", typeof(ITextGenerationCapability), "Generates text"),
            new AgentCapability("ImageProcessing", typeof(IImageProcessingCapability), "Processes images")
        };

        // Act
        agent.Capabilities = capabilities;
        agent.Tags = new[] { "nlp", "vision", "ai" };
        agent.Priority = 5;

        // Assert
        Assert.Equal(2, agent.Capabilities.Length);
        Assert.Equal(3, agent.Tags.Length);
        Assert.Equal(5, agent.Priority);
        Assert.Contains("nlp", agent.Tags);
        Assert.Contains(capabilities[0], agent.Capabilities);
        Assert.Contains(capabilities[1], agent.Capabilities);
    }

    [Fact]
    public void AgentDiscoveryCriteria_CanFilterByMultipleCriteria()
    {
        // Arrange & Act
        var criteria = new AgentDiscoveryCriteria
        {
            CapabilityType = typeof(ITextGenerationCapability),
            Tags = new[] { "nlp", "chat" },
            MinimumPriority = 3,
            EnabledOnly = true,
            MaxResults = 10
        };

        // Assert
        Assert.Equal(typeof(ITextGenerationCapability), criteria.CapabilityType);
        Assert.Equal(2, criteria.Tags!.Length);
        Assert.Equal(3, criteria.MinimumPriority);
        Assert.True(criteria.EnabledOnly);
        Assert.Equal(10, criteria.MaxResults);
    }

    // Sample capability interfaces for testing
    private interface ITextGenerationCapability : ICapabilityProvider
    {
        Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
    }

    private interface IImageProcessingCapability : ICapabilityProvider
    {
        Task<byte[]> ProcessImageAsync(byte[] imageData, CancellationToken cancellationToken = default);
    }
}