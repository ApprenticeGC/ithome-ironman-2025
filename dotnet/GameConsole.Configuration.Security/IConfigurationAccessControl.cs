using GameConsole.Core.Abstractions;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Interface for role-based access control for configuration operations.
/// Provides permission management and access validation.
/// </summary>
public interface IConfigurationAccessControl : IService
{
    /// <summary>
    /// Validates if the current user has permission to read a specific configuration.
    /// </summary>
    /// <param name="configurationPath">The path to the configuration to check.</param>
    /// <param name="userId">The user identifier to check permissions for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the user has read permission, false otherwise.</returns>
    Task<bool> CanReadConfigurationAsync(string configurationPath, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the current user has permission to write to a specific configuration.
    /// </summary>
    /// <param name="configurationPath">The path to the configuration to check.</param>
    /// <param name="userId">The user identifier to check permissions for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the user has write permission, false otherwise.</returns>
    Task<bool> CanWriteConfigurationAsync(string configurationPath, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the current user has permission to delete a specific configuration.
    /// </summary>
    /// <param name="configurationPath">The path to the configuration to check.</param>
    /// <param name="userId">The user identifier to check permissions for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the user has delete permission, false otherwise.</returns>
    Task<bool> CanDeleteConfigurationAsync(string configurationPath, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective permissions for a user on a specific configuration path.
    /// </summary>
    /// <param name="configurationPath">The path to check permissions for.</param>
    /// <param name="userId">The user identifier to check permissions for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The effective permissions for the user.</returns>
    Task<ConfigurationPermissions> GetPermissionsAsync(string configurationPath, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets permissions for a user on a specific configuration path.
    /// </summary>
    /// <param name="configurationPath">The path to set permissions for.</param>
    /// <param name="userId">The user identifier to set permissions for.</param>
    /// <param name="permissions">The permissions to set.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the permission setting operation.</returns>
    Task SetPermissionsAsync(string configurationPath, string userId, ConfigurationPermissions permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user to a role for configuration access.
    /// </summary>
    /// <param name="userId">The user identifier to add to the role.</param>
    /// <param name="role">The role to add the user to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the role assignment operation.</returns>
    Task AddUserToRoleAsync(string userId, ConfigurationRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from a role for configuration access.
    /// </summary>
    /// <param name="userId">The user identifier to remove from the role.</param>
    /// <param name="role">The role to remove the user from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the role removal operation.</returns>
    Task RemoveUserFromRoleAsync(string userId, ConfigurationRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the roles assigned to a user.
    /// </summary>
    /// <param name="userId">The user identifier to get roles for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of roles assigned to the user.</returns>
    Task<IEnumerable<ConfigurationRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a user has a specific role.
    /// </summary>
    /// <param name="userId">The user identifier to check.</param>
    /// <param name="role">The role to check for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the user has the role, false otherwise.</returns>
    Task<bool> UserHasRoleAsync(string userId, ConfigurationRole role, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents configuration access permissions.
/// </summary>
[Flags]
public enum ConfigurationPermissions
{
    /// <summary>No permissions.</summary>
    None = 0,
    /// <summary>Read permission.</summary>
    Read = 1,
    /// <summary>Write permission.</summary>
    Write = 2,
    /// <summary>Delete permission.</summary>
    Delete = 4,
    /// <summary>Full control (all permissions).</summary>
    FullControl = Read | Write | Delete
}

/// <summary>
/// Represents configuration access roles.
/// </summary>
public enum ConfigurationRole
{
    /// <summary>Guest user with minimal read access.</summary>
    Guest,
    /// <summary>Standard user with read access to non-sensitive configurations.</summary>
    User,
    /// <summary>Power user with read/write access to most configurations.</summary>
    PowerUser,
    /// <summary>Administrator with full access to all configurations.</summary>
    Administrator,
    /// <summary>System account with unrestricted access.</summary>
    System
}