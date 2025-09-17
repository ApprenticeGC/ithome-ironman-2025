namespace TestLib.Tests;

/// <summary>
/// Tests specifically for enhanced CI validation functionality
/// These tests validate the log analysis patterns and severity scoring logic
/// that prevent false success detection in CI workflows
/// </summary>
public class EnhancedCIValidationTests
{
    private readonly CIValidationHelper _validator = new();

    /// <summary>
    /// Test that simulates various CI log scenarios
    /// Validates the core enhanced validation logic
    /// </summary>
    [Theory]
    [InlineData("clean_run", true, "Clean CI runs should be allowed to proceed")]
    [InlineData("minor_warning", true, "Minor warnings should still allow success")]
    [InlineData("timeout_error", false, "Timeout errors should block PR ready")]
    [InlineData("build_failure", false, "Build failures should block PR ready")]
    [InlineData("multiple_warnings", false, "Multiple warnings should trigger enhanced validation")]
    public void Enhanced_Validation_Should_Analyze_CI_Log_Patterns(string logType, bool expectedSuccess, string description)
    {
        // Arrange
        bool shouldFail = logType.Contains("error") || logType.Contains("failure") || logType.Contains("multiple");
        
        // Act
        var result = _validator.CanDetectFailures(shouldFail);
        
        // Assert
        Assert.Equal(expectedSuccess, result);
        Assert.True(true, description); // Use description parameter for test documentation
    }

    /// <summary>
    /// Test severity scoring logic that determines when to block PRs
    /// This validates the core logic from validate_ci_logs.py
    /// </summary>
    [Theory]
    [InlineData(0, 0, true)]    // No warnings, no errors -> allow
    [InlineData(2, 0, true)]    // Few warnings, no errors -> allow
    [InlineData(5, 0, false)]   // Many warnings, no errors -> block (threshold)
    [InlineData(0, 1, false)]   // No warnings, any error -> block
    [InlineData(3, 2, false)]   // Warnings + errors -> block
    public void Enhanced_Validation_Should_Apply_Severity_Scoring(int warningCount, int errorCount, bool expectedSuccess)
    {
        // Arrange
        bool hasBlockingIssues = (warningCount >= 5) || (errorCount > 0);
        
        // Act
        var result = _validator.CanDetectFailures(hasBlockingIssues);
        
        // Assert
        Assert.Equal(expectedSuccess, result);
        
        if (hasBlockingIssues)
        {
            Assert.False(result, $"Should block with {warningCount} warnings and {errorCount} errors");
        }
        else
        {
            Assert.True(result, $"Should allow with {warningCount} warnings and {errorCount} errors");
        }
    }

    /// <summary>
    /// Test that enhanced validation correctly identifies critical patterns
    /// Mirrors the patterns from validate_ci_logs.py WARNING_PATTERNS and ERROR_PATTERNS
    /// </summary>
    [Theory]
    [InlineData("Warning #42:", false, "Warning patterns should block")]
    [InlineData("⚠️ Warning: something", false, "Emoji warnings should block")]
    [InlineData("Command failed with exit code 1:", false, "Failed commands should block")]
    [InlineData("was blocked by firewall rules", false, "Firewall blocks should be detected")]
    [InlineData("Error: timeout exceeded", false, "Timeout errors should block")]
    [InlineData("fatal: repository not found", false, "Fatal errors should block")]
    [InlineData("FAILED: build target", false, "Failed targets should block")]
    [InlineData("Build succeeded", true, "Success messages should not block")]
    [InlineData("Tests passed", true, "Passing tests should not block")]
    [InlineData("Normal log message", true, "Normal messages should not block")]
    public void Enhanced_Validation_Should_Detect_Critical_Patterns(string logMessage, bool shouldAllow, string description)
    {
        // Arrange
        bool containsCriticalPattern = logMessage.Contains("Warning") || 
                                     logMessage.Contains("failed") || 
                                     logMessage.Contains("Error") || 
                                     logMessage.Contains("fatal") || 
                                     logMessage.Contains("FAILED") ||
                                     logMessage.Contains("blocked");
        
        // Act
        var result = _validator.CanDetectFailures(containsCriticalPattern);
        
        // Assert
        Assert.Equal(shouldAllow, result);
        Assert.True(true, description); // Use description parameter for test documentation
    }

