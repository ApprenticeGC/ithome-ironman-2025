using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for validation types and their functionality.
/// </summary>
public class ValidationTypesTests
{
    [Fact]
    public void ProfileValidationResult_DefaultConstructor_ShouldCreateValidResult()
    {
        // Arrange & Act
        var result = new ProfileValidationResult();
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ProfileValidationResult_WithErrors_ShouldBeInvalid()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var warnings = new[] { "Warning 1" };
        
        // Act
        var result = new ProfileValidationResult(errors, warnings);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("Warning 1", result.Warnings);
    }

    [Fact]
    public void ProfileValidationResult_WithWarningsOnly_ShouldBeValid()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };
        
        // Act
        var result = new ProfileValidationResult(Array.Empty<string>(), warnings);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
    }

    [Fact]
    public void ProfileValidationResult_WithNullErrors_ShouldHandleGracefully()
    {
        // Arrange & Act
        var result = new ProfileValidationResult(null!, null);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ConfigurationOptionSchema_ValidConstruction_ShouldSetAllProperties()
    {
        // Arrange
        var name = "TestOption";
        var description = "Test configuration option";
        var valueType = typeof(string);
        var defaultValue = "default";
        var isRequired = true;
        var possibleValues = new object[] { "option1", "option2" };
        
        // Act
        var schema = new ConfigurationOptionSchema(
            name, description, valueType, defaultValue, isRequired, possibleValues);
        
        // Assert
        Assert.Equal(name, schema.Name);
        Assert.Equal(description, schema.Description);
        Assert.Equal(valueType, schema.ValueType);
        Assert.Equal(defaultValue, schema.DefaultValue);
        Assert.Equal(isRequired, schema.IsRequired);
        Assert.NotNull(schema.PossibleValues);
        Assert.Equal(2, schema.PossibleValues!.Count);
        Assert.Contains("option1", schema.PossibleValues);
        Assert.Contains("option2", schema.PossibleValues);
    }

    [Fact]
    public void ConfigurationOptionSchema_MinimalConstruction_ShouldSetRequiredProperties()
    {
        // Arrange
        var name = "TestOption";
        var description = "Test configuration option";
        var valueType = typeof(int);
        
        // Act
        var schema = new ConfigurationOptionSchema(name, description, valueType);
        
        // Assert
        Assert.Equal(name, schema.Name);
        Assert.Equal(description, schema.Description);
        Assert.Equal(valueType, schema.ValueType);
        Assert.Null(schema.DefaultValue);
        Assert.False(schema.IsRequired);
        Assert.Null(schema.PossibleValues);
    }

    [Theory]
    [InlineData(null, "description", typeof(string))]
    [InlineData("name", null, typeof(string))]
    [InlineData("name", "description", null)]
    public void ConfigurationOptionSchema_NullRequiredParameters_ShouldThrowArgumentNullException(
        string? name, string? description, Type? valueType)
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ConfigurationOptionSchema(name!, description!, valueType!));
    }
}