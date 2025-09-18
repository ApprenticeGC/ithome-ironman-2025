using GameConsole.AI.Core;
using GameConsole.AI.Core.Models;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests to verify the interface contracts and behavior for GameConsole.AI.Core.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IAIAgent_Should_Inherit_From_IService()
    {
        // Arrange & Act
        var agentType = typeof(IAIAgent);
        
        // Assert
        Assert.True(typeof(IService).IsAssignableFrom(agentType));
    }

    [Fact]
    public void IAIAgent_Should_Inherit_From_ICapabilityProvider()
    {
        // Arrange & Act
        var agentType = typeof(IAIAgent);
        
        // Assert
        Assert.True(typeof(ICapabilityProvider).IsAssignableFrom(agentType));
    }

    [Fact]
    public void IAIAgent_Should_Have_Required_Properties()
    {
        // Arrange
        var agentType = typeof(IAIAgent);
        
        // Act & Assert
        var metadataProperty = agentType.GetProperty("Metadata");
        Assert.NotNull(metadataProperty);
        Assert.Equal(typeof(AIAgentMetadata), metadataProperty.PropertyType);
        Assert.True(metadataProperty.CanRead);
        
        var isBusyProperty = agentType.GetProperty("IsBusy");
        Assert.NotNull(isBusyProperty);
        Assert.Equal(typeof(bool), isBusyProperty.PropertyType);
        Assert.True(isBusyProperty.CanRead);
        
        var activeContextCountProperty = agentType.GetProperty("ActiveContextCount");
        Assert.NotNull(activeContextCountProperty);
        Assert.Equal(typeof(int), activeContextCountProperty.PropertyType);
        Assert.True(activeContextCountProperty.CanRead);
    }

    [Fact]
    public void IAIAgent_Should_Have_Required_Methods()
    {
        // Arrange
        var agentType = typeof(IAIAgent);
        
        // Act & Assert
        var createContextMethod = agentType.GetMethod("CreateContextAsync");
        Assert.NotNull(createContextMethod);
        Assert.Equal(typeof(Task<IAIContext>), createContextMethod.ReturnType);
        
        var executeMethod = agentType.GetMethod("ExecuteAsync");
        Assert.NotNull(executeMethod);
        Assert.Equal(typeof(Task<string>), executeMethod.ReturnType);
        
        var streamMethod = agentType.GetMethod("StreamAsync");
        Assert.NotNull(streamMethod);
        Assert.Equal(typeof(IAsyncEnumerable<string>), streamMethod.ReturnType);
        
        var healthCheckMethod = agentType.GetMethod("HealthCheckAsync");
        Assert.NotNull(healthCheckMethod);
        Assert.Equal(typeof(Task<bool>), healthCheckMethod.ReturnType);
        
        var performanceMetricsMethod = agentType.GetMethod("GetPerformanceMetricsAsync");
        Assert.NotNull(performanceMetricsMethod);
        Assert.Equal(typeof(Task<IReadOnlyDictionary<string, object>>), performanceMetricsMethod.ReturnType);
    }

    [Fact]
    public void IAICapability_Should_Have_Required_Properties()
    {
        // Arrange
        var capabilityType = typeof(IAICapability);
        
        // Act & Assert
        var capabilityIdProperty = capabilityType.GetProperty("CapabilityId");
        Assert.NotNull(capabilityIdProperty);
        Assert.Equal(typeof(string), capabilityIdProperty.PropertyType);
        Assert.True(capabilityIdProperty.CanRead);
        
        var nameProperty = capabilityType.GetProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.Equal(typeof(string), nameProperty.PropertyType);
        Assert.True(nameProperty.CanRead);
        
        var descriptionProperty = capabilityType.GetProperty("Description");
        Assert.NotNull(descriptionProperty);
        Assert.Equal(typeof(string), descriptionProperty.PropertyType);
        Assert.True(descriptionProperty.CanRead);
        
        var versionProperty = capabilityType.GetProperty("Version");
        Assert.NotNull(versionProperty);
        Assert.Equal(typeof(string), versionProperty.PropertyType);
        Assert.True(versionProperty.CanRead);
    }

    [Fact]
    public void IAICapability_Should_Have_Required_Methods()
    {
        // Arrange
        var capabilityType = typeof(IAICapability);
        
        // Act & Assert
        var validateMethod = capabilityType.GetMethod("ValidateAsync");
        Assert.NotNull(validateMethod);
        Assert.Equal(typeof(Task<bool>), validateMethod.ReturnType);
        
        var getResourceRequirementsMethod = capabilityType.GetMethod("GetResourceRequirements");
        Assert.NotNull(getResourceRequirementsMethod);
        Assert.Equal(typeof(AIResourceRequirements), getResourceRequirementsMethod.ReturnType);
    }

    [Fact]
    public void IAIContext_Should_Inherit_From_IAsyncDisposable()
    {
        // Arrange & Act
        var contextType = typeof(IAIContext);
        
        // Assert
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(contextType));
    }

    [Fact]
    public void IAIContext_Should_Have_Required_Properties()
    {
        // Arrange
        var contextType = typeof(IAIContext);
        
        // Act & Assert
        var sessionIdProperty = contextType.GetProperty("SessionId");
        Assert.NotNull(sessionIdProperty);
        Assert.Equal(typeof(string), sessionIdProperty.PropertyType);
        Assert.True(sessionIdProperty.CanRead);
        
        var frameworkTypeProperty = contextType.GetProperty("FrameworkType");
        Assert.NotNull(frameworkTypeProperty);
        Assert.Equal(typeof(AIFrameworkType), frameworkTypeProperty.PropertyType);
        Assert.True(frameworkTypeProperty.CanRead);
        
        var allocatedResourcesProperty = contextType.GetProperty("AllocatedResources");
        Assert.NotNull(allocatedResourcesProperty);
        Assert.Equal(typeof(AIResourceRequirements), allocatedResourcesProperty.PropertyType);
        Assert.True(allocatedResourcesProperty.CanRead);
        
        var isActiveProperty = contextType.GetProperty("IsActive");
        Assert.NotNull(isActiveProperty);
        Assert.Equal(typeof(bool), isActiveProperty.PropertyType);
        Assert.True(isActiveProperty.CanRead);
    }

    [Fact]
    public void AIAgentMetadata_Should_Implement_IServiceMetadata()
    {
        // Arrange & Act
        var metadataType = typeof(AIAgentMetadata);
        
        // Assert
        Assert.True(typeof(IServiceMetadata).IsAssignableFrom(metadataType));
    }

    [Fact]
    public void AIAgentMetadata_Should_Have_AI_Specific_Properties()
    {
        // Arrange
        var metadataType = typeof(AIAgentMetadata);
        
        // Act & Assert
        var modelNameProperty = metadataType.GetProperty("ModelName");
        Assert.NotNull(modelNameProperty);
        Assert.Equal(typeof(string), modelNameProperty.PropertyType);
        Assert.True(modelNameProperty.CanRead);
        
        var frameworkTypeProperty = metadataType.GetProperty("FrameworkType");
        Assert.NotNull(frameworkTypeProperty);
        Assert.Equal(typeof(AIFrameworkType), frameworkTypeProperty.PropertyType);
        Assert.True(frameworkTypeProperty.CanRead);
        
        var resourceRequirementsProperty = metadataType.GetProperty("ResourceRequirements");
        Assert.NotNull(resourceRequirementsProperty);
        Assert.Equal(typeof(AIResourceRequirements), resourceRequirementsProperty.PropertyType);
        Assert.True(resourceRequirementsProperty.CanRead);
    }

    [Fact]
    public void AIExecutionResult_Should_Have_Required_Properties()
    {
        // Arrange
        var resultType = typeof(AIExecutionResult);
        
        // Act & Assert
        var outputProperty = resultType.GetProperty("Output");
        Assert.NotNull(outputProperty);
        Assert.Equal(typeof(string), outputProperty.PropertyType);
        Assert.True(outputProperty.CanRead);
        
        var isSuccessProperty = resultType.GetProperty("IsSuccess");
        Assert.NotNull(isSuccessProperty);
        Assert.Equal(typeof(bool), isSuccessProperty.PropertyType);
        Assert.True(isSuccessProperty.CanRead);
        
        var executionTimeProperty = resultType.GetProperty("ExecutionTimeMs");
        Assert.NotNull(executionTimeProperty);
        Assert.Equal(typeof(long), executionTimeProperty.PropertyType);
        Assert.True(executionTimeProperty.CanRead);
    }

    [Fact]
    public void AIPerformanceMetrics_Should_Have_Required_Properties()
    {
        // Arrange
        var metricsType = typeof(AIPerformanceMetrics);
        
        // Act & Assert
        var totalOperationsProperty = metricsType.GetProperty("TotalOperations");
        Assert.NotNull(totalOperationsProperty);
        Assert.Equal(typeof(long), totalOperationsProperty.PropertyType);
        Assert.True(totalOperationsProperty.CanRead);
        Assert.True(totalOperationsProperty.CanWrite);
        
        var successRateProperty = metricsType.GetProperty("SuccessRate");
        Assert.NotNull(successRateProperty);
        Assert.Equal(typeof(double), successRateProperty.PropertyType);
        Assert.True(successRateProperty.CanRead);
        
        var averageExecutionTimeProperty = metricsType.GetProperty("AverageExecutionTimeMs");
        Assert.NotNull(averageExecutionTimeProperty);
        Assert.Equal(typeof(double), averageExecutionTimeProperty.PropertyType);
        Assert.True(averageExecutionTimeProperty.CanRead);
        Assert.True(averageExecutionTimeProperty.CanWrite);
    }

    [Fact]
    public void AIFrameworkType_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var frameworkValues = Enum.GetValues<AIFrameworkType>();
        
        // Assert
        Assert.Contains(AIFrameworkType.Unknown, frameworkValues);
        Assert.Contains(AIFrameworkType.ONNX, frameworkValues);
        Assert.Contains(AIFrameworkType.TensorFlow, frameworkValues);
        Assert.Contains(AIFrameworkType.PyTorch, frameworkValues);
        Assert.Contains(AIFrameworkType.OpenAI, frameworkValues);
        Assert.Contains(AIFrameworkType.LocalLLM, frameworkValues);
        Assert.Contains(AIFrameworkType.Azure, frameworkValues);
        Assert.Contains(AIFrameworkType.Custom, frameworkValues);
    }

    [Fact]
    public void AIResourceRequirements_Should_Have_Default_Values()
    {
        // Arrange & Act
        var requirements = new AIResourceRequirements();
        
        // Assert
        Assert.Equal(1, requirements.MinCpuCores);
        Assert.Equal(512, requirements.MinMemoryMB);
        Assert.False(requirements.RequiresGpu);
        Assert.Equal(0, requirements.MinGpuMemoryMB);
        Assert.Equal(30000, requirements.MaxExecutionTimeMs);
        Assert.NotNull(requirements.Properties);
    }
}