using Xunit;

namespace GameConsole.UI.Configuration.Tests;

public class ProfileInheritanceManagerTests
{
    private readonly ProfileInheritanceManager _manager = new();

    [Fact]
    public void RegisterProfile_ValidProfile_ProfileIsRegistered()
    {
        // Arrange
        var profile = CreateTestProfile("test-profile");

        // Act
        _manager.RegisterProfile(profile);

        // Assert - No exception should be thrown, profile should be retrievable
        var resolved = _manager.ResolveInheritance("test-profile");
        Assert.Equal("test-profile", resolved.ProfileId);
    }

    [Fact]
    public void ResolveInheritance_ProfileWithNoParent_ReturnsOriginalProfile()
    {
        // Arrange
        var profile = CreateTestProfile("standalone-profile");
        _manager.RegisterProfile(profile);

        // Act
        var resolved = _manager.ResolveInheritance("standalone-profile");

        // Assert
        Assert.Equal(profile.ProfileId, resolved.ProfileId);
        Assert.Equal(profile.Name, resolved.Name);
    }

    [Fact]
    public void ResolveInheritance_WithSimpleInheritance_MergesConfigurations()
    {
        // Arrange
        var parentProfile = new ProfileConfigurationBuilder()
            .WithProfileId("parent")
            .WithName("Parent Profile")
            .WithSetting("ParentSetting", "ParentValue")
            .WithSetting("SharedSetting", "ParentSharedValue")
            .Build();

        var childProfile = new ProfileConfigurationBuilder()
            .WithProfileId("child")
            .WithName("Child Profile")
            .InheritsFrom("parent")
            .WithSetting("ChildSetting", "ChildValue")
            .WithSetting("SharedSetting", "ChildSharedValue")
            .Build();

        _manager.RegisterProfile(parentProfile);
        _manager.RegisterProfile(childProfile);

        // Act
        var resolved = _manager.ResolveInheritance("child");

        // Assert
        Assert.Equal("ParentValue", resolved.GetValue<string>("ParentSetting"));
        Assert.Equal("ChildValue", resolved.GetValue<string>("ChildSetting"));
        Assert.Equal("ChildSharedValue", resolved.GetValue<string>("SharedSetting")); // Child should override
    }

    [Fact]
    public void ResolveInheritance_WithChainedInheritance_MergesAllLevels()
    {
        // Arrange
        var grandparent = new ProfileConfigurationBuilder()
            .WithProfileId("grandparent")
            .WithName("Grandparent")
            .WithSetting("GrandparentSetting", "GrandparentValue")
            .WithSetting("SharedSetting", "GrandparentSharedValue")
            .Build();

        var parent = new ProfileConfigurationBuilder()
            .WithProfileId("parent")
            .WithName("Parent")
            .InheritsFrom("grandparent")
            .WithSetting("ParentSetting", "ParentValue")
            .WithSetting("SharedSetting", "ParentSharedValue")
            .Build();

        var child = new ProfileConfigurationBuilder()
            .WithProfileId("child")
            .WithName("Child")
            .InheritsFrom("parent")
            .WithSetting("ChildSetting", "ChildValue")
            .Build();

        _manager.RegisterProfile(grandparent);
        _manager.RegisterProfile(parent);
        _manager.RegisterProfile(child);

        // Act
        var resolved = _manager.ResolveInheritance("child");

        // Assert
        Assert.Equal("GrandparentValue", resolved.GetValue<string>("GrandparentSetting"));
        Assert.Equal("ParentValue", resolved.GetValue<string>("ParentSetting"));
        Assert.Equal("ChildValue", resolved.GetValue<string>("ChildSetting"));
        Assert.Equal("ParentSharedValue", resolved.GetValue<string>("SharedSetting")); // Parent overrides grandparent
    }

    [Fact]
    public void GetInheritanceChain_WithChainedInheritance_ReturnsCorrectOrder()
    {
        // Arrange
        var grandparent = CreateTestProfile("grandparent");
        var parent = CreateTestProfile("parent", "grandparent");
        var child = CreateTestProfile("child", "parent");

        _manager.RegisterProfile(grandparent);
        _manager.RegisterProfile(parent);
        _manager.RegisterProfile(child);

        // Act
        var chain = _manager.GetInheritanceChain("child");

        // Assert
        Assert.Equal(3, chain.Count);
        Assert.Equal("grandparent", chain[0]);
        Assert.Equal("parent", chain[1]);
        Assert.Equal("child", chain[2]);
    }

    [Fact]
    public void ValidateInheritanceChain_WithCircularDependency_ReturnsFalse()
    {
        // Arrange
        var profile1 = CreateTestProfile("profile1", "profile2");
        var profile2 = CreateTestProfile("profile2", "profile1");

        _manager.RegisterProfile(profile1);
        _manager.RegisterProfile(profile2);

        // Act & Assert
        Assert.False(_manager.ValidateInheritanceChain("profile1"));
        Assert.False(_manager.ValidateInheritanceChain("profile2"));
    }

    [Fact]
    public void ValidateInheritanceChain_WithValidChain_ReturnsTrue()
    {
        // Arrange
        var parent = CreateTestProfile("parent");
        var child = CreateTestProfile("child", "parent");

        _manager.RegisterProfile(parent);
        _manager.RegisterProfile(child);

        // Act & Assert
        Assert.True(_manager.ValidateInheritanceChain("child"));
        Assert.True(_manager.ValidateInheritanceChain("parent"));
    }

    [Fact]
    public void GetChildProfiles_ReturnsCorrectChildren()
    {
        // Arrange
        var parent = CreateTestProfile("parent");
        var child1 = CreateTestProfile("child1", "parent");
        var child2 = CreateTestProfile("child2", "parent");
        var unrelated = CreateTestProfile("unrelated");

        _manager.RegisterProfile(parent);
        _manager.RegisterProfile(child1);
        _manager.RegisterProfile(child2);
        _manager.RegisterProfile(unrelated);

        // Act
        var children = _manager.GetChildProfiles("parent").ToList();

        // Assert
        Assert.Equal(2, children.Count);
        Assert.Contains(children, c => c.ProfileId == "child1");
        Assert.Contains(children, c => c.ProfileId == "child2");
    }

    [Fact]
    public void ResolveInheritance_NonExistentProfile_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _manager.ResolveInheritance("non-existent"));
    }

    [Fact]
    public void ResolveInheritance_CircularDependency_ThrowsInvalidOperationException()
    {
        // Arrange
        var profile1 = CreateTestProfile("profile1", "profile2");
        var profile2 = CreateTestProfile("profile2", "profile1");

        _manager.RegisterProfile(profile1);
        _manager.RegisterProfile(profile2);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _manager.ResolveInheritance("profile1"));
    }

    [Fact]
    public void UnregisterProfile_RemovesProfile()
    {
        // Arrange
        var profile = CreateTestProfile("test-profile");
        _manager.RegisterProfile(profile);

        // Act
        _manager.UnregisterProfile("test-profile");

        // Assert
        Assert.Throws<ArgumentException>(() => _manager.ResolveInheritance("test-profile"));
    }

    private static IProfileConfiguration CreateTestProfile(string id, string? parentId = null)
    {
        var builder = new ProfileConfigurationBuilder()
            .WithProfileId(id)
            .WithName($"Profile {id}")
            .WithDescription($"Test profile {id}");

        if (!string.IsNullOrEmpty(parentId))
        {
            builder.InheritsFrom(parentId);
        }

        return builder.Build();
    }
}