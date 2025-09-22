using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameConsole.Configuration.Security
{
    /// <summary>
    /// Role-based access control for configuration management.
    /// Implements Tier 3 service logic following the 4-tier architecture.
    /// </summary>
    public class ConfigurationAccessControl
    {
        private readonly ILogger<ConfigurationAccessControl> _logger;
        private readonly Dictionary<string, HashSet<string>> _rolePermissions;
        private readonly Dictionary<string, HashSet<string>> _userRoles;

        /// <summary>
        /// Defines the available configuration permissions.
        /// </summary>
        public static class Permissions
        {
            public const string ReadConfiguration = "config:read";
            public const string WriteConfiguration = "config:write";
            public const string EncryptConfiguration = "config:encrypt";
            public const string ManageKeys = "config:keys";
            public const string AuditAccess = "config:audit";
        }

        /// <summary>
        /// Defines the standard security roles.
        /// </summary>
        public static class Roles
        {
            public const string ConfigurationAdmin = "config-admin";
            public const string ConfigurationUser = "config-user";
            public const string SecurityOfficer = "security-officer";
            public const string ReadOnlyUser = "readonly-user";
        }

        public ConfigurationAccessControl(ILogger<ConfigurationAccessControl> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rolePermissions = new Dictionary<string, HashSet<string>>();
            _userRoles = new Dictionary<string, HashSet<string>>();

            InitializeDefaultRoles();
        }

        /// <summary>
        /// Checks if the current user has the specified permission for configuration access.
        /// </summary>
        /// <param name="user">The user identity to check</param>
        /// <param name="permission">The required permission</param>
        /// <param name="configurationSection">Optional specific configuration section</param>
        /// <returns>True if access is granted</returns>
        public async Task<bool> HasPermissionAsync(IIdentity user, string permission, string? configurationSection = null)
        {
            if (user?.Name == null)
            {
                _logger.LogWarning("Access denied: Invalid user identity");
                return false;
            }

            var userRoles = GetUserRoles(user.Name);
            foreach (var role in userRoles)
            {
                if (_rolePermissions.TryGetValue(role, out var permissions) &&
                    permissions.Contains(permission))
                {
                    _logger.LogInformation("Access granted for user {User} with permission {Permission} via role {Role}",
                        user.Name, permission, role);
                    return true;
                }
            }

            _logger.LogWarning("Access denied for user {User} requesting permission {Permission} on section {Section}",
                user.Name, permission, configurationSection ?? "global");

            return false;
        }

        /// <summary>
        /// Assigns a role to a user for configuration access.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="role">The role to assign</param>
        /// <returns>Task representing the assignment operation</returns>
        public Task AssignRoleAsync(string userName, string role)
        {
            if (!_rolePermissions.ContainsKey(role))
            {
                throw new ArgumentException($"Unknown role: {role}", nameof(role));
            }

            if (!_userRoles.ContainsKey(userName))
            {
                _userRoles[userName] = new HashSet<string>();
            }

            _userRoles[userName].Add(role);

            _logger.LogInformation("Assigned role {Role} to user {User}", role, userName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="role">The role to remove</param>
        /// <returns>Task representing the removal operation</returns>
        public Task RemoveRoleAsync(string userName, string role)
        {
            if (_userRoles.TryGetValue(userName, out var roles))
            {
                roles.Remove(role);
                if (roles.Count == 0)
                {
                    _userRoles.Remove(userName);
                }
            }

            _logger.LogInformation("Removed role {Role} from user {User}", role, userName);
            return Task.CompletedTask;
        }

        private HashSet<string> GetUserRoles(string userName)
        {
            return _userRoles.TryGetValue(userName, out var roles) ? roles : new HashSet<string>();
        }

        private void InitializeDefaultRoles()
        {
            // Configuration Administrator - full access
            _rolePermissions[Roles.ConfigurationAdmin] = new HashSet<string>
            {
                Permissions.ReadConfiguration,
                Permissions.WriteConfiguration,
                Permissions.EncryptConfiguration,
                Permissions.ManageKeys,
                Permissions.AuditAccess
            };

            // Configuration User - read/write but no security operations
            _rolePermissions[Roles.ConfigurationUser] = new HashSet<string>
            {
                Permissions.ReadConfiguration,
                Permissions.WriteConfiguration
            };

            // Security Officer - encryption and key management only
            _rolePermissions[Roles.SecurityOfficer] = new HashSet<string>
            {
                Permissions.EncryptConfiguration,
                Permissions.ManageKeys,
                Permissions.AuditAccess
            };

            // Read-only User - minimal access
            _rolePermissions[Roles.ReadOnlyUser] = new HashSet<string>
            {
                Permissions.ReadConfiguration
            };

            _logger.LogInformation("Initialized {RoleCount} default security roles", _rolePermissions.Count);
        }
    }
}