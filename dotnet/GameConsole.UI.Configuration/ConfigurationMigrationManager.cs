using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Manages configuration versioning and provides migration capabilities for UI profiles.
/// Supports upgrading configurations from older versions to newer formats.
/// </summary>
public class ConfigurationMigrationManager
{
    private readonly ILogger<ConfigurationMigrationManager>? _logger;
    private readonly Dictionary<string, IMigrationStrategy> _migrationStrategies = new();

    public ConfigurationMigrationManager(ILogger<ConfigurationMigrationManager>? logger = null)
    {
        _logger = logger;
        RegisterDefaultMigrations();
    }

    /// <summary>
    /// Registers a migration strategy for a specific version.
    /// </summary>
    /// <param name="fromVersion">The version to migrate from.</param>
    /// <param name="strategy">The migration strategy.</param>
    public void RegisterMigration(string fromVersion, IMigrationStrategy strategy)
    {
        _migrationStrategies[fromVersion] = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _logger?.LogDebug("Registered migration strategy from version {FromVersion}", fromVersion);
    }

    /// <summary>
    /// Checks if a configuration needs migration to the current version.
    /// </summary>
    /// <param name="configuration">The configuration to check.</param>
    /// <param name="targetVersion">The target version to migrate to.</param>
    /// <returns>True if migration is needed, otherwise false.</returns>
    public bool NeedsMigration(IProfileConfiguration configuration, string targetVersion)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var currentVersion = Version.Parse(configuration.Version);
        var target = Version.Parse(targetVersion);

        return currentVersion < target;
    }

    /// <summary>
    /// Migrates a configuration to the specified target version.
    /// </summary>
    /// <param name="configuration">The configuration to migrate.</param>
    /// <param name="targetVersion">The target version to migrate to.</param>
    /// <returns>The migrated configuration.</returns>
    public async Task<IProfileConfiguration> MigrateAsync(IProfileConfiguration configuration, string targetVersion)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (!NeedsMigration(configuration, targetVersion))
        {
            _logger?.LogDebug("Configuration {ProfileId} is already at or above target version {TargetVersion}", 
                configuration.ProfileId, targetVersion);
            return configuration;
        }

        _logger?.LogInformation("Migrating configuration {ProfileId} from {CurrentVersion} to {TargetVersion}",
            configuration.ProfileId, configuration.Version, targetVersion);

        var migrationPath = BuildMigrationPath(configuration.Version, targetVersion);
        var currentConfig = configuration;

        foreach (var step in migrationPath)
        {
            if (_migrationStrategies.TryGetValue(step.FromVersion, out var strategy))
            {
                _logger?.LogDebug("Applying migration step from {FromVersion} to {ToVersion}",
                    step.FromVersion, step.ToVersion);

                currentConfig = await strategy.MigrateAsync(currentConfig, step.ToVersion);
            }
            else
            {
                _logger?.LogWarning("No migration strategy found for version {FromVersion}", step.FromVersion);
                throw new InvalidOperationException($"No migration strategy found for version {step.FromVersion}");
            }
        }

        _logger?.LogInformation("Successfully migrated configuration {ProfileId} to version {TargetVersion}",
            currentConfig.ProfileId, currentConfig.Version);

        return currentConfig;
    }

    /// <summary>
    /// Creates a backup of a configuration before migration.
    /// </summary>
    /// <param name="configuration">The configuration to backup.</param>
    /// <param name="backupPath">The path to save the backup.</param>
    public async Task CreateBackupAsync(IProfileConfiguration configuration, string backupPath)
    {
        var serializer = new ProfileConfigurationSerializer();
        var backupFileName = $"{configuration.ProfileId}_{configuration.Version}_{DateTime.UtcNow:yyyyMMddHHmmss}.backup.json";
        var fullBackupPath = Path.Combine(backupPath, backupFileName);

        Directory.CreateDirectory(backupPath);
        await serializer.SaveToJsonFileAsync(configuration, fullBackupPath);

        _logger?.LogInformation("Created backup of configuration {ProfileId} at {BackupPath}",
            configuration.ProfileId, fullBackupPath);
    }

    /// <summary>
    /// Gets all supported migration versions.
    /// </summary>
    /// <returns>Collection of supported migration versions.</returns>
    public IEnumerable<string> GetSupportedVersions()
    {
        return _migrationStrategies.Keys.OrderBy(Version.Parse);
    }

    private List<MigrationStep> BuildMigrationPath(string fromVersion, string targetVersion)
    {
        var steps = new List<MigrationStep>();
        var currentVersion = Version.Parse(fromVersion);
        var target = Version.Parse(targetVersion);

        // Simple linear migration path - in a real scenario, you might need a more sophisticated approach
        var availableVersions = _migrationStrategies.Keys
            .Select(Version.Parse)
            .Where(v => v > currentVersion && v <= target)
            .OrderBy(v => v)
            .ToList();

        var previousVersion = fromVersion;
        foreach (var version in availableVersions)
        {
            steps.Add(new MigrationStep(previousVersion, version.ToString()));
            previousVersion = version.ToString();
        }

        return steps;
    }

    private void RegisterDefaultMigrations()
    {
        // Register built-in migration strategies
        RegisterMigration("1.0.0", new Migration_1_0_to_1_1());
        RegisterMigration("1.1.0", new Migration_1_1_to_2_0());
    }
}

