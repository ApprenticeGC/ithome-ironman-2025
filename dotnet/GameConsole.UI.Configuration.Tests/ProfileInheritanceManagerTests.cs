namespace GameConsole.UI.Configuration.Tests;

public class ProfileInheritanceManagerTests
{
    private readonly ProfileInheritanceManager _manager;
    private readonly IProfileConfiguration _parentConfig;
    private readonly IProfileConfiguration _childConfig;
    private readonly IProfileConfiguration _grandchildConfig;

    public ProfileInheritanceManagerTests()
    {
        _manager = new ProfileInheritanceManager();
        
        _parentConfig = new ProfileConfigurationBuilder()
            .WithId("parent")
            .WithName("Parent Profile")
            .WithSetting("parentSetting", "parentValue")
            .WithSetting("sharedSetting", "parentShared")
            .Build();

        _childConfig = new ProfileConfigurationBuilder()
            .WithId("child")
            .WithName("Child Profile")
            .InheritsFrom("parent")
            .WithSetting("childSetting", "childValue")
            .WithSetting("sharedSetting", "childShared") // Override parent
            .Build();

        _grandchildConfig = new ProfileConfigurationBuilder()
            .WithId("grandchild")
            .WithName("Grandchild Profile")
            .InheritsFrom("child")
            .WithSetting("grandchildSetting", "grandchildValue")
            .WithSetting("sharedSetting", "grandchildShared") // Override child and parent
            .Build();
    }

    [Fact]
    public void RegisterProfile_WithValidProfile_AddsToManager()
    {
        // Act
        _manager.RegisterProfile(_parentConfig);

        // Assert
        var profiles = _manager.GetAllProfiles();
        Assert.Single(profiles);
        Assert.Equal("parent", profiles.First().Key);
    }

