using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for profile event arguments classes.
/// </summary>
public class ProfileEventArgsTests
{
    [Fact]
    public void ProfileActivatedEventArgs_Construction_ShouldSetProperties()
    {
        // Arrange
        var activatedProfile = CreateMockProfile("active", ProfileType.TUI);
        var previousProfile = CreateMockProfile("previous", ProfileType.UnityLike);
        
        // Act
        var eventArgs = new ProfileActivatedEventArgs(activatedProfile, previousProfile);
        
        // Assert
        Assert.Same(activatedProfile, eventArgs.ActivatedProfile);
        Assert.Same(previousProfile, eventArgs.PreviousProfile);
    }

    [Fact]
    public void ProfileActivatedEventArgs_WithoutPreviousProfile_ShouldHandleNull()
    {
        // Arrange
        var activatedProfile = CreateMockProfile("active", ProfileType.TUI);
        
        // Act
        var eventArgs = new ProfileActivatedEventArgs(activatedProfile);
        
        // Assert
        Assert.Same(activatedProfile, eventArgs.ActivatedProfile);
        Assert.Null(eventArgs.PreviousProfile);
    }

    [Fact]
    public void ProfileCreatedEventArgs_Construction_ShouldSetProperties()
    {
        // Arrange
        var createdProfile = CreateMockProfile("created", ProfileType.GodotLike);
        
        // Act
        var eventArgs = new ProfileCreatedEventArgs(createdProfile);
        
        // Assert
        Assert.Same(createdProfile, eventArgs.CreatedProfile);
    }

    [Fact]
    public void ProfileUpdatedEventArgs_Construction_ShouldSetProperties()
    {
        // Arrange
        var updatedProfile = CreateMockProfile("updated", ProfileType.Custom);
        
        // Act
        var eventArgs = new ProfileUpdatedEventArgs(updatedProfile);
        
        // Assert
        Assert.Same(updatedProfile, eventArgs.UpdatedProfile);
    }

    [Fact]
    public void ProfileDeletedEventArgs_Construction_ShouldSetProperties()
    {
        // Arrange
        var profileId = "test-id";
        var profileName = "Test Profile";
        
        // Act
        var eventArgs = new ProfileDeletedEventArgs(profileId, profileName);
        
        // Assert
        Assert.Equal(profileId, eventArgs.ProfileId);
        Assert.Equal(profileName, eventArgs.ProfileName);
    }

    private static IUIProfile CreateMockProfile(string name, ProfileType type)
    {
        return new TestUIProfile(name, type);
    }

    // Simple test implementation of IUIProfile for testing
    private class TestUIProfile : IUIProfile
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; }
        public string Description { get; } = "Test profile";
        public ProfileType Type { get; }
        public IReadOnlyDictionary<string, object> Configuration { get; } = 
            new Dictionary<string, object>();
        public bool IsActive { get; } = false;
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public TestUIProfile(string name, ProfileType type)
        {
            Name = name;
            Type = type;
        }

        public T GetConfigurationValue<T>(string key)
        {
            if (Configuration.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }
    }
}