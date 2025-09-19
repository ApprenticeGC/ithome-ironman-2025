using GameConsole.AI.Models;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for the AIModelInfo class.
/// </summary>
public class AIModelInfoTests
{
    [Fact]
    public void AIModelInfo_Should_Initialize_Properties_Correctly()
    {
        // Arrange
        var name = "TestModel";
        var version = new Version(2, 1, 0);
        var framework = AIFrameworkType.TensorFlow;

        // Act
        var modelInfo = new AIModelInfo(name, version, framework);

        // Assert
        Assert.Equal(name, modelInfo.Name);
        Assert.Equal(version, modelInfo.Version);
        Assert.Equal(framework, modelInfo.Framework);
        Assert.NotNull(modelInfo.Properties);
        Assert.NotNull(modelInfo.Metrics);
    }

    [Fact]
    public void AIModelInfo_Should_Throw_ArgumentNullException_For_Null_Parameters()
    {
        // Arrange
        var validVersion = new Version(1, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIModelInfo(null!, validVersion, AIFrameworkType.ONNX));
        Assert.Throws<ArgumentNullException>(() => new AIModelInfo("name", null!, AIFrameworkType.ONNX));
    }

    [Fact]
    public void AIModelInfo_Should_Allow_Setting_Optional_Properties()
    {
        // Arrange
        var modelInfo = new AIModelInfo("TestModel", new Version(1, 0), AIFrameworkType.PyTorch);
        var sizeInBytes = 1024L * 1024 * 100; // 100 MB
        var modelPath = "/path/to/model";
        var license = "MIT";

        // Act
        modelInfo.SizeInBytes = sizeInBytes;
        modelInfo.ModelPath = modelPath;
        modelInfo.License = license;
        modelInfo.Properties["custom"] = "value";
        modelInfo.Metrics["accuracy"] = 0.95;

        // Assert
        Assert.Equal(sizeInBytes, modelInfo.SizeInBytes);
        Assert.Equal(modelPath, modelInfo.ModelPath);
        Assert.Equal(license, modelInfo.License);
        Assert.Equal("value", modelInfo.Properties["custom"]);
        Assert.Equal(0.95, modelInfo.Metrics["accuracy"]);
    }
}