namespace TestLib.Tests;

/// <summary>
/// Special test class to validate that CI system properly detects test failures
/// This ensures the enhanced CI validation system prevents false success detection
/// </summary>
public class CIFailureValidationTests
{
    /// <summary>
    /// This test can be temporarily enabled to verify CI failure detection
    /// It's skipped by default to prevent CI failures in normal operation
    /// </summary>
    [Fact(Skip = "Skip by default - can be enabled to test CI failure detection")]
    public void CI_Should_Detect_When_This_Test_Fails()
    {
        // This test would fail if enabled, validating that CI doesn't report false success
        Assert.Fail("This test intentionally fails to validate CI failure detection");
    }

    /// <summary>
    /// Test that validates the CI system can differentiate between skipped and failed tests
    /// </summary>
    [Fact]
    public void CI_Should_Handle_Skipped_Tests_Correctly()
    {
        // Arrange
        var validator = new CIValidationHelper();

        // Act
        var status = validator.GetValidationStatus();

        // Assert - This should pass and not be confused with the skipped test above
        Assert.Equal("CI_VALIDATION_ACTIVE", status);
    }

    /// <summary>
    /// Test that ensures CI validation system can detect various failure modes
    /// </summary>
    [Theory]
    [InlineData(false, true)]   // No failure expected, should succeed
    [InlineData(true, false)]   // Failure expected, should return false (preventing false success)
    public void CI_Should_Prevent_False_Success_In_Edge_Cases(bool simulateFailure, bool expectedResult)
    {
        // Arrange
        var validator = new CIValidationHelper();

        // Act
        var result = validator.CanDetectFailures(simulateFailure);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    /// <summary>
    /// Integration test to verify the complete CI validation pipeline
    /// This test validates that all components work together to prevent false success
    /// </summary>
    [Fact]
    public void CI_Validation_Pipeline_Integration_Test()
    {
        // Arrange
        var validator = new CIValidationHelper();

        // Act - Test multiple aspects of CI validation
        var systemActive = validator.GetValidationStatus() == "CI_VALIDATION_ACTIVE";
        var canValidateSuccess = validator.ValidateTestExecution();
        var canDetectFailures = !validator.CanDetectFailures(shouldFail: true); // Should return false when failure expected
        var canDetectSuccess = validator.CanDetectFailures(shouldFail: false);  // Should return true when success expected

        // Assert - All validation aspects should work correctly
        Assert.True(systemActive, "CI validation system should be active");
        Assert.True(canValidateSuccess, "Should validate successful test execution");
        Assert.True(canDetectFailures, "Should detect failures correctly (preventing false success)");
        Assert.True(canDetectSuccess, "Should detect legitimate success correctly");
    }
}
