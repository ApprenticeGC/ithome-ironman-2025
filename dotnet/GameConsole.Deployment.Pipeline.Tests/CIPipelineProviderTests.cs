using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Deployment.Pipeline.Tests;

public class CIPipelineProviderTests
{
    private readonly ILogger<GitHubActionsPipelineProvider> _githubLogger;
    private readonly ILogger<AzureDevOpsPipelineProvider> _azureLogger;

    public CIPipelineProviderTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _githubLogger = loggerFactory.CreateLogger<GitHubActionsPipelineProvider>();
        _azureLogger = loggerFactory.CreateLogger<AzureDevOpsPipelineProvider>();
    }

    [Fact]
    public void GitHubActionsPipelineProvider_PlatformName_ShouldBeCorrect()
    {
        // Arrange
        var provider = new GitHubActionsPipelineProvider(_githubLogger, new HttpClient());

        // Assert
        Assert.Equal("GitHub Actions", provider.PlatformName);
    }

    [Fact]
    public void AzureDevOpsPipelineProvider_PlatformName_ShouldBeCorrect()
    {
        // Arrange
        var provider = new AzureDevOpsPipelineProvider(_azureLogger, new HttpClient());

        // Assert
        Assert.Equal("Azure DevOps", provider.PlatformName);
    }

    [Fact]
    public async Task GitHubActionsPipelineProvider_TriggerPipelineAsync_WithoutToken_ShouldReturnFailure()
    {
        // Arrange
        var provider = new GitHubActionsPipelineProvider(_githubLogger, new HttpClient());
        var config = new CIPipelineConfiguration
        {
            PipelineName = "test-workflow",
            Repository = "test/repo",
            Branch = "main"
        };

        // Act
        var result = await provider.TriggerPipelineAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("GitHub Actions provider is not configured", result.ErrorMessage);
    }

    [Fact]
    public async Task AzureDevOpsPipelineProvider_TriggerPipelineAsync_WithoutToken_ShouldReturnFailure()
    {
        // Arrange
        var provider = new AzureDevOpsPipelineProvider(_azureLogger, new HttpClient());
        var config = new CIPipelineConfiguration
        {
            PipelineName = "test-pipeline",
            Repository = "test-org",
            Branch = "main"
        };

        // Act
        var result = await provider.TriggerPipelineAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Azure DevOps provider is not configured", result.ErrorMessage);
    }

    [Fact]
    public async Task GitHubActionsPipelineProvider_GetPipelineStatusAsync_WithoutToken_ShouldReturnNull()
    {
        // Arrange
        var provider = new GitHubActionsPipelineProvider(_githubLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var status = await provider.GetPipelineStatusAsync(pipelineId);

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task AzureDevOpsPipelineProvider_GetPipelineStatusAsync_WithoutToken_ShouldReturnNull()
    {
        // Arrange
        var provider = new AzureDevOpsPipelineProvider(_azureLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var status = await provider.GetPipelineStatusAsync(pipelineId);

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task GitHubActionsPipelineProvider_CancelPipelineAsync_WithoutToken_ShouldReturnFalse()
    {
        // Arrange
        var provider = new GitHubActionsPipelineProvider(_githubLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var result = await provider.CancelPipelineAsync(pipelineId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AzureDevOpsPipelineProvider_CancelPipelineAsync_WithoutToken_ShouldReturnFalse()
    {
        // Arrange
        var provider = new AzureDevOpsPipelineProvider(_azureLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var result = await provider.CancelPipelineAsync(pipelineId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GitHubActionsPipelineProvider_GetPipelineLogsAsync_WithoutToken_ShouldReturnEmptyCollection()
    {
        // Arrange
        var provider = new GitHubActionsPipelineProvider(_githubLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var logs = await provider.GetPipelineLogsAsync(pipelineId);

        // Assert
        Assert.NotNull(logs);
        Assert.Empty(logs);
    }

    [Fact]
    public async Task AzureDevOpsPipelineProvider_GetPipelineLogsAsync_WithoutToken_ShouldReturnEmptyCollection()
    {
        // Arrange
        var provider = new AzureDevOpsPipelineProvider(_azureLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var logs = await provider.GetPipelineLogsAsync(pipelineId);

        // Assert
        Assert.NotNull(logs);
        Assert.Empty(logs);
    }

    [Fact]
    public async Task GitHubActionsPipelineProvider_GetPipelineArtifactsAsync_WithoutToken_ShouldReturnEmptyCollection()
    {
        // Arrange
        var provider = new GitHubActionsPipelineProvider(_githubLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var artifacts = await provider.GetPipelineArtifactsAsync(pipelineId);

        // Assert
        Assert.NotNull(artifacts);
        Assert.Empty(artifacts);
    }

    [Fact]
    public async Task AzureDevOpsPipelineProvider_GetPipelineArtifactsAsync_WithoutToken_ShouldReturnEmptyCollection()
    {
        // Arrange
        var provider = new AzureDevOpsPipelineProvider(_azureLogger, new HttpClient());
        var pipelineId = Guid.NewGuid().ToString();

        // Act
        var artifacts = await provider.GetPipelineArtifactsAsync(pipelineId);

        // Assert
        Assert.NotNull(artifacts);
        Assert.Empty(artifacts);
    }

    [Fact]
    public void CIPipelineConfiguration_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new CIPipelineConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.PipelineName);
        Assert.Equal(string.Empty, config.Repository);
        Assert.Equal("main", config.Branch);
        Assert.Equal(60, config.TimeoutMinutes);
        Assert.NotNull(config.EnvironmentVariables);
        Assert.NotNull(config.Parameters);
    }
}