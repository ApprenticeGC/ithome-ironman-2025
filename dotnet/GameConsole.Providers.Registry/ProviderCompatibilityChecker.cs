namespace GameConsole.Providers.Registry;

/// <summary>
/// Provides compatibility checking functionality for providers including version validation and platform compatibility.
/// </summary>
public static class ProviderCompatibilityChecker
{
    /// <summary>
    /// Checks if a provider is compatible with the specified selection criteria.
    /// </summary>
    /// <param name="metadata">The provider metadata to check.</param>
    /// <param name="criteria">The selection criteria to validate against.</param>
    /// <returns>A compatibility result indicating whether the provider is compatible and any issues found.</returns>
    public static ProviderCompatibilityResult CheckCompatibility(ProviderMetadata metadata, ProviderSelectionCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(criteria);

        var issues = new List<CompatibilityIssue>();

        // Check required capabilities
        if (criteria.RequiredCapabilities != null && criteria.RequiredCapabilities.Count > 0)
        {
            var missingCapabilities = criteria.RequiredCapabilities.Except(metadata.Capabilities).ToArray();
            if (missingCapabilities.Length > 0)
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueType.MissingCapabilities,
                    $"Provider is missing required capabilities: {string.Join(", ", missingCapabilities)}"
                ));
            }
        }

        // Check platform compatibility
        if (criteria.TargetPlatform.HasValue && criteria.TargetPlatform.Value != Platform.None)
        {
            if (!metadata.SupportedPlatforms.HasFlag(criteria.TargetPlatform.Value))
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueType.PlatformIncompatible,
                    $"Provider does not support target platform: {criteria.TargetPlatform.Value}"
                ));
            }
        }

        // Check minimum priority
        if (criteria.MinimumPriority.HasValue && metadata.Priority < criteria.MinimumPriority.Value)
        {
            issues.Add(new CompatibilityIssue(
                CompatibilityIssueType.PriorityTooLow,
                $"Provider priority ({metadata.Priority}) is below minimum required ({criteria.MinimumPriority.Value})"
            ));
        }

        // Check version compatibility
        var versionResult = CheckVersionCompatibility(metadata.Version, criteria.MinimumVersion, criteria.MaximumVersion);
        if (!versionResult.IsCompatible)
        {
            issues.AddRange(versionResult.Issues);
        }

        return new ProviderCompatibilityResult(issues.Count == 0, issues.AsReadOnly());
    }

    /// <summary>
    /// Checks if a provider version is compatible with the specified version constraints.
    /// </summary>
    /// <param name="providerVersion">The provider version to check.</param>
    /// <param name="minimumVersion">The minimum version requirement, if any.</param>
    /// <param name="maximumVersion">The maximum version requirement, if any.</param>
    /// <returns>A version compatibility result.</returns>
    public static VersionCompatibilityResult CheckVersionCompatibility(Version providerVersion, Version? minimumVersion, Version? maximumVersion)
    {
        ArgumentNullException.ThrowIfNull(providerVersion);

        var issues = new List<CompatibilityIssue>();

        // Check minimum version
        if (minimumVersion != null && providerVersion < minimumVersion)
        {
            issues.Add(new CompatibilityIssue(
                CompatibilityIssueType.VersionTooOld,
                $"Provider version ({providerVersion}) is below minimum required version ({minimumVersion})"
            ));
        }

        // Check maximum version
        if (maximumVersion != null && providerVersion > maximumVersion)
        {
            issues.Add(new CompatibilityIssue(
                CompatibilityIssueType.VersionTooNew,
                $"Provider version ({providerVersion}) exceeds maximum allowed version ({maximumVersion})"
            ));
        }

        return new VersionCompatibilityResult(issues.Count == 0, issues.AsReadOnly());
    }

    /// <summary>
    /// Checks if provider dependencies are satisfied by the available providers.
    /// </summary>
    /// <param name="metadata">The provider metadata containing dependencies.</param>
    /// <param name="availableDependencies">Dictionary of available dependencies and their versions.</param>
    /// <returns>A dependency compatibility result.</returns>
    public static DependencyCompatibilityResult CheckDependencyCompatibility(
        ProviderMetadata metadata, 
        IReadOnlyDictionary<string, Version> availableDependencies)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(availableDependencies);

        var issues = new List<CompatibilityIssue>();

        if (metadata.Dependencies != null)
        {
            foreach (var dependency in metadata.Dependencies)
            {
                if (!availableDependencies.TryGetValue(dependency.Key, out var availableVersion))
                {
                    issues.Add(new CompatibilityIssue(
                        CompatibilityIssueType.MissingDependency,
                        $"Required dependency '{dependency.Key}' is not available"
                    ));
                }
                else if (availableVersion < dependency.Value)
                {
                    issues.Add(new CompatibilityIssue(
                        CompatibilityIssueType.DependencyVersionTooOld,
                        $"Dependency '{dependency.Key}' version ({availableVersion}) is below required version ({dependency.Value})"
                    ));
                }
            }
        }

        return new DependencyCompatibilityResult(issues.Count == 0, issues.AsReadOnly());
    }

    /// <summary>
    /// Calculates a compatibility score for a provider based on how well it matches the selection criteria.
    /// </summary>
    /// <param name="metadata">The provider metadata to score.</param>
    /// <param name="criteria">The selection criteria to score against.</param>
    /// <returns>A score from 0.0 (incompatible) to 1.0 (perfect match).</returns>
    public static double CalculateCompatibilityScore(ProviderMetadata metadata, ProviderSelectionCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(criteria);

        var result = CheckCompatibility(metadata, criteria);
        
        // If there are hard compatibility issues, return 0
        if (!result.IsCompatible)
        {
            return 0.0;
        }

        double score = 0.0;
        int factors = 0;

        // Base score for being compatible
        score += 0.5;
        factors++;

        // Bonus for preferred capabilities
        if (criteria.PreferredCapabilities != null && criteria.PreferredCapabilities.Count > 0)
        {
            var matchedPreferred = criteria.PreferredCapabilities.Intersect(metadata.Capabilities).Count();
            var preferredScore = (double)matchedPreferred / criteria.PreferredCapabilities.Count;
            score += preferredScore * 0.3;
            factors++;
        }

        // Bonus for higher priority
        if (criteria.MinimumPriority.HasValue)
        {
            var priorityBonus = Math.Min(0.2, (metadata.Priority - criteria.MinimumPriority.Value) * 0.01);
            score += Math.Max(0, priorityBonus);
            factors++;
        }

        return Math.Min(1.0, score);
    }
}