    [Fact]
    public void RegisterProfile_WithDuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _manager.RegisterProfile(_parentConfig));
        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void UnregisterProfile_WithExistingProfile_RemovesFromManager()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);
        
        // Act
        var result = _manager.UnregisterProfile("parent");

        // Assert
        Assert.True(result);
        Assert.Empty(_manager.GetAllProfiles());
    }

    [Fact]
    public void UnregisterProfile_WithNonExistentProfile_ReturnsFalse()
    {
        // Act
        var result = _manager.UnregisterProfile("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ResolveConfiguration_WithSimpleInheritance_MergesSettings()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);
        _manager.RegisterProfile(_childConfig);

        // Act
        var resolved = _manager.ResolveConfiguration("child");

        // Assert
        Assert.Equal("child", resolved.Id);
        Assert.Equal("parentValue", resolved.GetValue<string>("parentSetting")); // Inherited
        Assert.Equal("childValue", resolved.GetValue<string>("childSetting")); // Own setting
        Assert.Equal("childShared", resolved.GetValue<string>("sharedSetting")); // Override
    }

    [Fact]
    public void ResolveConfiguration_WithMultiLevelInheritance_MergesAllLevels()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);
        _manager.RegisterProfile(_childConfig);
        _manager.RegisterProfile(_grandchildConfig);

        // Act
        var resolved = _manager.ResolveConfiguration("grandchild");

        // Assert
        Assert.Equal("grandchild", resolved.Id);
        Assert.Equal("parentValue", resolved.GetValue<string>("parentSetting")); // From grandparent
        Assert.Equal("childValue", resolved.GetValue<string>("childSetting")); // From parent
        Assert.Equal("grandchildValue", resolved.GetValue<string>("grandchildSetting")); // Own setting
        Assert.Equal("grandchildShared", resolved.GetValue<string>("sharedSetting")); // Final override
    }

    [Fact]
    public void ResolveConfiguration_WithNonExistentProfile_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _manager.ResolveConfiguration("nonexistent"));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void ResolveConfiguration_WithCircularInheritance_ThrowsInvalidOperationException()
    {
        // Arrange
        var profile1 = new ProfileConfigurationBuilder()
            .WithId("profile1")
            .WithName("Profile 1")
            .InheritsFrom("profile2")
            .Build();

        var profile2 = new ProfileConfigurationBuilder()
            .WithId("profile2")
            .WithName("Profile 2")
            .InheritsFrom("profile1")
            .Build();

        _manager.RegisterProfile(profile1);
        _manager.RegisterProfile(profile2);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _manager.ResolveConfiguration("profile1"));
        Assert.Contains("Circular inheritance", exception.Message);
    }

    [Fact]
    public void GetChildProfiles_WithRegisteredChildren_ReturnsChildIds()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);
        _manager.RegisterProfile(_childConfig);
        _manager.RegisterProfile(_grandchildConfig);

        // Act
        var parentChildren = _manager.GetChildProfiles("parent");
        var childChildren = _manager.GetChildProfiles("child");

        // Assert
        Assert.Single(parentChildren);
        Assert.Contains("child", parentChildren);
        Assert.Single(childChildren);
        Assert.Contains("grandchild", childChildren);
    }

    [Fact]
    public void GetChildProfiles_WithNoChildren_ReturnsEmpty()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);

        // Act
        var children = _manager.GetChildProfiles("parent");

        // Assert
        Assert.Empty(children);
    }

    [Fact]
    public void GetInheritanceChain_WithMultiLevelInheritance_ReturnsCompleteChain()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);
        _manager.RegisterProfile(_childConfig);
        _manager.RegisterProfile(_grandchildConfig);

        // Act
        var chain = _manager.GetInheritanceChain("grandchild").ToList();

        // Assert
        Assert.Equal(3, chain.Count);
        Assert.Equal("parent", chain[0]);
        Assert.Equal("child", chain[1]);
        Assert.Equal("grandchild", chain[2]);
    }

    [Fact]
    public void ValidateInheritance_WithValidHierarchy_ReturnsSuccess()
    {
        // Arrange
        _manager.RegisterProfile(_parentConfig);
        _manager.RegisterProfile(_childConfig);

        // Act
        var result = _manager.ValidateInheritance();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateInheritance_WithMissingParent_ReturnsError()
    {
        // Arrange
        _manager.RegisterProfile(_childConfig); // Parent not registered

        // Act
        var result = _manager.ValidateInheritance();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0].Message);
        Assert.Equal("MISSING_PARENT", result.Errors[0].ErrorCode);
    }

    [Fact]
    public void CreateMultiInheritanceProfile_WithMultipleParents_MergesAllParents()
    {
        // Arrange
        var parent1 = new ProfileConfigurationBuilder()
            .WithId("parent1")
            .WithName("Parent 1")
            .WithSetting("setting1", "value1")
            .WithSetting("shared", "parent1Value")
            .Build();

        var parent2 = new ProfileConfigurationBuilder()
            .WithId("parent2")
            .WithName("Parent 2")
            .WithSetting("setting2", "value2")
            .WithSetting("shared", "parent2Value") // Should override parent1
            .Build();

        _manager.RegisterProfile(parent1);
        _manager.RegisterProfile(parent2);

        var additionalSettings = new Dictionary<string, object?>
        {
            ["newSetting"] = "newValue"
        };

        // Act
        var multiConfig = _manager.CreateMultiInheritanceProfile(
            "multi",
            ["parent1", "parent2"],
            additionalSettings);

        // Assert
        Assert.Equal("multi", multiConfig.Id);
        Assert.Equal("value1", multiConfig.GetValue<string>("setting1")); // From parent1
        Assert.Equal("value2", multiConfig.GetValue<string>("setting2")); // From parent2
        Assert.Equal("parent2Value", multiConfig.GetValue<string>("shared")); // parent2 overrides parent1
        Assert.Equal("newValue", multiConfig.GetValue<string>("newSetting")); // Additional setting
    }

    [Fact]
    public void CreateMultiInheritanceProfile_WithNoValidParents_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => _manager.CreateMultiInheritanceProfile("multi", ["nonexistent"]));
        Assert.Contains("at least one valid parent", exception.Message);
    }
}