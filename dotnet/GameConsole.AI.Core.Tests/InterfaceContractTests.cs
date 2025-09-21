using GameConsole.AI.Core;
using GameConsole.AI.Core.Models;
using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using TestLib;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests to validate AI agent interface contracts and basic functionality.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    public void IAIAgent_InheritsFromRequiredInterfaces()
    {
        // Arrange & Act
        var agentType = typeof(IAIAgent);

        // Assert
        Assert.True(typeof(ICapabilityProvider).IsAssignableFrom(agentType));
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(agentType));
    }

    [Fact]
    public void IAIService_InheritsFromRequiredInterfaces()
    {
        // Arrange & Act
        var serviceType = typeof(GameConsole.AI.Services.IService);

        // Assert
        Assert.True(typeof(GameConsole.Core.Abstractions.IService).IsAssignableFrom(serviceType));
        Assert.True(typeof(ICapabilityProvider).IsAssignableFrom(serviceType));
    }

    [Fact]
    public void IAIContext_InheritsFromIAsyncDisposable()
    {
        // Arrange & Act
        var contextType = typeof(IAIContext);

        // Assert
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(contextType));
    }

    [Fact]
    public void AIAgentMetadata_HasRequiredProperties()
    {
        // Arrange
        var metadata = new AIAgentMetadata
        {
            Id = "test-agent-001",
            Name = "Test Agent",
            Version = new Version(1, 0, 0),
            Description = "A test AI agent",
            Author = "Test Author",
            ModelInfo = new AIModelInfo
            {
                FrameworkType = AIFrameworkType.Native
            }
        };

        // Act & Assert
        Assert.Equal("test-agent-001", metadata.Id);
        Assert.Equal("Test Agent", metadata.Name);
        Assert.Equal(new Version(1, 0, 0), metadata.Version);
        Assert.Equal("A test AI agent", metadata.Description);
        Assert.Equal("Test Author", metadata.Author);
        Assert.Equal(AIFrameworkType.Native, metadata.ModelInfo.FrameworkType);
    }

    [Fact]
    public void AIModelInfo_HasRequiredProperties()
    {
        // Arrange
        var modelInfo = new AIModelInfo
        {
            FrameworkType = AIFrameworkType.ONNX,
            FrameworkVersion = new Version(1, 2, 3),
            ModelName = "test-model",
            ModelVersion = new Version(2, 0, 0),
            ModelPath = "/path/to/model.onnx",
            ModelSize = 1024 * 1024, // 1MB
            License = "MIT",
            Source = "Test Source"
        };

        // Act & Assert
        Assert.Equal(AIFrameworkType.ONNX, modelInfo.FrameworkType);
        Assert.Equal(new Version(1, 2, 3), modelInfo.FrameworkVersion);
        Assert.Equal("test-model", modelInfo.ModelName);
        Assert.Equal(new Version(2, 0, 0), modelInfo.ModelVersion);
        Assert.Equal("/path/to/model.onnx", modelInfo.ModelPath);
        Assert.Equal(1024 * 1024, modelInfo.ModelSize);
        Assert.Equal("MIT", modelInfo.License);
        Assert.Equal("Test Source", modelInfo.Source);
    }

    [Fact]
    public void ResourceRequirement_HasCorrectStructure()
    {
        // Arrange
        var requirement = new ResourceRequirement
        {
            Type = ResourceType.Memory,
            MinimumAmount = 512,
            RecommendedAmount = 1024,
            MaximumAmount = 2048,
            Unit = "MB"
        };

        // Act & Assert
        Assert.Equal(ResourceType.Memory, requirement.Type);
        Assert.Equal(512, requirement.MinimumAmount);
        Assert.Equal(1024, requirement.RecommendedAmount);
        Assert.Equal(2048, requirement.MaximumAmount);
        Assert.Equal("MB", requirement.Unit);
    }

    [Fact]
    public void PerformanceMetric_HasCorrectStructure()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var metric = new PerformanceMetric
        {
            Name = "response_time",
            Value = 150.5,
            Unit = "ms",
            Timestamp = timestamp
        };

        // Act & Assert
        Assert.Equal("response_time", metric.Name);
        Assert.Equal(150.5, metric.Value);
        Assert.Equal("ms", metric.Unit);
        Assert.Equal(timestamp, metric.Timestamp);
    }

    [Fact]
    public void AgentStatus_EnumHasExpectedValues()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Uninitialized));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Initializing));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Ready));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Processing));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Error));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.ShuttingDown));
        Assert.True(Enum.IsDefined(typeof(AgentStatus), AgentStatus.Disposed));
    }

    [Fact]
    public void AIFrameworkType_EnumHasExpectedValues()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(AIFrameworkType), AIFrameworkType.ONNX));
        Assert.True(Enum.IsDefined(typeof(AIFrameworkType), AIFrameworkType.TensorFlow));
        Assert.True(Enum.IsDefined(typeof(AIFrameworkType), AIFrameworkType.PyTorch));
        Assert.True(Enum.IsDefined(typeof(AIFrameworkType), AIFrameworkType.Custom));
        Assert.True(Enum.IsDefined(typeof(AIFrameworkType), AIFrameworkType.Native));
    }

    [Fact]
    public void SecurityLevel_EnumHasExpectedValues()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(SecurityLevel), SecurityLevel.None));
        Assert.True(Enum.IsDefined(typeof(SecurityLevel), SecurityLevel.Basic));
        Assert.True(Enum.IsDefined(typeof(SecurityLevel), SecurityLevel.Sandboxed));
        Assert.True(Enum.IsDefined(typeof(SecurityLevel), SecurityLevel.Isolated));
    }

    [Fact]
    public void CapabilityValidationResult_SuccessCreatesValidResult()
    {
        // Act
        var result = CapabilityValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.MissingDependencies);
    }

    [Fact]
    public void CapabilityValidationResult_FailureCreatesInvalidResult()
    {
        // Arrange
        var issues = new[] { "Issue 1", "Issue 2" };
        var warnings = new[] { "Warning 1" };
        var dependencies = new[] { "Dependency 1" };

        // Act
        var result = CapabilityValidationResult.Failure(issues, dependencies, warnings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(issues, result.Issues);
        Assert.Equal(warnings, result.Warnings);
        Assert.Equal(dependencies, result.MissingDependencies);
    }

    [Fact]
    public void AgentExecutionOptions_DefaultValues()
    {
        // Act
        var options = new AgentExecutionOptions();

        // Assert
        Assert.Null(options.Timeout);
        Assert.Null(options.MaxResponseLength);
        Assert.Null(options.Temperature);
        Assert.NotNull(options.Parameters);
        Assert.Empty(options.Parameters);
        Assert.Equal(ExecutionPriority.Normal, options.Priority);
        Assert.False(options.EnableProfiling);
        Assert.Null(options.ConversationId);
    }

    [Theory]
    [InlineData(ExecutionPriority.Low)]
    [InlineData(ExecutionPriority.Normal)]
    [InlineData(ExecutionPriority.High)]
    [InlineData(ExecutionPriority.Critical)]
    public void ExecutionPriority_EnumHasExpectedValues(ExecutionPriority priority)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(ExecutionPriority), priority));
    }
}