/// <summary>
/// Result of a provider compatibility check.
/// </summary>
/// <param name="IsCompatible">Whether the provider is compatible with the criteria.</param>
/// <param name="Issues">List of compatibility issues found.</param>
public record ProviderCompatibilityResult(bool IsCompatible, IReadOnlyList<CompatibilityIssue> Issues);

/// <summary>
/// Result of a version compatibility check.
/// </summary>
/// <param name="IsCompatible">Whether the version is compatible.</param>
/// <param name="Issues">List of version compatibility issues found.</param>
public record VersionCompatibilityResult(bool IsCompatible, IReadOnlyList<CompatibilityIssue> Issues);

/// <summary>
/// Result of a dependency compatibility check.
/// </summary>
/// <param name="IsCompatible">Whether dependencies are satisfied.</param>
/// <param name="Issues">List of dependency issues found.</param>
public record DependencyCompatibilityResult(bool IsCompatible, IReadOnlyList<CompatibilityIssue> Issues);

/// <summary>
/// Represents a compatibility issue found during provider validation.
/// </summary>
/// <param name="Type">The type of compatibility issue.</param>
/// <param name="Message">Detailed message describing the issue.</param>
public record CompatibilityIssue(CompatibilityIssueType Type, string Message);

/// <summary>
/// Types of compatibility issues that can be detected.
/// </summary>
public enum CompatibilityIssueType
{
    /// <summary>Provider is missing required capabilities.</summary>
    MissingCapabilities,
    
    /// <summary>Provider doesn't support the target platform.</summary>
    PlatformIncompatible,
    
    /// <summary>Provider priority is too low.</summary>
    PriorityTooLow,
    
    /// <summary>Provider version is too old.</summary>
    VersionTooOld,
    
    /// <summary>Provider version is too new.</summary>
    VersionTooNew,
    
    /// <summary>Required dependency is missing.</summary>
    MissingDependency,
    
    /// <summary>Dependency version is too old.</summary>
    DependencyVersionTooOld
}