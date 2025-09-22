using GameConsole.AI.Services;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for AI agent metadata and enumeration types.
/// </summary>
public class AgentMetadataTests
{
    [Fact]
    public void AgentMetadata_Construction_SetsPropertiesCorrectly()
    {
        // Arrange
        const string id = "test-agent";
        const string name = "Test Agent";
        const string description = "A test AI agent";
        const AgentCapabilities capabilities = AgentCapabilities.TextGeneration | AgentCapabilities.Dialogue;
        const string version = "1.2.3";

        // Act
        var metadata = new AgentMetadata(id, name, description, capabilities, version);

        // Assert
        Assert.Equal(id, metadata.Id);
        Assert.Equal(name, metadata.Name);
        Assert.Equal(description, metadata.Description);
        Assert.Equal(capabilities, metadata.Capabilities);
        Assert.Equal(version, metadata.Version);
        Assert.NotNull(metadata.Properties);
        Assert.Empty(metadata.Properties);
    }

    [Fact]
    public void AgentMetadata_DefaultVersion_IsCorrect()
    {
        // Arrange & Act
        var metadata = new AgentMetadata("test", "Test", "Test agent", AgentCapabilities.TextGeneration);

        // Assert
        Assert.Equal("1.0.0", metadata.Version);
    }

    [Fact]
    public void AgentMetadata_WithProperties_SetsCustomProperties()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["model"] = "gpt-4",
            ["temperature"] = 0.7,
            ["maxTokens"] = 2048
        };

        // Act
        var metadata = new AgentMetadata("test", "Test", "Test agent", AgentCapabilities.TextGeneration)
        {
            Properties = properties
        };

        // Assert
        Assert.Equal(3, metadata.Properties.Count);
        Assert.Equal("gpt-4", metadata.Properties["model"]);
        Assert.Equal(0.7, metadata.Properties["temperature"]);
        Assert.Equal(2048, metadata.Properties["maxTokens"]);
    }

    [Theory]
    [InlineData(AgentCapabilities.TextGeneration)]
    [InlineData(AgentCapabilities.CodeGeneration)]
    [InlineData(AgentCapabilities.Dialogue)]
    [InlineData(AgentCapabilities.CreativeWriting)]
    [InlineData(AgentCapabilities.GameContentGeneration)]
    [InlineData(AgentCapabilities.AssetAnalysis)]
    [InlineData(AgentCapabilities.All)]
    public void AgentCapabilities_SingleValues_AreValid(AgentCapabilities capability)
    {
        // Arrange & Act
        var metadata = new AgentMetadata("test", "Test", "Test agent", capability);

        // Assert
        Assert.Equal(capability, metadata.Capabilities);
    }

    [Fact]
    public void AgentCapabilities_CombinedFlags_WorkCorrectly()
    {
        // Arrange
        const AgentCapabilities combined = AgentCapabilities.TextGeneration | 
                                         AgentCapabilities.CodeGeneration | 
                                         AgentCapabilities.Dialogue;

        // Act
        var metadata = new AgentMetadata("test", "Test", "Test agent", combined);

        // Assert
        Assert.Equal(combined, metadata.Capabilities);
        Assert.True(metadata.Capabilities.HasFlag(AgentCapabilities.TextGeneration));
        Assert.True(metadata.Capabilities.HasFlag(AgentCapabilities.CodeGeneration));
        Assert.True(metadata.Capabilities.HasFlag(AgentCapabilities.Dialogue));
        Assert.False(metadata.Capabilities.HasFlag(AgentCapabilities.CreativeWriting));
    }

    [Fact]
    public void AgentCapabilities_All_ContainsAllFlags()
    {
        // Arrange
        const AgentCapabilities all = AgentCapabilities.All;

        // Assert
        Assert.True(all.HasFlag(AgentCapabilities.TextGeneration));
        Assert.True(all.HasFlag(AgentCapabilities.CodeGeneration));
        Assert.True(all.HasFlag(AgentCapabilities.Dialogue));
        Assert.True(all.HasFlag(AgentCapabilities.CreativeWriting));
        Assert.True(all.HasFlag(AgentCapabilities.GameContentGeneration));
        Assert.True(all.HasFlag(AgentCapabilities.AssetAnalysis));
    }
}