using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI framework type definitions and enums.
/// </summary>
public class UITypesTests
{
    [Fact]
    public void UIFrameworkType_Should_Have_Expected_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<UIFrameworkType>();

        // Assert
        Assert.Contains(UIFrameworkType.Console, values);
        Assert.Contains(UIFrameworkType.Web, values);
        Assert.Contains(UIFrameworkType.Desktop, values);
        Assert.Contains(UIFrameworkType.Mobile, values);
        Assert.Contains(UIFrameworkType.Headless, values);
    }

    [Fact]
    public void UICapabilities_Should_Support_Flags()
    {
        // Arrange
        var capabilities = UICapabilities.TextInput | UICapabilities.ColorDisplay | UICapabilities.MouseInteraction;

        // Act & Assert
        Assert.True(capabilities.HasFlag(UICapabilities.TextInput));
        Assert.True(capabilities.HasFlag(UICapabilities.ColorDisplay));
        Assert.True(capabilities.HasFlag(UICapabilities.MouseInteraction));
        Assert.False(capabilities.HasFlag(UICapabilities.AudioOutput));
    }

    [Fact]
    public void UIComponentState_Should_Have_Lifecycle_Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<UIComponentState>();

        // Assert
        Assert.Contains(UIComponentState.Uninitialized, values);
        Assert.Contains(UIComponentState.Initialized, values);
        Assert.Contains(UIComponentState.Mounted, values);
        Assert.Contains(UIComponentState.Updating, values);
        Assert.Contains(UIComponentState.Unmounted, values);
    }

    [Theory]
    [InlineData(UIResponsiveMode.Fixed)]
    [InlineData(UIResponsiveMode.Adaptive)]
    [InlineData(UIResponsiveMode.Responsive)]
    [InlineData(UIResponsiveMode.Fluid)]
    public void UIResponsiveMode_Should_Have_All_Expected_Values(UIResponsiveMode mode)
    {
        // Arrange & Act
        var values = Enum.GetValues<UIResponsiveMode>();

        // Assert
        Assert.Contains(mode, values);
    }

    [Fact]
    public void UIUpdatePriority_Should_Order_Correctly()
    {
        // Arrange & Act
        var low = (int)UIUpdatePriority.Low;
        var normal = (int)UIUpdatePriority.Normal;
        var high = (int)UIUpdatePriority.High;
        var critical = (int)UIUpdatePriority.Critical;

        // Assert - Ensure proper ordering
        Assert.True(low < normal);
        Assert.True(normal < high);
        Assert.True(high < critical);
    }
}