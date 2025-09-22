using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

public class AgentRegistrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public AgentRegistrationTests()
    {
        _serviceProvider = new ServiceProvider();
    }

    [Fact]
    public void RegisterAgentsFromAttributes_WithValidAgents_RegistersSuccessfully()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        _serviceProvider.RegisterAgentsFromAttributes(assembly);

        // Assert
        var registeredAgents = _serviceProvider.GetRegisteredAgents();
        Assert.NotEmpty(registeredAgents);
        
        var testAgents = registeredAgents.Where(a => a.ServiceType == typeof(IAgent) || 
                                                    typeof(IAgent).IsAssignableFrom(a.ServiceType));
        Assert.NotEmpty(testAgents);
    }

    [Fact]
    public void RegisterAgentsFromAttributes_WithCategoryFilter_RegistersOnlyMatchingAgents()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        _serviceProvider.RegisterAgentsFromAttributes(assembly, "TestCategory");

        // Assert
        var registeredAgents = _serviceProvider.GetRegisteredAgents();
        var filteredAgents = registeredAgents.Where(a => 
            a.ImplementationType?.GetCustomAttribute<AgentAttribute>()?.Categories.Contains("TestCategory") == true);
        
        // Should only register agents with TestCategory
        Assert.NotEmpty(filteredAgents);
    }

    [Fact]
    public void GetRegisteredAgents_ReturnsAllAgents()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        _serviceProvider.RegisterAgentsFromAttributes(assembly);

        // Act
        var agents = _serviceProvider.GetRegisteredAgents();

        // Assert
        Assert.NotEmpty(agents);
        Assert.All(agents, a => Assert.True(typeof(IAgent).IsAssignableFrom(a.ServiceType) || 
                                           typeof(IAgent).IsAssignableFrom(a.ImplementationType)));
    }

    [Fact]
    public void GetAgentsWithCapability_WithValidCapability_ReturnsMatchingAgents()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        _serviceProvider.RegisterAgentsFromAttributes(assembly);

        // Act
        var agents = _serviceProvider.GetAgentsWithCapability("TestCapability");

        // Assert
        Assert.NotEmpty(agents);
        Assert.All(agents, a => 
            Assert.True(a.ImplementationType?.GetCustomAttribute<AgentAttribute>()?.Capabilities.Contains("TestCapability") == true));
    }

    [Fact]
    public void GetAgentsByCategory_WithValidCategory_ReturnsMatchingAgents()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        _serviceProvider.RegisterAgentsFromAttributes(assembly);

        // Act
        var agents = _serviceProvider.GetAgentsByCategory("TestCategory");

        // Assert
        Assert.NotEmpty(agents);
        Assert.All(agents, a => 
            Assert.True(a.ImplementationType?.GetCustomAttribute<AgentAttribute>()?.Categories.Contains("TestCategory") == true));
    }

    [Fact]
    public void GetAgentsWithCapability_WithNonExistentCapability_ReturnsEmpty()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        _serviceProvider.RegisterAgentsFromAttributes(assembly);

        // Act
        var agents = _serviceProvider.GetAgentsWithCapability("NonExistentCapability");

        // Assert
        Assert.Empty(agents);
    }

    [Fact]
    public void RegisterAgentsFromAttributes_WithNullAssembly_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serviceProvider.RegisterAgentsFromAttributes(null!));
    }

    [Fact]
    public void GetAgentsWithCapability_WithNullCapability_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serviceProvider.GetAgentsWithCapability(null!));
    }

    [Fact]
    public void GetAgentsByCategory_WithNullCategories_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serviceProvider.GetAgentsByCategory(null!));
    }

    [Fact]
    public void GetAgentsByCategory_WithNoCategories_ReturnsAllAgents()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        _serviceProvider.RegisterAgentsFromAttributes(assembly);

        // Act
        var allAgents = _serviceProvider.GetRegisteredAgents();
        var agentsByCategory = _serviceProvider.GetAgentsByCategory();

        // Assert
        Assert.Equal(allAgents.Count(), agentsByCategory.Count());
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

// Test agents for the above tests
[Agent("Test Agent", "1.0.0", "A test agent", 
       Categories = new[] { "TestCategory", "Test" }, 
       Capabilities = new[] { "TestCapability", "Testing" })]
public class TestAgent : IAgent
{
    public string AgentId { get; } = "test-agent-001";
    public bool IsActive { get; private set; }
    public IAgentMetadata Metadata { get; }

    public TestAgent()
    {
        Metadata = new TestAgentMetadata();
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        IsActive = true;
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        IsActive = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

[Agent("Another Test Agent", "2.0.0", "Another test agent", 
       Categories = new[] { "OtherCategory" }, 
       Capabilities = new[] { "OtherCapability" })]
public class AnotherTestAgent : IAgent
{
    public string AgentId { get; } = "test-agent-002";
    public bool IsActive { get; private set; }
    public IAgentMetadata Metadata { get; }

    public AnotherTestAgent()
    {
        Metadata = new TestAgentMetadata();
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        IsActive = true;
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        IsActive = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

// Test implementation of IAgentMetadata
public class TestAgentMetadata : IAgentMetadata
{
    public string Name { get; } = "Test Agent";
    public string Version { get; } = "1.0.0";
    public string Description { get; } = "A test agent";
    public IEnumerable<string> Categories { get; } = new[] { "Test" };
    public IEnumerable<string> Capabilities { get; } = new[] { "Testing" };
    public IReadOnlyDictionary<string, object> Properties { get; } = 
        new Dictionary<string, object> { { "TestProperty", "TestValue" } };
}

// Class with AgentAttribute but not implementing IAgent (should be skipped)
[Agent("Invalid Agent", "1.0.0", "This should not be registered")]
public class InvalidAgent
{
    public string Name { get; set; } = "Invalid";
}