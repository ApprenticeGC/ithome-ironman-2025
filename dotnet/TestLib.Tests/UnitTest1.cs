namespace TestLib.Tests;

/// <summary>
/// Tests for the enhanced CI validation system that prevents false success detection
/// </summary>
public class CIValidationTests
{
    private readonly CIValidationHelper _validationHelper;

    public CIValidationTests()
    {
        _validationHelper = new CIValidationHelper();
    }

    /// <summary>
    /// Test that CI validation system is active and working
    /// </summary>
    [Fact]
    public void CI_Validation_System_Should_Be_Active()
    {
        // Arrange & Act
        var status = _validationHelper.GetValidationStatus();
        
        // Assert
        Assert.Equal("CI_VALIDATION_ACTIVE", status);
    }

    /// <summary>
    /// Test that CI validation correctly identifies successful test execution
    /// </summary>
    [Fact]
    public void CI_Validation_Should_Detect_Successful_Tests()
    {
        // Arrange & Act
        var result = _validationHelper.ValidateTestExecution();
        
        // Assert
        Assert.True(result, "CI validation should confirm successful test execution");
    }

    /// <summary>
    /// Test that CI validation can properly detect when failures should occur
    /// This prevents false success detection
    /// </summary>
    [Fact]
    public void CI_Validation_Should_Detect_Failures_Correctly()
    {
        // Arrange & Act
        var successCase = _validationHelper.CanDetectFailures(shouldFail: false);
        var failureCase = _validationHelper.CanDetectFailures(shouldFail: true);
        
        // Assert
        Assert.True(successCase, "Should return true when no failure is expected");
        Assert.False(failureCase, "Should return false when failure is expected - prevents false success");
    }

    /// <summary>
    /// Test that ensures CI system processes test results properly
    /// This test validates the core requirement of preventing false success detection
    /// </summary>
    [Theory]
    [InlineData(true, false)]  // When shouldFail=true, expect false (no false success)
    [InlineData(false, true)]  // When shouldFail=false, expect true (normal success)
    public void CI_Validation_Should_Prevent_False_Success_Detection(bool shouldFail, bool expectedResult)
    {
        // Arrange & Act
        var actualResult = _validationHelper.CanDetectFailures(shouldFail);
        
        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    /// <summary>
    /// Integration test to verify CI validation system components work together
    /// </summary>
    [Fact]
    public void CI_Validation_Integration_Test()
    {
        // Arrange
        var helper = new CIValidationHelper();
        
        // Act
        var status = helper.GetValidationStatus();
        var validationResult = helper.ValidateTestExecution();
        var failureDetection = helper.CanDetectFailures(false);
        
        // Assert
        Assert.Equal("CI_VALIDATION_ACTIVE", status);
        Assert.True(validationResult);
        Assert.True(failureDetection);
    }
}