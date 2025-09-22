using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Implementation of role-based access control for configuration operations.
/// Provides in-memory permission management with extensible role system.
/// </summary>
public class ConfigurationAccessControl : IConfigurationAccessControl
{
    private readonly ILogger<ConfigurationAccessControl> _logger;
    private readonly ConcurrentDictionary<string, HashSet<ConfigurationRole>> _userRoles;
    private readonly ConcurrentDictionary<string, Dictionary<string, ConfigurationPermissions>> _pathPermissions;
    private readonly Dictionary<ConfigurationRole, ConfigurationPermissions> _defaultRolePermissions;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public ConfigurationAccessControl(ILogger<ConfigurationAccessControl> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRoles = new ConcurrentDictionary<string, HashSet<ConfigurationRole>>();
        _pathPermissions = new ConcurrentDictionary<string, Dictionary<string, ConfigurationPermissions>>();
        
        // Initialize default permissions for each role
        _defaultRolePermissions = new Dictionary<ConfigurationRole, ConfigurationPermissions>
        {
            { ConfigurationRole.Guest, ConfigurationPermissions.None },
            { ConfigurationRole.User, ConfigurationPermissions.Read },
            { ConfigurationRole.PowerUser, ConfigurationPermissions.Read | ConfigurationPermissions.Write },
            { ConfigurationRole.Administrator, ConfigurationPermissions.FullControl },
            { ConfigurationRole.System, ConfigurationPermissions.FullControl }
        };
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Configuration Access Control");

        // Initialize default system user with full permissions
        _userRoles.TryAdd("system", new HashSet<ConfigurationRole> { ConfigurationRole.System });

        _logger.LogInformation("Configuration Access Control initialized");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Configuration Access Control");
        _isRunning = true;
        _logger.LogInformation("Configuration Access Control started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Configuration Access Control");
        _isRunning = false;
        _logger.LogInformation("Configuration Access Control stopped");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _isRunning = false;
        _userRoles.Clear();
        _pathPermissions.Clear();
        return ValueTask.CompletedTask;
    }

