using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UIProfileValidationResultTests
{
    [Fact]
    public void Success_CreatesValidResult()
    {
        // Arrange & Act
        var result = UIProfileValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Failure_CreatesInvalidResultWithErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = UIProfileValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void SuccessWithWarnings_CreatesValidResultWithWarnings()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };

        // Act
        var result = UIProfileValidationResult.SuccessWithWarnings(warnings);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains("Warning 1", result.Warnings);
        Assert.Contains("Warning 2", result.Warnings);
    }

    [Fact]
    public void Constructor_InitializesEmptyLists()
    {
        // Arrange & Act
        var result = new UIProfileValidationResult();

        // Assert
        Assert.False(result.IsValid); // Default value
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Warnings);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Errors_CanBeModified()
    {
        // Arrange
        var result = new UIProfileValidationResult();

        // Act
        result.Errors.Add("Custom error");

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains("Custom error", result.Errors);
    }

    [Fact]
    public void Warnings_CanBeModified()
    {
        // Arrange
        var result = new UIProfileValidationResult();

        // Act
        result.Warnings.Add("Custom warning");

        // Assert
        Assert.Single(result.Warnings);
        Assert.Contains("Custom warning", result.Warnings);
    }
}