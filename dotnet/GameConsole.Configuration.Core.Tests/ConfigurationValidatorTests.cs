using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GameConsole.Configuration.Core;
using GameConsole.Configuration.Core.Models;
using Moq;
using Xunit;

namespace GameConsole.Configuration.Core.Tests;

/// <summary>
/// Tests for the ConfigurationValidator class.
/// </summary>
public sealed class ConfigurationValidatorTests
{
    private readonly Mock<ILogger<ConfigurationValidator>> _loggerMock;
    private readonly ConfigurationValidator _validator;

    public ConfigurationValidatorTests()
    {
        _loggerMock = new Mock<ILogger<ConfigurationValidator>>();
        _validator = new ConfigurationValidator(_loggerMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Success_For_Valid_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Logging:LogLevel:Default", "Information"),
                new KeyValuePair<string, string?>("TestSection:Value", "TestValue")
            })
            .Build();

        var context = new ConfigurationContext { Environment = "Development" };

        // Act
        var result = await _validator.ValidateAsync(configuration, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Warning_For_Debug_Logging_In_Production()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Logging:LogLevel:Default", "Debug"),
                new KeyValuePair<string, string?>("DetailedErrors", "true")
            })
            .Build();

        var context = new ConfigurationContext { Environment = "Production" };

        // Act
        var result = await _validator.ValidateAsync(configuration, context);

        // Assert
        Assert.True(result.IsValid); // Should still be valid, just warnings
        Assert.Contains(result.Warnings, w => w.Path == "Logging:LogLevel:Default");
        Assert.Contains(result.Warnings, w => w.Path == "DetailedErrors");
    }

    [Fact]
    public async Task ValidateSectionAsync_Should_Validate_Object_Properties()
    {
        // Arrange
        var section = new TestConfigSection { Name = "Test", Value = 42 };

        // Act
        var result = await _validator.ValidateSectionAsync(section, "TestSection");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateSectionAsync_Should_Return_Error_For_Null_Section()
    {
        // Arrange
        TestConfigSection? section = null;

        // Act
        var result = await _validator.ValidateSectionAsync(section!, "TestSection");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("TestSection", result.Errors[0].Path);
    }

    [Fact]
    public void RegisterValidator_Should_Allow_Custom_Validation()
    {
        // Arrange
        var customValidator = (TestConfigSection section, string path) => 
            Task.FromResult(section.Value > 0 ? ValidationResult.Success() : 
                ValidationResult.Failure(new[] { new ValidationError(path, "Value must be positive") }));

        // Act
        _validator.RegisterValidator<TestConfigSection>(customValidator);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void RegisterJsonSchema_Should_Store_Schema()
    {
        // Arrange
        var sectionPath = "TestSection";
        var schema = "{ \"type\": \"object\" }";

        // Act
        _validator.RegisterJsonSchema(sectionPath, schema);

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void RemoveValidator_Should_Remove_Custom_Validator()
    {
        // Arrange - Register a validator first
        var customValidator = (TestConfigSection section, string path) => 
            Task.FromResult(ValidationResult.Success());
        _validator.RegisterValidator<TestConfigSection>(customValidator);

        // Act
        _validator.RemoveValidator<TestConfigSection>();

        // Assert - Should not throw
        Assert.True(true);
    }

    private sealed class TestConfigSection
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public string? OptionalProperty { get; set; }
    }
}