/// <summary>
/// Represents a single migration step between two versions.
/// </summary>
public record MigrationStep(string FromVersion, string ToVersion);

/// <summary>
/// Interface for configuration migration strategies.
/// </summary>
public interface IMigrationStrategy
{
    /// <summary>
    /// Migrates a configuration from one version to another.
    /// </summary>
    /// <param name="configuration">The configuration to migrate.</param>
    /// <param name="targetVersion">The target version to migrate to.</param>
    /// <returns>The migrated configuration.</returns>
    Task<IProfileConfiguration> MigrateAsync(IProfileConfiguration configuration, string targetVersion);
}

/// <summary>
/// Migration strategy from version 1.0.0 to 1.1.0.
/// </summary>
internal class Migration_1_0_to_1_1 : IMigrationStrategy
{
    public Task<IProfileConfiguration> MigrateAsync(IProfileConfiguration configuration, string targetVersion)
    {
        var builder = ProfileConfigurationBuilder.FromExisting(configuration);
        
        // Migration changes for 1.1.0
        builder.WithVersion(targetVersion);
        
        // Add new metadata fields introduced in 1.1.0
        builder.WithMetadata("MigrationDate", DateTime.UtcNow);
        builder.WithMetadata("MigrationFrom", configuration.Version);
        
        // Convert old UI settings format if present
        if (configuration.HasKey("WindowWidth") && configuration.HasKey("WindowHeight"))
        {
            var width = configuration.GetValue<int>("WindowWidth");
            var height = configuration.GetValue<int>("WindowHeight");
            
            builder.WithSetting("UI:Window:Width", width);
            builder.WithSetting("UI:Window:Height", height);
        }

        return Task.FromResult(builder.Build());
    }
}

/// <summary>
/// Migration strategy from version 1.1.0 to 2.0.0.
/// </summary>
internal class Migration_1_1_to_2_0 : IMigrationStrategy
{
    public Task<IProfileConfiguration> MigrateAsync(IProfileConfiguration configuration, string targetVersion)
    {
        var builder = ProfileConfigurationBuilder.FromExisting(configuration);
        
        // Migration changes for 2.0.0
        builder.WithVersion(targetVersion);
        
        // Update metadata
        builder.WithMetadata("MigrationDate", DateTime.UtcNow);
        builder.WithMetadata("MigrationFrom", configuration.Version);
        
        // Restructure theme settings for new theme system in 2.0.0
        if (configuration.HasKey("UI:Theme"))
        {
            var oldTheme = configuration.GetValue<string>("UI:Theme");
            builder.WithSetting("UI:Theme:Name", oldTheme);
            builder.WithSetting("UI:Theme:Version", "2.0");
            
            // Set default theme properties
            builder.WithSetting("UI:Theme:ColorScheme", "Auto");
            builder.WithSetting("UI:Theme:FontScale", 1.0);
        }

        // Convert old layout settings
        if (configuration.HasKey("UI:Layout"))
        {
            var oldLayout = configuration.GetValue<string>("UI:Layout");
            builder.WithSetting("UI:Layout:Type", oldLayout);
            builder.WithSetting("UI:Layout:Responsive", true);
        }

        return Task.FromResult(builder.Build());
    }
}

/// <summary>
/// Provides utilities for configuration version management.
/// </summary>
public static class VersionUtilities
{
    /// <summary>
    /// Gets the current supported configuration version.
    /// </summary>
    public static string CurrentVersion => "2.0.0";

    /// <summary>
    /// Validates that a version string is in a valid format.
    /// </summary>
    /// <param name="version">The version string to validate.</param>
    /// <returns>True if the version is valid, otherwise false.</returns>
    public static bool IsValidVersion(string version)
    {
        return Version.TryParse(version, out _);
    }

    /// <summary>
    /// Compares two version strings.
    /// </summary>
    /// <param name="version1">The first version.</param>
    /// <param name="version2">The second version.</param>
    /// <returns>-1 if version1 &lt; version2, 0 if equal, 1 if version1 &gt; version2.</returns>
    public static int CompareVersions(string version1, string version2)
    {
        var v1 = Version.Parse(version1);
        var v2 = Version.Parse(version2);
        return v1.CompareTo(v2);
    }

    /// <summary>
    /// Gets the major version number from a version string.
    /// </summary>
    /// <param name="version">The version string.</param>
    /// <returns>The major version number.</returns>
    public static int GetMajorVersion(string version)
    {
        return Version.Parse(version).Major;
    }

    /// <summary>
    /// Creates a new version string with an incremented minor version.
    /// </summary>
    /// <param name="currentVersion">The current version.</param>
    /// <returns>The new version string.</returns>
    public static string IncrementMinorVersion(string currentVersion)
    {
        var version = Version.Parse(currentVersion);
        return new Version(version.Major, version.Minor + 1, 0).ToString();
    }
}