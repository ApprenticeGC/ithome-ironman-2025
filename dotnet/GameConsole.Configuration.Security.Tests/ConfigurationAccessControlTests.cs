using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameConsole.Configuration.Security;

namespace GameConsole.Configuration.Security.Tests;

/// <summary>
/// Unit tests for ConfigurationAccessControl implementation.
/// </summary>
public class ConfigurationAccessControlTests
{
    private readonly Mock<ILogger<ConfigurationAccessControl>> _mockLogger;
    private readonly ConfigurationAccessControl _accessControl;
    private const string TestUserId = "test-user";
    private const string TestPath = "test:configuration:path";

    public ConfigurationAccessControlTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationAccessControl>>();
        _accessControl = new ConfigurationAccessControl(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_SetsSystemUser()
    {
        // Act
        await _accessControl.InitializeAsync();
        
        // Assert
        var systemHasRole = await _accessControl.UserHasRoleAsync("system", ConfigurationRole.System);
        Assert.True(systemHasRole);
    }

    [Fact]
    public async Task AddUserToRoleAsync_ValidInput_AddsUserToRole()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        
        // Act
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.User);
        
        // Assert
        var hasRole = await _accessControl.UserHasRoleAsync(TestUserId, ConfigurationRole.User);
        Assert.True(hasRole);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_UserInRole_RemovesUserFromRole()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.User);
        
        // Act
        await _accessControl.RemoveUserFromRoleAsync(TestUserId, ConfigurationRole.User);
        
        // Assert
        var hasRole = await _accessControl.UserHasRoleAsync(TestUserId, ConfigurationRole.User);
        Assert.False(hasRole);
    }

    [Fact]
    public async Task GetUserRolesAsync_UserWithMultipleRoles_ReturnsAllRoles()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.User);
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.PowerUser);
        
        // Act
        var roles = await _accessControl.GetUserRolesAsync(TestUserId);
        
        // Assert
        Assert.Contains(ConfigurationRole.User, roles);
        Assert.Contains(ConfigurationRole.PowerUser, roles);
        Assert.Equal(2, roles.Count());
    }

    [Fact]
    public async Task CanReadConfigurationAsync_UserRole_ReturnsTrue()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.User);
        
        // Act
        var canRead = await _accessControl.CanReadConfigurationAsync(TestPath, TestUserId);
        
        // Assert
        Assert.True(canRead);
    }

    [Fact]
    public async Task CanWriteConfigurationAsync_GuestRole_ReturnsFalse()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.Guest);
        
        // Act
        var canWrite = await _accessControl.CanWriteConfigurationAsync(TestPath, TestUserId);
        
        // Assert
        Assert.False(canWrite);
    }

    [Fact]
    public async Task CanWriteConfigurationAsync_PowerUserRole_ReturnsTrue()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.PowerUser);
        
        // Act
        var canWrite = await _accessControl.CanWriteConfigurationAsync(TestPath, TestUserId);
        
        // Assert
        Assert.True(canWrite);
    }

    [Fact]
    public async Task CanDeleteConfigurationAsync_AdministratorRole_ReturnsTrue()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.Administrator);
        
        // Act
        var canDelete = await _accessControl.CanDeleteConfigurationAsync(TestPath, TestUserId);
        
        // Assert
        Assert.True(canDelete);
    }

    [Fact]
    public async Task SetPermissionsAsync_ValidInput_SetsExplicitPermissions()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.Guest); // Guest has no permissions by default
        
        // Act
        await _accessControl.SetPermissionsAsync(TestPath, TestUserId, ConfigurationPermissions.Read | ConfigurationPermissions.Write);
        var permissions = await _accessControl.GetPermissionsAsync(TestPath, TestUserId);
        
        // Assert
        Assert.True(permissions.HasFlag(ConfigurationPermissions.Read));
        Assert.True(permissions.HasFlag(ConfigurationPermissions.Write));
        Assert.False(permissions.HasFlag(ConfigurationPermissions.Delete));
    }

    [Fact]
    public async Task GetPermissionsAsync_ExplicitPermissions_OverrideRolePermissions()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.User); // User has read permissions
        await _accessControl.SetPermissionsAsync(TestPath, TestUserId, ConfigurationPermissions.FullControl);
        
        // Act
        var permissions = await _accessControl.GetPermissionsAsync(TestPath, TestUserId);
        
        // Assert
        Assert.Equal(ConfigurationPermissions.FullControl, permissions);
    }

    [Theory]
    [InlineData("test:password")]
    [InlineData("config:secret:key")]
    [InlineData("database:connectionstring")]
    [InlineData("api:token")]
    public async Task CanReadConfigurationAsync_SensitivePath_RequiresHighPrivileges(string sensitivePath)
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.User);
        
        // Act
        var canRead = await _accessControl.CanReadConfigurationAsync(sensitivePath, TestUserId);
        
        // Assert
        Assert.False(canRead, $"User should not be able to read sensitive path: {sensitivePath}");
    }

    [Theory]
    [InlineData("test:password")]
    [InlineData("config:secret:key")]
    [InlineData("database:connectionstring")]
    [InlineData("api:token")]
    public async Task CanReadConfigurationAsync_SensitivePathAdministrator_ReturnsTrue(string sensitivePath)
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.AddUserToRoleAsync(TestUserId, ConfigurationRole.Administrator);
        
        // Act
        var canRead = await _accessControl.CanReadConfigurationAsync(sensitivePath, TestUserId);
        
        // Assert
        Assert.True(canRead, $"Administrator should be able to read sensitive path: {sensitivePath}");
    }

    [Fact]
    public async Task CanReadConfigurationAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _accessControl.CanReadConfigurationAsync(TestPath, ""));
    }

    [Fact]
    public async Task CanReadConfigurationAsync_EmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _accessControl.CanReadConfigurationAsync("", TestUserId));
    }

    [Fact]
    public async Task UserHasRoleAsync_UserWithoutRoles_ReturnsFalse()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        
        // Act
        var hasRole = await _accessControl.UserHasRoleAsync("nonexistent-user", ConfigurationRole.User);
        
        // Assert
        Assert.False(hasRole);
    }

    [Fact]
    public async Task GetUserRolesAsync_UserWithoutRoles_ReturnsEmptyCollection()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        
        // Act
        var roles = await _accessControl.GetUserRolesAsync("nonexistent-user");
        
        // Assert
        Assert.Empty(roles);
    }

    [Fact]
    public async Task StartAsync_SetsIsRunning()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        
        // Act
        await _accessControl.StartAsync();
        
        // Assert
        Assert.True(_accessControl.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ClearsIsRunning()
    {
        // Arrange
        await _accessControl.InitializeAsync();
        await _accessControl.StartAsync();
        
        // Act
        await _accessControl.StopAsync();
        
        // Assert
        Assert.False(_accessControl.IsRunning);
    }
}