namespace GameConsole.Providers.Registry;

/// <summary>
/// Provides functionality for checking provider compatibility based on version constraints and platform requirements.
/// </summary>
public static class ProviderCompatibilityChecker
{
    /// <summary>
    /// Checks if a provider is compatible with the specified selection criteria.
    /// </summary>
    /// <param name="metadata">The provider metadata to check.</param>
    /// <param name="criteria">The selection criteria to match against.</param>
    /// <returns>True if the provider is compatible, false otherwise.</returns>
    public static bool IsCompatible(ProviderMetadata metadata, ProviderSelectionCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(criteria);

        // Check version constraints
        if (!IsVersionCompatible(metadata.Version, criteria.MinimumVersion, criteria.MaximumVersion))
            return false;

        // Check platform compatibility
        if (criteria.TargetPlatform.HasValue && !IsPlatformCompatible(metadata.SupportedPlatforms, criteria.TargetPlatform.Value))
            return false;

        // Check priority
        if (criteria.MinimumPriority.HasValue && metadata.Priority < criteria.MinimumPriority.Value)
            return false;

        // Check required capabilities
        if (criteria.RequiredCapabilities != null && !HasRequiredCapabilities(metadata.Capabilities, criteria.RequiredCapabilities))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a version satisfies the specified version constraints.
    /// </summary>
    /// <param name="version">The version to check.</param>
    /// <param name="minimumVersion">The minimum version requirement, or null if no minimum.</param>
    /// <param name="maximumVersion">The maximum version requirement, or null if no maximum.</param>
    /// <returns>True if the version satisfies the constraints, false otherwise.</returns>
    public static bool IsVersionCompatible(Version version, Version? minimumVersion, Version? maximumVersion)
    {
        ArgumentNullException.ThrowIfNull(version);

        if (minimumVersion != null && version < minimumVersion)
            return false;

        if (maximumVersion != null && version > maximumVersion)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the provider's supported platforms include the target platform.
    /// </summary>
    /// <param name="supportedPlatforms">The platforms supported by the provider.</param>
    /// <param name="targetPlatform">The target platform.</param>
    /// <returns>True if the provider supports the target platform, false otherwise.</returns>
    public static bool IsPlatformCompatible(Platform supportedPlatforms, Platform targetPlatform)
    {
        // If no specific platform requirement, it's compatible
        if (targetPlatform == Platform.None)
            return true;

        // Check if the supported platforms include the target platform
        return (supportedPlatforms & targetPlatform) != Platform.None;
    }

    /// <summary>
    /// Checks if the provider has all the required capabilities.
    /// </summary>
    /// <param name="providerCapabilities">The capabilities provided by the provider.</param>
    /// <param name="requiredCapabilities">The required capabilities.</param>
    /// <returns>True if the provider has all required capabilities, false otherwise.</returns>
    public static bool HasRequiredCapabilities(IReadOnlySet<string> providerCapabilities, IReadOnlySet<string> requiredCapabilities)
    {
        ArgumentNullException.ThrowIfNull(providerCapabilities);
        ArgumentNullException.ThrowIfNull(requiredCapabilities);

        return requiredCapabilities.All(capability => providerCapabilities.Contains(capability));
    }

    /// <summary>
    /// Calculates a compatibility score for a provider based on selection criteria.
    /// Higher scores indicate better matches.
    /// </summary>
    /// <param name="metadata">The provider metadata.</param>
    /// <param name="criteria">The selection criteria.</param>
    /// <returns>A compatibility score, or null if the provider is not compatible.</returns>
    public static int? CalculateCompatibilityScore(ProviderMetadata metadata, ProviderSelectionCriteria criteria)
    {
        if (!IsCompatible(metadata, criteria))
            return null;

        int score = metadata.Priority * 100; // Base score from priority

        // Bonus points for preferred capabilities
        if (criteria.PreferredCapabilities != null)
        {
            var matchingPreferred = criteria.PreferredCapabilities.Count(capability => metadata.Capabilities.Contains(capability));
            score += matchingPreferred * 10;
        }

        // Bonus points for newer versions (within bounds)
        score += metadata.Version.Major * 5 + metadata.Version.Minor;

        return score;
    }
}