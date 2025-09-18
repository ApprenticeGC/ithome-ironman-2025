using GameConsole.Providers.Registry;

namespace GameConsole.Providers.Registry.Tests;

public class ProviderCompatibilityCheckerTests
{
    [Fact]
    public void IsCompatible_WithMatchingCriteria_ReturnsTrue()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            "TestProvider",
            10,
            new HashSet<string> { "Capability1", "Capability2" },
            Platform.Windows | Platform.Linux,
            new Version(2, 0, 0));

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "Capability1" },
            TargetPlatform: Platform.Windows,
            MinimumPriority: 5,
            MinimumVersion: new Version(1, 0, 0),
            MaximumVersion: new Version(3, 0, 0));

        // Act
        var result = ProviderCompatibilityChecker.IsCompatible(metadata, criteria);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCompatible_WithMissingRequiredCapability_ReturnsFalse()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            "TestProvider",
            10,
            new HashSet<string> { "Capability1" },
            Platform.All,
            new Version(1, 0, 0));

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "MissingCapability" });

        // Act
        var result = ProviderCompatibilityChecker.IsCompatible(metadata, criteria);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsVersionCompatible_WithVersionInRange_ReturnsTrue()
    {
        // Arrange
        var version = new Version(2, 0, 0);
        var minVersion = new Version(1, 0, 0);
        var maxVersion = new Version(3, 0, 0);

        // Act
        var result = ProviderCompatibilityChecker.IsVersionCompatible(version, minVersion, maxVersion);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsVersionCompatible_WithVersionBelowMinimum_ReturnsFalse()
    {
        // Arrange
        var version = new Version(1, 0, 0);
        var minVersion = new Version(2, 0, 0);

        // Act
        var result = ProviderCompatibilityChecker.IsVersionCompatible(version, minVersion, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPlatformCompatible_WithSupportedPlatform_ReturnsTrue()
    {
        // Arrange
        var supportedPlatforms = Platform.Windows | Platform.Linux;
        var targetPlatform = Platform.Windows;

        // Act
        var result = ProviderCompatibilityChecker.IsPlatformCompatible(supportedPlatforms, targetPlatform);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPlatformCompatible_WithUnsupportedPlatform_ReturnsFalse()
    {
        // Arrange
        var supportedPlatforms = Platform.Windows;
        var targetPlatform = Platform.MacOS;

        // Act
        var result = ProviderCompatibilityChecker.IsPlatformCompatible(supportedPlatforms, targetPlatform);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRequiredCapabilities_WithAllCapabilities_ReturnsTrue()
    {
        // Arrange
        var providerCapabilities = new HashSet<string> { "Cap1", "Cap2", "Cap3" };
        var requiredCapabilities = new HashSet<string> { "Cap1", "Cap2" };

        // Act
        var result = ProviderCompatibilityChecker.HasRequiredCapabilities(providerCapabilities, requiredCapabilities);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRequiredCapabilities_WithMissingCapabilities_ReturnsFalse()
    {
        // Arrange
        var providerCapabilities = new HashSet<string> { "Cap1" };
        var requiredCapabilities = new HashSet<string> { "Cap1", "Cap2" };

        // Act
        var result = ProviderCompatibilityChecker.HasRequiredCapabilities(providerCapabilities, requiredCapabilities);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CalculateCompatibilityScore_WithCompatibleProvider_ReturnsScore()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            "TestProvider",
            10,
            new HashSet<string> { "Capability1", "Capability2" },
            Platform.All,
            new Version(2, 1, 0));

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "Capability1" },
            PreferredCapabilities: new HashSet<string> { "Capability2" });

        // Act
        var score = ProviderCompatibilityChecker.CalculateCompatibilityScore(metadata, criteria);

        // Assert
        Assert.NotNull(score);
        Assert.True(score > 0);
    }

    [Fact]
    public void CalculateCompatibilityScore_WithIncompatibleProvider_ReturnsNull()
    {
        // Arrange
        var metadata = new ProviderMetadata(
            "TestProvider",
            10,
            new HashSet<string> { "Capability1" },
            Platform.All,
            new Version(1, 0, 0));

        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: new HashSet<string> { "MissingCapability" });

        // Act
        var score = ProviderCompatibilityChecker.CalculateCompatibilityScore(metadata, criteria);

        // Assert
        Assert.Null(score);
    }
}