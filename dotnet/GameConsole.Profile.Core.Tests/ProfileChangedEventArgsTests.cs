using GameConsole.Profile.Core;
using Xunit;

namespace GameConsole.Profile.Core.Tests;

/// <summary>
/// Tests for the ProfileChangedEventArgs class.
/// </summary>
public class ProfileChangedEventArgsTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var previousProfile = new TuiProfile();
        var currentProfile = new UnityProfile();

        // Act
        var eventArgs = new ProfileChangedEventArgs(previousProfile, currentProfile);

        // Assert
        Assert.Equal(previousProfile, eventArgs.PreviousProfile);
        Assert.Equal(currentProfile, eventArgs.CurrentProfile);
        Assert.True(eventArgs.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(eventArgs.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void Constructor_NullProfiles_HandledCorrectly()
    {
        // Act
        var eventArgs = new ProfileChangedEventArgs(null, null);

        // Assert
        Assert.Null(eventArgs.PreviousProfile);
        Assert.Null(eventArgs.CurrentProfile);
        Assert.True(eventArgs.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_MixedNullProfiles_HandledCorrectly()
    {
        // Arrange
        var currentProfile = new TuiProfile();

        // Act
        var eventArgs1 = new ProfileChangedEventArgs(null, currentProfile);
        var eventArgs2 = new ProfileChangedEventArgs(currentProfile, null);

        // Assert
        Assert.Null(eventArgs1.PreviousProfile);
        Assert.Equal(currentProfile, eventArgs1.CurrentProfile);
        
        Assert.Equal(currentProfile, eventArgs2.PreviousProfile);
        Assert.Null(eventArgs2.CurrentProfile);
    }

    [Fact]
    public void Timestamp_IsInUtc()
    {
        // Act
        var eventArgs = new ProfileChangedEventArgs(null, null);

        // Assert
        Assert.Equal(TimeSpan.Zero, eventArgs.Timestamp.Offset);
    }
}