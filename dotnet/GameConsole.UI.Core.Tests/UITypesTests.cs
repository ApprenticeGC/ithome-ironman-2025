using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UITypesTests
{
    [Fact]
    public void FrameworkType_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var console = FrameworkType.Console;
        var web = FrameworkType.Web;
        var desktop = FrameworkType.Desktop;
        
        // Assert
        Assert.Equal(0, (int)console);
        Assert.Equal(1, (int)web);
        Assert.Equal(2, (int)desktop);
    }

    [Fact]
    public void UICapabilities_ShouldSupportFlags()
    {
        // Arrange
        var capabilities = UICapabilities.TextInput | UICapabilities.MouseInteraction | UICapabilities.ColorDisplay;
        
        // Act & Assert
        Assert.True(capabilities.HasFlag(UICapabilities.TextInput));
        Assert.True(capabilities.HasFlag(UICapabilities.MouseInteraction));
        Assert.True(capabilities.HasFlag(UICapabilities.ColorDisplay));
        Assert.False(capabilities.HasFlag(UICapabilities.FileSelection));
        Assert.False(capabilities.HasFlag(UICapabilities.TableDisplay));
    }

    [Fact]
    public void ComponentType_ShouldHaveCommonUIElements()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)ComponentType.Button);
        Assert.Equal(1, (int)ComponentType.TextInput);
        Assert.Equal(2, (int)ComponentType.Label);
        Assert.Equal(3, (int)ComponentType.Panel);
    }

    [Fact]
    public void UIMode_ShouldRepresentDifferentModes()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)UIMode.CLI);
        Assert.Equal(1, (int)UIMode.TUI);
        Assert.Equal(2, (int)UIMode.Web);
        Assert.Equal(3, (int)UIMode.Desktop);
    }
}