    public Task<bool> CanReadConfigurationAsync(string configurationPath, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationPath))
            throw new ArgumentException("Configuration path cannot be null or empty.", nameof(configurationPath));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var permissions = GetEffectivePermissions(configurationPath, userId);
            bool canRead = permissions.HasFlag(ConfigurationPermissions.Read);

            _logger.LogDebug("User {UserId} read permission for {Path}: {CanRead}", userId, configurationPath, canRead);
            return Task.FromResult(canRead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking read permission for user {UserId} on path {Path}", userId, configurationPath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> CanWriteConfigurationAsync(string configurationPath, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationPath))
            throw new ArgumentException("Configuration path cannot be null or empty.", nameof(configurationPath));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var permissions = GetEffectivePermissions(configurationPath, userId);
            bool canWrite = permissions.HasFlag(ConfigurationPermissions.Write);

            _logger.LogDebug("User {UserId} write permission for {Path}: {CanWrite}", userId, configurationPath, canWrite);
            return Task.FromResult(canWrite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking write permission for user {UserId} on path {Path}", userId, configurationPath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> CanDeleteConfigurationAsync(string configurationPath, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationPath))
            throw new ArgumentException("Configuration path cannot be null or empty.", nameof(configurationPath));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var permissions = GetEffectivePermissions(configurationPath, userId);
            bool canDelete = permissions.HasFlag(ConfigurationPermissions.Delete);

            _logger.LogDebug("User {UserId} delete permission for {Path}: {CanDelete}", userId, configurationPath, canDelete);
            return Task.FromResult(canDelete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking delete permission for user {UserId} on path {Path}", userId, configurationPath);
            return Task.FromResult(false);
        }
    }

    public Task<ConfigurationPermissions> GetPermissionsAsync(string configurationPath, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationPath))
            throw new ArgumentException("Configuration path cannot be null or empty.", nameof(configurationPath));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var permissions = GetEffectivePermissions(configurationPath, userId);
            _logger.LogDebug("User {UserId} effective permissions for {Path}: {Permissions}", userId, configurationPath, permissions);
            return Task.FromResult(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId} on path {Path}", userId, configurationPath);
            return Task.FromResult(ConfigurationPermissions.None);
        }
    }

    public Task SetPermissionsAsync(string configurationPath, string userId, ConfigurationPermissions permissions, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationPath))
            throw new ArgumentException("Configuration path cannot be null or empty.", nameof(configurationPath));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var pathPermissions = _pathPermissions.GetOrAdd(configurationPath, _ => new Dictionary<string, ConfigurationPermissions>());
            
            lock (pathPermissions)
            {
                pathPermissions[userId] = permissions;
            }

            _logger.LogInformation("Set permissions for user {UserId} on path {Path}: {Permissions}", userId, configurationPath, permissions);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting permissions for user {UserId} on path {Path}", userId, configurationPath);
            throw;
        }
    }

    public Task AddUserToRoleAsync(string userId, ConfigurationRole role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var userRoles = _userRoles.GetOrAdd(userId, _ => new HashSet<ConfigurationRole>());
            
            lock (userRoles)
            {
                userRoles.Add(role);
            }

            _logger.LogInformation("Added user {UserId} to role {Role}", userId, role);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to role {Role}", userId, role);
            throw;
        }
    }

    public Task RemoveUserFromRoleAsync(string userId, ConfigurationRole role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            if (_userRoles.TryGetValue(userId, out var userRoles))
            {
                lock (userRoles)
                {
                    userRoles.Remove(role);
                }

                _logger.LogInformation("Removed user {UserId} from role {Role}", userId, role);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from role {Role}", userId, role);
            throw;
        }
    }

    public Task<IEnumerable<ConfigurationRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            if (_userRoles.TryGetValue(userId, out var userRoles))
            {
                lock (userRoles)
                {
                    var roles = userRoles.ToList();
                    _logger.LogDebug("User {UserId} has roles: {Roles}", userId, string.Join(", ", roles));
                    return Task.FromResult<IEnumerable<ConfigurationRole>>(roles);
                }
            }

            _logger.LogDebug("User {UserId} has no roles assigned", userId);
            return Task.FromResult<IEnumerable<ConfigurationRole>>(Enumerable.Empty<ConfigurationRole>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            throw;
        }
    }

    public Task<bool> UserHasRoleAsync(string userId, ConfigurationRole role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            if (_userRoles.TryGetValue(userId, out var userRoles))
            {
                lock (userRoles)
                {
                    bool hasRole = userRoles.Contains(role);
                    _logger.LogDebug("User {UserId} has role {Role}: {HasRole}", userId, role, hasRole);
                    return Task.FromResult(hasRole);
                }
            }

            _logger.LogDebug("User {UserId} does not have role {Role}", userId, role);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} has role {Role}", userId, role);
            return Task.FromResult(false);
        }
    }

    private ConfigurationPermissions GetEffectivePermissions(string configurationPath, string userId)
    {
        var effectivePermissions = ConfigurationPermissions.None;

        // Check explicit path permissions first
        if (_pathPermissions.TryGetValue(configurationPath, out var pathPermissions))
        {
            lock (pathPermissions)
            {
                if (pathPermissions.TryGetValue(userId, out var explicitPermissions))
                {
                    effectivePermissions = explicitPermissions;
                }
            }
        }

        // If no explicit permissions, use role-based permissions
        if (effectivePermissions == ConfigurationPermissions.None && _userRoles.TryGetValue(userId, out var userRoles))
        {
            lock (userRoles)
            {
                foreach (var role in userRoles)
                {
                    if (_defaultRolePermissions.TryGetValue(role, out var rolePermissions))
                    {
                        effectivePermissions |= rolePermissions;
                    }
                }
            }
        }

        // Check for path-specific restrictions based on sensitivity
        effectivePermissions = ApplyPathBasedRestrictions(configurationPath, effectivePermissions, userId);

        return effectivePermissions;
    }

    private ConfigurationPermissions ApplyPathBasedRestrictions(string configurationPath, ConfigurationPermissions permissions, string userId)
    {
        // Apply restrictions for sensitive configuration paths
        var lowerPath = configurationPath.ToLowerInvariant();
        
        // Restrict access to security-related configurations
        if (lowerPath.Contains("password") || lowerPath.Contains("secret") || lowerPath.Contains("key") || lowerPath.Contains("token") || lowerPath.Contains("connectionstring"))
        {
            // Only administrators and system can access sensitive data
            if (_userRoles.TryGetValue(userId, out var userRoles))
            {
                lock (userRoles)
                {
                    var hasHighPrivileges = userRoles.Contains(ConfigurationRole.Administrator) || userRoles.Contains(ConfigurationRole.System);
                    if (!hasHighPrivileges)
                    {
                        return ConfigurationPermissions.None;
                    }
                }
            }
            else
            {
                // User has no roles, so no access to sensitive data
                return ConfigurationPermissions.None;
            }
        }

        return permissions;
    }
}