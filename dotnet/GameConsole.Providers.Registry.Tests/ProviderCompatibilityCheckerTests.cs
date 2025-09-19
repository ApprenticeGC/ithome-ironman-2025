using GameConsole.Core.Abstractions;
using GameConsole.Providers.Registry;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Providers.Registry.Tests;

/// <summary>
/// Tests for provider compatibility checking functionality.
/// </summary>
public class ProviderCompatibilityCheckerTests
{
    [Fact]
    public void CheckCompatibility_WithMatchingCriteria_ShouldReturnCompatible()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio", "video", "input" },
            SupportedPlatforms: Platform.Windows | Platform.Linux
        );

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "audio", "video" },
            TargetPlatform: Platform.Windows,
            MinimumPriority: 5,
            MinimumVersion: new Version(0, 9, 0)
        );

        // Act
        var result = ProviderCompatibilityChecker.CheckCompatibility(metadata, criteria);

        // Assert
        Assert.True(result.IsCompatible);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void CheckCompatibility_WithMissingCapabilities_ShouldReturnIncompatible()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio" },
            SupportedPlatforms: Platform.All
        );

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "audio", "video" }
        );

        // Act
        var result = ProviderCompatibilityChecker.CheckCompatibility(metadata, criteria);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Single(result.Issues);
        Assert.Equal(CompatibilityIssueType.MissingCapabilities, result.Issues[0].Type);
        Assert.Contains("video", result.Issues[0].Message);
    }

    [Fact]
    public void CheckCompatibility_WithIncompatiblePlatform_ShouldReturnIncompatible()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string> { "audio" },
            SupportedPlatforms: Platform.Windows
        );

        var criteria = new ProviderSelectionCriteria(
            TargetPlatform: Platform.Linux
        );

        // Act
        var result = ProviderCompatibilityChecker.CheckCompatibility(metadata, criteria);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Single(result.Issues);
        Assert.Equal(CompatibilityIssueType.PlatformIncompatible, result.Issues[0].Type);
    }

    [Fact]
    public void CheckVersionCompatibility_WithOldVersion_ShouldReturnIncompatible()
    {
        // Arrange
        var providerVersion = new Version(1, 0, 0);
        var minimumVersion = new Version(1, 1, 0);

        // Act
        var result = ProviderCompatibilityChecker.CheckVersionCompatibility(providerVersion, minimumVersion, null);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Single(result.Issues);
        Assert.Equal(CompatibilityIssueType.VersionTooOld, result.Issues[0].Type);
    }

    [Fact]
    public void CheckVersionCompatibility_WithNewVersion_ShouldReturnIncompatible()
    {
        // Arrange
        var providerVersion = new Version(2, 0, 0);
        var maximumVersion = new Version(1, 9, 0);

        // Act
        var result = ProviderCompatibilityChecker.CheckVersionCompatibility(providerVersion, null, maximumVersion);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Single(result.Issues);
        Assert.Equal(CompatibilityIssueType.VersionTooNew, result.Issues[0].Type);
    }

    [Fact]
    public void CheckDependencyCompatibility_WithMissingDependency_ShouldReturnIncompatible()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 10,
            Capabilities: new HashSet<string>(),
            SupportedPlatforms: Platform.All,
            Dependencies: new Dictionary<string, Version> { { "CoreLib", new Version(2, 0, 0) } }
        );

        var availableDependencies = new Dictionary<string, Version>();

        // Act
        var result = ProviderCompatibilityChecker.CheckDependencyCompatibility(metadata, availableDependencies);

        // Assert
        Assert.False(result.IsCompatible);
        Assert.Single(result.Issues);
        Assert.Equal(CompatibilityIssueType.MissingDependency, result.Issues[0].Type);
    }

    [Fact]
    public void CalculateCompatibilityScore_WithPerfectMatch_ShouldReturnHighScore()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            Name: "TestProvider",
            Version: new Version(1, 0, 0),
            Priority: 20,
            Capabilities: new HashSet<string> { "audio", "video", "input", "networking" },
            SupportedPlatforms: Platform.All
        );

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "audio", "video" },
            PreferredCapabilities: new HashSet<string> { "input", "networking" },
            TargetPlatform: Platform.Windows,
            MinimumPriority: 10
        );

        // Act
        var score = ProviderCompatibilityChecker.CalculateCompatibilityScore(metadata, criteria);

        // Assert
        Assert.True(score > 0.8, $"Expected high compatibility score, got {score}");
    }
}