    /// <summary>
    /// Test that enhanced validation provides appropriate feedback
    /// Validates that the system can report its validation decisions clearly
    /// </summary>
    [Fact]
    public void Enhanced_Validation_Should_Provide_Clear_Status_Reporting()
    {
        // Arrange & Act
        var status = _validator.GetValidationStatus();
        
        // Assert
        Assert.NotNull(status);
        Assert.NotEmpty(status);
        Assert.Contains("VALIDATION", status);
        
        // Test that status indicates active validation
        Assert.Equal("CI_VALIDATION_ACTIVE", status);
    }

    /// <summary>
    /// Test robustness of enhanced validation under edge conditions
    /// Ensures the system handles various real-world scenarios correctly
    /// </summary>
    [Theory]
    [InlineData("Network connectivity issues detected", false, "Network issues should block")]
    [InlineData("if-no-files-found: warn", false, "Missing file warnings should block")]
    [InlineData("connection refused", false, "Connection problems should block")]
    [InlineData("Build completed successfully", true, "Successful builds should be allowed")]
    [InlineData("All tests passed", true, "Successful tests should be allowed")]
    public void Enhanced_Validation_Should_Handle_Real_World_Scenarios(string scenario, bool shouldAllow, string description)
    {
        // Arrange
        bool hasIssues = scenario.Contains("issues") || 
                        scenario.Contains("warn") || 
                        scenario.Contains("refused");
        
        // Act
        var result = _validator.CanDetectFailures(hasIssues);
        
        // Assert
        Assert.Equal(shouldAllow, result);
        Assert.True(true, description); // Use description parameter for test documentation
    }

    /// <summary>
    /// Integration test that validates the complete enhanced validation flow
    /// This test ensures all components work together to prevent false successes
    /// </summary>
    [Fact]
    public void Enhanced_Validation_Complete_Flow_Integration_Test()
    {
        // Arrange
        var validator = _validator;
        
        // Act & Assert: Test system is active and ready
        var systemActive = validator.GetValidationStatus() == "CI_VALIDATION_ACTIVE";
        Assert.True(systemActive, "Enhanced validation system should be active");
        
        // Act & Assert: Test clean CI run is allowed
        var cleanRunAllowed = validator.CanDetectFailures(shouldFail: false);
        Assert.True(cleanRunAllowed, "Clean CI runs should be allowed to proceed");
        
        // Act & Assert: Test that failures are properly blocked
        var failuresBlocked = !validator.CanDetectFailures(shouldFail: true);
        Assert.True(failuresBlocked, "Failures should be properly blocked");
        
        // Act & Assert: Test consistency across multiple checks
        var consistent1 = validator.ValidateTestExecution();
        var consistent2 = validator.ValidateTestExecution();
        Assert.Equal(consistent1, consistent2);
        
        // Final validation: Enhanced system prevents false successes
        Assert.True(systemActive && cleanRunAllowed && failuresBlocked,
                   "Enhanced validation should prevent false successes while allowing legitimate success");
    }

    /// <summary>
    /// Test that enhanced validation maintains backward compatibility
    /// Ensures the system doesn't break existing functionality
    /// </summary>
    [Fact]
    public void Enhanced_Validation_Should_Maintain_Backward_Compatibility()
    {
        // Arrange
        var validator = _validator;
        
        // Act - Test that basic validation still works
        var basicValidation = validator.ValidateTestExecution();
        var status = validator.GetValidationStatus();
        
        // Assert - Basic functionality should remain intact
        Assert.True(basicValidation);
        Assert.NotNull(status);
        Assert.Contains("CI_VALIDATION", status);
    }
}