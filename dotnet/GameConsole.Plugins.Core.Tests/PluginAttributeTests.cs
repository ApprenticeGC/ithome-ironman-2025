using GameConsole.Plugins.Core;
using Xunit;

namespace GameConsole.Plugins.Core.Tests;

/// <summary>
/// Tests for the PluginAttribute class.
/// </summary>
public class PluginAttributeTests
{
    [Fact]
    public void PluginAttribute_Should_Initialize_Required_Properties()
    {
        // Arrange
        var id = "test.plugin";
        var name = "Test Plugin";
        var version = "1.0.0";
        var description = "A test plugin";
        var author = "Test Author";

        // Act
        var attribute = new PluginAttribute(id, name, version, description, author);

        // Assert
        Assert.Equal(id, attribute.Id);
        Assert.Equal(name, attribute.Name);
        Assert.Equal(version, attribute.Version);
        Assert.Equal(description, attribute.Description);
        Assert.Equal(author, attribute.Author);
        Assert.Empty(attribute.Dependencies);
        Assert.Null(attribute.MinimumHostVersion);
        Assert.True(attribute.CanUnload);
        Assert.Empty(attribute.Tags);
    }

    [Fact]
    public void PluginAttribute_Should_Allow_Setting_Optional_Properties()
    {
        // Arrange
        var dependencies = new[] { "dependency1", "dependency2" };
        var tags = new[] { "tag1", "tag2" };
        var minHostVersion = "2.0.0";

        // Act
        var attribute = new PluginAttribute("test", "Test", "1.0", "Description", "Author")
        {
            Dependencies = dependencies,
            MinimumHostVersion = minHostVersion,
            CanUnload = false,
            Tags = tags
        };

        // Assert
        Assert.Equal(dependencies, attribute.Dependencies);
        Assert.Equal(minHostVersion, attribute.MinimumHostVersion);
        Assert.False(attribute.CanUnload);
        Assert.Equal(tags, attribute.Tags);
    }

    [Theory]
    [InlineData(null, "name", "version", "description", "author")]
    [InlineData("id", null, "version", "description", "author")]
    [InlineData("id", "name", null, "description", "author")]
    [InlineData("id", "name", "version", null, "author")]
    [InlineData("id", "name", "version", "description", null)]
    public void PluginAttribute_Should_Throw_ArgumentNullException_For_Null_Required_Parameters(
        string? id, string? name, string? version, string? description, string? author)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new PluginAttribute(id!, name!, version!, description!, author!));
    }

    [Fact]
    public void PluginAttribute_Should_Be_Applicable_To_Classes_Only()
    {
        // Arrange
        var attributeType = typeof(PluginAttribute);

        // Act
        var usage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }
}