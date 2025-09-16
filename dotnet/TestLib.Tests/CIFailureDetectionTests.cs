namespace TestLib.Tests;

/// <summary>
/// Advanced tests for CI validation system to prevent false success detection
/// These tests specifically validate edge cases and failure scenarios
/// </summary>
public class CIFailureDetectionTests
{
    /// <summary>
    /// Test that verifies CI doesn't incorrectly report success when build should fail
    /// This is critical for the enhanced CI validation system
    /// </summary>
    [Fact]
    public void CI_Should_Not_Report_False_Success_On_Build_Issues()
    {
        // Arrange
        var validator = new CIValidationHelper();
        
        // Act - Test scenario where failure detection is critical
        var result = validator.CanDetectFailures(shouldFail: true);
        
        // Assert - Should return false to prevent false success
        Assert.False(result, "CI validation must not report false success when failure is expected");
    }

    /// <summary>
    /// Test that validates CI properly handles multiple validation scenarios
    /// This ensures robustness of the enhanced validation system
    /// </summary>
    [Theory]
    [InlineData("valid_scenario", true)]
    [InlineData("failure_scenario", false)]
    public void CI_Validation_Should_Handle_Multiple_Scenarios(string scenario, bool expectedSuccess)
    {
        // Arrange
        var validator = new CIValidationHelper();
        bool shouldFail = scenario == "failure_scenario";
        
        // Act
        var result = validator.CanDetectFailures(shouldFail);
        
        // Assert
        Assert.Equal(expectedSuccess, result);
    }

    /// <summary>
    /// Test that ensures CI validation system maintains consistency
    /// Multiple calls should produce consistent results
    /// </summary>
    [Fact]
    public void CI_Validation_Should_Be_Consistent_Across_Multiple_Calls()
    {
        // Arrange
        var validator = new CIValidationHelper();
        
        // Act - Call validation multiple times
        var result1 = validator.ValidateTestExecution();
        var result2 = validator.ValidateTestExecution();
        var result3 = validator.ValidateTestExecution();
        
        // Assert - All calls should return consistent results
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    /// <summary>
    /// Test that validates CI system status reporting
    /// This ensures the validation system can be monitored
    /// </summary>
    [Fact]
    public void CI_Validation_Status_Should_Be_Reportable()
    {
        // Arrange
        var validator = new CIValidationHelper();
        
        // Act
        var status = validator.GetValidationStatus();
        
        // Assert
        Assert.NotNull(status);
        Assert.NotEmpty(status);
        Assert.Contains("CI_VALIDATION", status);
    }
}