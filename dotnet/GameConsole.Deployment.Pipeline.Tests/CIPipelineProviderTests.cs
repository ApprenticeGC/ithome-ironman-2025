using Xunit;
using GameConsole.Deployment.Pipeline;
using GameConsole.Deployment.Pipeline.Providers;

namespace GameConsole.Deployment.Pipeline.Tests;

/// <summary>
/// Tests for the CIPipelineProvider implementation.
/// </summary>
public class CIPipelineProviderTests
{
    private readonly CIPipelineProvider _provider;

    public CIPipelineProviderTests()
    {
        _provider = new CIPipelineProvider();
    }

    [Fact]
    public void ProviderName_Should_Return_Correct_Name()
    {
        // Assert
        Assert.Equal("CIPipelineProvider", _provider.ProviderName);
    }

    [Fact]
    public void SupportedPlatforms_Should_Include_Major_CI_Platforms()
    {
        // Assert
        var platforms = _provider.SupportedPlatforms;
        Assert.Contains("GitHubActions", platforms);
        Assert.Contains("AzureDevOps", platforms);
        Assert.Contains("Jenkins", platforms);
        Assert.Equal(3, platforms.Count);
    }

    [Fact]
    public async Task InitializeAsync_Should_Complete_Successfully()
    {
        // Act & Assert
        await _provider.InitializeAsync();
        // No exception should be thrown
    }

    [Fact]
    public async Task StartAsync_Should_Set_Running_State()
    {
        // Act
        await _provider.StartAsync();

        // Assert
        Assert.True(_provider.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Unset_Running_State()
    {
        // Arrange
        await _provider.StartAsync();
        Assert.True(_provider.IsRunning);

        // Act
        await _provider.StopAsync();

        // Assert
        Assert.False(_provider.IsRunning);
    }

    [Theory]
    [InlineData("GitHubActions")]
    [InlineData("AzureDevOps")]
    [InlineData("Jenkins")]
    public async Task TriggerWorkflowAsync_Should_Return_Success_For_Supported_Platforms(string platformType)
    {
        // Arrange
        var workflowConfig = new WorkflowConfig
        {
            WorkflowId = "test-workflow",
            ProviderType = platformType,
            Repository = "test/repo",
            Reference = "main"
        };

        // Act
        var result = await _provider.TriggerWorkflowAsync(workflowConfig);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(WorkflowStatus.Completed, result.Status);
        Assert.Contains(platformType == "AzureDevOps" ? "Azure DevOps" : platformType.Replace("Actions", " Actions"), result.ExecutionLog);
    }

    [Fact]
    public async Task TriggerWorkflowAsync_Should_Return_Failure_For_Unsupported_Platform()
    {
        // Arrange
        var workflowConfig = new WorkflowConfig
        {
            WorkflowId = "test-workflow",
            ProviderType = "UnsupportedPlatform",
            Repository = "test/repo",
            Reference = "main"
        };

        // Act
        var result = await _provider.TriggerWorkflowAsync(workflowConfig);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.Contains("Unsupported platform: UnsupportedPlatform", result.ErrorMessage);
    }

    [Fact]
    public async Task TriggerWorkflowAsync_Should_Throw_ArgumentNullException_For_Null_Config()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _provider.TriggerWorkflowAsync(null!));
    }

    [Fact]
    public async Task ValidateWorkflowAsync_Should_Return_Invalid_For_Missing_Required_Fields()
    {
        // Arrange
        var workflowConfig = new WorkflowConfig
        {
            WorkflowId = "",
            ProviderType = "",
            Repository = "",
            Reference = ""
        };

        // Act
        var result = await _provider.ValidateWorkflowAsync(workflowConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Workflow ID is required.", result.Errors);
        Assert.Contains("Provider type is required.", result.Errors);
        Assert.Contains("Repository is required.", result.Errors);
        Assert.Contains("Reference (branch/tag) is required.", result.Errors);
    }

    [Fact]
    public async Task ValidateWorkflowAsync_Should_Return_Invalid_For_Unsupported_Provider()
    {
        // Arrange
        var workflowConfig = new WorkflowConfig
        {
            WorkflowId = "test-workflow",
            ProviderType = "UnsupportedProvider",
            Repository = "test/repo",
            Reference = "main"
        };

        // Act
        var result = await _provider.ValidateWorkflowAsync(workflowConfig);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Unsupported provider type: UnsupportedProvider", result.Errors);
    }

    [Fact]
    public async Task ValidateWorkflowAsync_Should_Return_Valid_For_Proper_Configuration()
    {
        // Arrange
        var workflowConfig = new WorkflowConfig
        {
            WorkflowId = "test-workflow",
            ProviderType = "GitHubActions",
            Repository = "test/repo",
            Reference = "main"
        };

        // Act
        var result = await _provider.ValidateWorkflowAsync(workflowConfig);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_Should_Return_Status()
    {
        // Act
        var status = await _provider.GetWorkflowStatusAsync("test-workflow-id");

        // Assert
        Assert.Equal(WorkflowStatus.Completed, status);
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_Should_Throw_ArgumentException_For_Empty_WorkflowId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _provider.GetWorkflowStatusAsync(""));
    }

    [Fact]
    public async Task CancelWorkflowAsync_Should_Complete_Without_Exception()
    {
        // Act & Assert - Should not throw
        await _provider.CancelWorkflowAsync("test-workflow-id");
    }

    [Fact]
    public async Task CancelWorkflowAsync_Should_Throw_ArgumentException_For_Empty_WorkflowId()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _provider.CancelWorkflowAsync(""));
    }

    [Fact]
    public async Task DisposeAsync_Should_Complete_Successfully()
    {
        // Act & Assert
        await _provider.DisposeAsync();
        // No exception should be thrown
    }
}