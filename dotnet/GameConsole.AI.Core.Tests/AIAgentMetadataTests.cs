using GameConsole.AI.Models;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the AIAgentMetadata class.
/// </summary>
public class AIAgentMetadataTests
{
    [Fact]
    public void AIAgentMetadata_Should_Initialize_Properties_Correctly()
    {
        // Arrange
        var id = "test-agent";
        var name = "Test Agent";
        var version = new Version(1, 0, 0);
        var description = "A test AI agent";
        var author = "Test Author";
        var modelInfo = new AIModelInfo("TestModel", new Version(1, 0), AIFrameworkType.ONNX);

        // Act
        var metadata = new AIAgentMetadata(id, name, version, description, author, modelInfo);

        // Assert
        Assert.Equal(id, metadata.Id);
        Assert.Equal(name, metadata.Name);
        Assert.Equal(version, metadata.Version);
        Assert.Equal(description, metadata.Description);
        Assert.Equal(author, metadata.Author);
        Assert.Equal(modelInfo, metadata.ModelInfo);
        Assert.NotNull(metadata.Dependencies);
        Assert.NotNull(metadata.Properties);
        Assert.NotNull(metadata.ResourceRequirements);
        Assert.NotNull(metadata.SupportedFrameworks);
        Assert.NotNull(metadata.MinimumFrameworkVersions);
    }

    [Fact]
    public void AIAgentMetadata_Should_Throw_ArgumentNullException_For_Null_Parameters()
    {
        // Arrange
        var validVersion = new Version(1, 0, 0);
        var validModelInfo = new AIModelInfo("TestModel", new Version(1, 0), AIFrameworkType.ONNX);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIAgentMetadata(null!, "name", validVersion, "desc", "author", validModelInfo));
        Assert.Throws<ArgumentNullException>(() => new AIAgentMetadata("id", null!, validVersion, "desc", "author", validModelInfo));
        Assert.Throws<ArgumentNullException>(() => new AIAgentMetadata("id", "name", null!, "desc", "author", validModelInfo));
        Assert.Throws<ArgumentNullException>(() => new AIAgentMetadata("id", "name", validVersion, null!, "author", validModelInfo));
        Assert.Throws<ArgumentNullException>(() => new AIAgentMetadata("id", "name", validVersion, "desc", null!, validModelInfo));
        Assert.Throws<ArgumentNullException>(() => new AIAgentMetadata("id", "name", validVersion, "desc", "author", null!));
    }
}