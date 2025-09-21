using GameConsole.AI.Core;
using Xunit;

namespace GameConsole.AI.Core.Tests;

public class AIModelsTests
{
    [Fact]
    public void AIModel_ShouldCreateWithRequiredProperties()
    {
        // Arrange
        var id = "gpt-4";
        var name = "GPT-4";
        var provider = "OpenAI";
        var capabilities = AIModelCapabilities.TextCompletion | AIModelCapabilities.Chat;

        // Act
        var model = new AIModel(id, name, provider, capabilities);

        // Assert
        Assert.Equal(id, model.Id);
        Assert.Equal(name, model.Name);
        Assert.Equal(provider, model.Provider);
        Assert.Equal(capabilities, model.Capabilities);
    }

    [Fact]
    public void AIRequest_ShouldCreateWithRequiredProperties()
    {
        // Arrange
        var modelId = "gpt-4";
        var prompt = "Hello, world!";

        // Act
        var request = new AIRequest
        {
            ModelId = modelId,
            Prompt = prompt
        };

        // Assert
        Assert.Equal(modelId, request.ModelId);
        Assert.Equal(prompt, request.Prompt);
        Assert.False(request.Stream);
    }

    [Fact]
    public void AIResponse_ShouldCreateWithRequiredProperties()
    {
        // Arrange
        var content = "Hello, human!";
        var modelId = "gpt-4";

        // Act
        var response = new AIResponse
        {
            Content = content,
            ModelId = modelId
        };

        // Assert
        Assert.Equal(content, response.Content);
        Assert.Equal(modelId, response.ModelId);
        Assert.True(response.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AIHealthStatus_ShouldCreateWithHealthyStatus()
    {
        // Arrange & Act
        var healthStatus = new AIHealthStatus
        {
            IsHealthy = true,
            Status = "All systems operational"
        };

        // Assert
        Assert.True(healthStatus.IsHealthy);
        Assert.Equal("All systems operational", healthStatus.Status);
        Assert.True(healthStatus.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(AIModelCapabilities.TextCompletion, true)]
    [InlineData(AIModelCapabilities.Chat, false)]
    [InlineData(AIModelCapabilities.TextCompletion | AIModelCapabilities.Chat, true)]
    public void AIModelCapabilities_ShouldSupportFlagsOperations(AIModelCapabilities capabilities, bool hasTextCompletion)
    {
        // Act
        var result = capabilities.HasFlag(AIModelCapabilities.TextCompletion);

        // Assert
        Assert.Equal(hasTextCompletion, result);
    }

    [Fact]
    public void AITokenUsage_ShouldCalculateTotalTokens()
    {
        // Arrange
        var promptTokens = 100;
        var completionTokens = 50;
        var expectedTotal = 150;

        // Act
        var tokenUsage = new AITokenUsage(promptTokens, completionTokens, expectedTotal);

        // Assert
        Assert.Equal(promptTokens, tokenUsage.PromptTokens);
        Assert.Equal(completionTokens, tokenUsage.CompletionTokens);
        Assert.Equal(expectedTotal, tokenUsage.TotalTokens);
    }

    [Fact]
    public void AIResponseChunk_ShouldCreateForStreaming()
    {
        // Arrange
        var content = "Hello";
        var index = 1;
        var isFinal = false;

        // Act
        var chunk = new AIResponseChunk
        {
            Content = content,
            Index = index,
            IsFinal = isFinal
        };

        // Assert
        Assert.Equal(content, chunk.Content);
        Assert.Equal(index, chunk.Index);
        Assert.Equal(isFinal, chunk.IsFinal);
    }
}