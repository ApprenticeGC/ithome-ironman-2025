using GameConsole.UI.Configuration;
using Xunit;

namespace GameConsole.UI.Configuration.Tests;

public class ProfileInheritanceManagerTests
{
    private readonly ProfileInheritanceManager _manager;

    public ProfileInheritanceManagerTests()
    {
        _manager = new ProfileInheritanceManager();
    }

    [Fact]
    public async Task ResolveConfigurationAsync_WithNoParent_ReturnsOriginalConfiguration()
    {
        // Arrange
        var config = ProfileConfigurationBuilder.Create()
            .WithId("standalone-profile")
            .WithName("Standalone Profile")
            .WithDescription("A profile without inheritance")
            .Build();

        // Act
        var resolved = await _manager.ResolveConfigurationAsync(config);

        // Assert
        Assert.Equal(config.Id, resolved.Id);
        Assert.Equal(config.Name, resolved.Name);
        Assert.Null(resolved.ParentProfileId);
    }

    [Fact]
    public async Task ResolveConfigurationAsync_WithInheritance_MergesConfigurations()
    {
        // Arrange
        var parentConfig = ProfileConfigurationBuilder.Create()
            .WithId("parent-profile")
            .WithName("Parent Profile")
            .WithDescription("Parent configuration")
            .AddJsonString("""
            {
                "UI": { "Theme": "Light", "FontSize": 12 },
                "Commands": { "DefaultTimeout": "30" }
            }
            """)
            .Build();

        var childConfig = ProfileConfigurationBuilder.Create()
            .WithId("child-profile")
            .WithName("Child Profile")
            .WithDescription("Child configuration")
            .InheritsFrom("parent-profile")
            .AddJsonString("""
            {
                "UI": { "Theme": "Dark" },
                "Layout": { "DefaultView": "Console" }
            }
            """)
            .Build();

        _manager.RegisterProfile(parentConfig);
        _manager.RegisterProfile(childConfig);

        // Act
        var resolved = await _manager.ResolveConfigurationAsync(childConfig);

        // Assert
        Assert.Equal("child-profile", resolved.Id);
        Assert.Equal("Dark", resolved.Configuration["UI:Theme"]); // Child overrides parent
        Assert.Equal("12", resolved.Configuration["UI:FontSize"]); // Inherited from parent
        Assert.Equal("30", resolved.Configuration["Commands:DefaultTimeout"]); // Inherited from parent
        Assert.Equal("Console", resolved.Configuration["Layout:DefaultView"]); // Child only
    }

    [Fact]
    public async Task GetInheritanceChainAsync_WithValidChain_ReturnsOrderedList()
    {
        // Arrange
        var rootConfig = ProfileConfigurationBuilder.Create()
            .WithId("root-profile")
            .WithName("Root Profile")
            .WithDescription("Root configuration")
            .Build();

        var middleConfig = ProfileConfigurationBuilder.Create()
            .WithId("middle-profile")
            .WithName("Middle Profile")
            .WithDescription("Middle configuration")
            .InheritsFrom("root-profile")
            .Build();

        var leafConfig = ProfileConfigurationBuilder.Create()
            .WithId("leaf-profile")
            .WithName("Leaf Profile")
            .WithDescription("Leaf configuration")
            .InheritsFrom("middle-profile")
            .Build();

        _manager.RegisterProfile(rootConfig);
        _manager.RegisterProfile(middleConfig);
        _manager.RegisterProfile(leafConfig);

        // Act
        var chain = await _manager.GetInheritanceChainAsync("leaf-profile");

        // Assert
        Assert.Equal(3, chain.Count);
        Assert.Equal("leaf-profile", chain[0].Id);
        Assert.Equal("middle-profile", chain[1].Id);
        Assert.Equal("root-profile", chain[2].Id);
    }

    [Fact]
    public async Task CanInheritFromAsync_WithValidInheritance_ReturnsTrue()
    {
        // Arrange
        var parentConfig = ProfileConfigurationBuilder.Create()
            .WithId("parent-profile")
            .WithName("Parent Profile")
            .WithDescription("Parent configuration")
            .Build();

        var childConfig = ProfileConfigurationBuilder.Create()
            .WithId("child-profile")
            .WithName("Child Profile")
            .WithDescription("Child configuration")
            .Build();

        _manager.RegisterProfile(parentConfig);
        _manager.RegisterProfile(childConfig);

        // Act
        var canInherit = await _manager.CanInheritFromAsync("child-profile", "parent-profile");

        // Assert
        Assert.True(canInherit);
    }

