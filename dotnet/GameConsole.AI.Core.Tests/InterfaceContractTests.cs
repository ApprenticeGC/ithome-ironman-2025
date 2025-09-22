using GameConsole.AI.Core;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.AI.Core abstractions.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IAIAgent_Should_Inherit_From_IPlugin()
    {
        // Arrange & Act
        var aiAgentType = typeof(IAIAgent);

        // Assert
        Assert.True(typeof(GameConsole.Plugins.Core.IPlugin).IsAssignableFrom(aiAgentType));
    }

    [Fact]
    public void IAIAgent_Should_Have_Required_Properties_And_Methods()
    {
        // Arrange
        var aiAgentType = typeof(IAIAgent);

        // Assert - Properties
        var capabilitiesProperty = aiAgentType.GetProperty("Capabilities");
        Assert.NotNull(capabilitiesProperty);
        Assert.Equal(typeof(IAIAgentCapabilities), capabilitiesProperty.PropertyType);
        Assert.True(capabilitiesProperty.CanRead);

        var statusProperty = aiAgentType.GetProperty("Status");
        Assert.NotNull(statusProperty);
        Assert.Equal(typeof(AIAgentStatus), statusProperty.PropertyType);
        Assert.True(statusProperty.CanRead);

        // Assert - Methods
        var processRequestMethod = aiAgentType.GetMethod("ProcessRequestAsync");
        Assert.NotNull(processRequestMethod);
        Assert.Equal(typeof(Task<IAIAgentResponse>), processRequestMethod.ReturnType);

        var canHandleRequestMethod = aiAgentType.GetMethod("CanHandleRequestAsync");
        Assert.NotNull(canHandleRequestMethod);
        Assert.Equal(typeof(Task<bool>), canHandleRequestMethod.ReturnType);

        var getPriorityMethod = aiAgentType.GetMethod("GetPriorityAsync");
        Assert.NotNull(getPriorityMethod);
        Assert.Equal(typeof(Task<int>), getPriorityMethod.ReturnType);
    }

    [Fact]
    public void IAIAgentDiscovery_Should_Inherit_From_IService_And_ICapabilityProvider()
    {
        // Arrange & Act
        var discoveryType = typeof(IAIAgentDiscovery);

        // Assert
        Assert.True(typeof(GameConsole.Core.Abstractions.IService).IsAssignableFrom(discoveryType));
        Assert.True(typeof(GameConsole.Core.Abstractions.ICapabilityProvider).IsAssignableFrom(discoveryType));
    }

    [Fact]
    public void IAIAgentRegistry_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var registryType = typeof(IAIAgentRegistry);

        // Assert
        Assert.True(typeof(GameConsole.Core.Abstractions.IService).IsAssignableFrom(registryType));
    }

    [Fact]
    public void IAIAgentCapabilities_Should_Have_Required_Properties()
    {
        // Arrange
        var capabilitiesType = typeof(IAIAgentCapabilities);

        // Act & Assert
        var supportedRequestTypesProperty = capabilitiesType.GetProperty("SupportedRequestTypes");
        Assert.NotNull(supportedRequestTypesProperty);
        Assert.Equal(typeof(IEnumerable<Type>), supportedRequestTypesProperty.PropertyType);

        var skillDomainsProperty = capabilitiesType.GetProperty("SkillDomains");
        Assert.NotNull(skillDomainsProperty);
        Assert.Equal(typeof(IEnumerable<string>), skillDomainsProperty.PropertyType);

        var processingCapabilitiesProperty = capabilitiesType.GetProperty("ProcessingCapabilities");
        Assert.NotNull(processingCapabilitiesProperty);
        Assert.Equal(typeof(IEnumerable<string>), processingCapabilitiesProperty.PropertyType);

        var maxConcurrentRequestsProperty = capabilitiesType.GetProperty("MaxConcurrentRequests");
        Assert.NotNull(maxConcurrentRequestsProperty);
        Assert.Equal(typeof(int), maxConcurrentRequestsProperty.PropertyType);
    }

    [Fact]
    public void IAIAgentRequest_Should_Have_Required_Properties()
    {
        // Arrange
        var requestType = typeof(IAIAgentRequest);

        // Act & Assert
        var requestIdProperty = requestType.GetProperty("RequestId");
        Assert.NotNull(requestIdProperty);
        Assert.Equal(typeof(string), requestIdProperty.PropertyType);

        var timestampProperty = requestType.GetProperty("Timestamp");
        Assert.NotNull(timestampProperty);
        Assert.Equal(typeof(DateTimeOffset), timestampProperty.PropertyType);

        var priorityProperty = requestType.GetProperty("Priority");
        Assert.NotNull(priorityProperty);
        Assert.Equal(typeof(int), priorityProperty.PropertyType);

        var timeoutProperty = requestType.GetProperty("Timeout");
        Assert.NotNull(timeoutProperty);
        Assert.Equal(typeof(TimeSpan), timeoutProperty.PropertyType);
    }

    [Fact]
    public void IAIAgentResponse_Should_Have_Required_Properties()
    {
        // Arrange
        var responseType = typeof(IAIAgentResponse);

        // Act & Assert
        var requestIdProperty = responseType.GetProperty("RequestId");
        Assert.NotNull(requestIdProperty);
        Assert.Equal(typeof(string), requestIdProperty.PropertyType);

        var timestampProperty = responseType.GetProperty("Timestamp");
        Assert.NotNull(timestampProperty);
        Assert.Equal(typeof(DateTimeOffset), timestampProperty.PropertyType);

        var statusProperty = responseType.GetProperty("Status");
        Assert.NotNull(statusProperty);
        Assert.Equal(typeof(AIResponseStatus), statusProperty.PropertyType);

        var processingTimeProperty = responseType.GetProperty("ProcessingTime");
        Assert.NotNull(processingTimeProperty);
        Assert.Equal(typeof(TimeSpan), processingTimeProperty.PropertyType);
    }

    [Fact]
    public void AIAgentStatus_Enum_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var statusValues = Enum.GetValues<AIAgentStatus>();

        // Assert
        Assert.Contains(AIAgentStatus.Uninitialized, statusValues);
        Assert.Contains(AIAgentStatus.Initializing, statusValues);
        Assert.Contains(AIAgentStatus.Ready, statusValues);
        Assert.Contains(AIAgentStatus.Processing, statusValues);
        Assert.Contains(AIAgentStatus.Busy, statusValues);
        Assert.Contains(AIAgentStatus.Error, statusValues);
        Assert.Contains(AIAgentStatus.Stopping, statusValues);
        Assert.Contains(AIAgentStatus.Stopped, statusValues);
    }

    [Fact]
    public void AIResponseStatus_Enum_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var statusValues = Enum.GetValues<AIResponseStatus>();

        // Assert
        Assert.Contains(AIResponseStatus.Success, statusValues);
        Assert.Contains(AIResponseStatus.Warning, statusValues);
        Assert.Contains(AIResponseStatus.Error, statusValues);
        Assert.Contains(AIResponseStatus.Timeout, statusValues);
        Assert.Contains(AIResponseStatus.Cancelled, statusValues);
        Assert.Contains(AIResponseStatus.NotSupported, statusValues);
    }
}