using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the ProfileValidator class, ensuring profile consistency and validation.
/// These tests validate the profile validation requirements for RFC-011-01.
/// </summary>
public class ProfileValidatorTests
{
    /// <summary>
    /// Test that a valid profile passes validation.
    /// Validates: Profile validation ensures compatibility and completeness
    /// </summary>
    [Fact]
    public async Task ValidateProfileAsync_Should_Pass_For_Valid_Profile()
    {
        // Arrange
        var validator = new ProfileValidator();
        var profile = new TestUIProfile("ValidProfile", ConsoleMode.Game);
        var context = new TestUIContext(ConsoleMode.Game);

        // Act
        var result = await validator.ValidateProfileAsync(profile, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Test that a profile with empty name fails validation.
    /// Validates: Profile validator catches invalid configurations
    /// </summary>
    [Fact]
    public async Task ValidateProfileAsync_Should_Fail_For_Profile_With_Empty_Name()
    {
        // Arrange
        var validator = new ProfileValidator();
        var profile = new TestUIProfile("", ConsoleMode.Game);
        var context = new TestUIContext(ConsoleMode.Game);

        // Act
        var result = await validator.ValidateProfileAsync(profile, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Profile name cannot be null or empty", result.Errors);
    }

    /// <summary>
    /// Test validation of profile collection for conflicts.
    /// Validates: Profile validator detects conflicts between profiles
    /// </summary>
    [Fact]
    public async Task ValidateProfileCollectionAsync_Should_Detect_Duplicate_Names()
    {
        // Arrange
        var validator = new ProfileValidator();
        var profile1 = new TestUIProfile("SameName", ConsoleMode.Game);
        var profile2 = new TestUIProfile("SameName", ConsoleMode.Editor);
        var profiles = new[] { profile1, profile2 };

        // Act
        var result = await validator.ValidateProfileCollectionAsync(profiles);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Duplicate profile name found: SameName", result.Errors);
    }

    /// <summary>
    /// Test that profile metadata validation works correctly.
    /// Validates: Comprehensive profile configuration validation
    /// </summary>
    [Fact]
    public async Task ValidateProfileAsync_Should_Generate_Warnings_For_Missing_Metadata()
    {
        // Arrange
        var validator = new ProfileValidator();
        var profile = new TestUIProfile("TestProfile", ConsoleMode.Game);
        
        // Create profile with minimal metadata
        profile.SetMetadata(new UIProfileMetadata
        {
            Version = "",
            DisplayName = "",
            Description = ""
        });
        
        var context = new TestUIContext(ConsoleMode.Game);

        // Act
        var result = await validator.ValidateProfileAsync(profile, context);

        // Assert
        Assert.True(result.IsValid); // Should still be valid, but with warnings
        Assert.Contains("Profile version is not specified", result.Warnings);
        Assert.Contains("Profile display name is not specified", result.Warnings);
    }
}