    [Fact]
    public async Task CanInheritFromAsync_WithSelfInheritance_ReturnsFalse()
    {
        // Arrange
        var config = ProfileConfigurationBuilder.Create()
            .WithId("self-profile")
            .WithName("Self Profile")
            .WithDescription("Self configuration")
            .Build();

        _manager.RegisterProfile(config);

        // Act
        var canInherit = await _manager.CanInheritFromAsync("self-profile", "self-profile");

        // Assert
        Assert.False(canInherit);
    }

    [Fact]
    public async Task CanInheritFromAsync_WithMissingProfile_ReturnsFalse()
    {
        // Arrange
        var config = ProfileConfigurationBuilder.Create()
            .WithId("existing-profile")
            .WithName("Existing Profile")
            .WithDescription("Existing configuration")
            .Build();

        _manager.RegisterProfile(config);

        // Act
        var canInherit = await _manager.CanInheritFromAsync("existing-profile", "missing-profile");

        // Assert
        Assert.False(canInherit);
    }

    [Fact]
    public async Task HasCircularDependencyAsync_WithValidChain_ReturnsFalse()
    {
        // Arrange
        var rootConfig = ProfileConfigurationBuilder.Create()
            .WithId("root-profile")
            .WithName("Root Profile")
            .WithDescription("Root configuration")
            .Build();

        var childConfig = ProfileConfigurationBuilder.Create()
            .WithId("child-profile")
            .WithName("Child Profile")
            .WithDescription("Child configuration")
            .InheritsFrom("root-profile")
            .Build();

        _manager.RegisterProfile(rootConfig);
        _manager.RegisterProfile(childConfig);

        // Act
        var hasCircular = await _manager.HasCircularDependencyAsync("child-profile");

        // Assert
        Assert.False(hasCircular);
    }

    [Fact]
    public async Task ResolveConfigurationAsync_WithCircularDependency_ThrowsException()
    {
        // Arrange - Create a circular dependency (A -> B -> A)
        var configA = CreateMockConfiguration("profile-a", "profile-b");
        var configB = CreateMockConfiguration("profile-b", "profile-a");

        _manager.RegisterProfile(configA);
        _manager.RegisterProfile(configB);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.ResolveConfigurationAsync(configA));
    }

    [Fact]
    public void RegisterProfile_WithValidConfiguration_RegistersSuccessfully()
    {
        // Arrange
        var config = ProfileConfigurationBuilder.Create()
            .WithId("register-test-profile")
            .WithName("Register Test Profile")
            .WithDescription("Profile for registration test")
            .Build();

        // Act
        _manager.RegisterProfile(config);

        // Assert
        var registeredIds = _manager.GetRegisteredProfileIds();
        Assert.Contains("register-test-profile", registeredIds);
    }

    [Fact]
    public void ClearCache_RemovesAllRegisteredProfiles()
    {
        // Arrange
        var config1 = ProfileConfigurationBuilder.Create()
            .WithId("profile-1")
            .WithName("Profile 1")
            .WithDescription("First profile")
            .Build();

        var config2 = ProfileConfigurationBuilder.Create()
            .WithId("profile-2")
            .WithName("Profile 2")
            .WithDescription("Second profile")
            .Build();

        _manager.RegisterProfile(config1);
        _manager.RegisterProfile(config2);

        // Act
        _manager.ClearCache();

        // Assert
        var registeredIds = _manager.GetRegisteredProfileIds();
        Assert.Empty(registeredIds);
    }

    [Fact]
    public async Task GetInheritanceChainAsync_WithMissingProfile_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.GetInheritanceChainAsync("missing-profile"));
    }

    /// <summary>
    /// Creates a mock configuration for testing circular dependencies.
    /// </summary>
    private static IProfileConfiguration CreateMockConfiguration(string id, string? parentId)
    {
        return new TestProfileConfiguration(id, parentId);
    }

    /// <summary>
    /// Test implementation of IProfileConfiguration for circular dependency testing.
    /// </summary>
    private sealed class TestProfileConfiguration : IProfileConfiguration
    {
        public TestProfileConfiguration(string id, string? parentProfileId)
        {
            Id = id;
            ParentProfileId = parentProfileId;
            Configuration = new Microsoft.Extensions.Configuration.ConfigurationManager();
        }

        public string Id { get; }
        public string Name => Id;
        public string Description => $"Test configuration for {Id}";
        public Version Version => new(1, 0, 0);
        public string Environment => "Test";
        public string? ParentProfileId { get; }
        public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        public Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ValidationResult.Success());
        }

        public T GetSection<T>(string sectionPath) where T : class, new()
        {
            return new T();
        }
    }
}