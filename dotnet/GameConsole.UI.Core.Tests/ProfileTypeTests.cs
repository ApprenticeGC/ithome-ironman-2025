using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for ProfileType enum functionality and contract validation.
/// </summary>
public class ProfileTypeTests
{
    [Fact]
    public void ProfileType_DefaultTUI_ShouldBeZero()
    {
        // Arrange & Act
        var tuiValue = (int)ProfileType.TUI;
        
        // Assert
        Assert.Equal(0, tuiValue);
    }

    [Theory]
    [InlineData(ProfileType.TUI, 0)]
    [InlineData(ProfileType.UnityLike, 1)]
    [InlineData(ProfileType.GodotLike, 2)]
    [InlineData(ProfileType.Custom, 99)]
    public void ProfileType_Values_ShouldMatchExpected(ProfileType type, int expectedValue)
    {
        // Arrange & Act
        var actualValue = (int)type;
        
        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void ProfileType_AllValues_ShouldBeDefinedAndParseable()
    {
        // Arrange
        var expectedValues = new[] { ProfileType.TUI, ProfileType.UnityLike, ProfileType.GodotLike, ProfileType.Custom };
        
        // Act & Assert
        foreach (var expectedValue in expectedValues)
        {
            Assert.True(Enum.IsDefined(typeof(ProfileType), expectedValue));
            Assert.Equal(expectedValue, Enum.Parse<ProfileType>(expectedValue.ToString()));
        }
    }
}