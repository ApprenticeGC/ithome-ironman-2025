using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UITypesTests
{
    [Fact]
    public void ConsoleMode_HasCorrectFlags()
    {
        // Arrange & Act
        var all = ConsoleMode.All;
        var game = ConsoleMode.Game;
        var editor = ConsoleMode.Editor;
        var debug = ConsoleMode.Debug;

        // Assert
        Assert.True(all.HasFlag(game));
        Assert.True(all.HasFlag(editor));
        Assert.True(all.HasFlag(debug));
        Assert.Equal(7, (int)all); // 1 + 2 + 4 = 7
    }

    [Fact]
    public void UICapabilities_HasCorrectBitValues()
    {
        // Arrange & Act
        var textDisplay = UICapabilities.TextDisplay;
        var menuNavigation = UICapabilities.MenuNavigation;
        var formInput = UICapabilities.FormInput;

        // Assert
        Assert.Equal(1, (int)textDisplay);
        Assert.Equal(2, (int)menuNavigation);
        Assert.Equal(4, (int)formInput);
    }

    [Theory]
    [InlineData(UICapabilities.TextDisplay | UICapabilities.MenuNavigation, UICapabilities.TextDisplay, true)]
    [InlineData(UICapabilities.TextDisplay | UICapabilities.MenuNavigation, UICapabilities.FormInput, false)]
    [InlineData(UICapabilities.All, UICapabilities.ColorDisplay, true)]
    public void UICapabilities_FlagsWork(UICapabilities capabilities, UICapabilities check, bool expected)
    {
        // Act
        var hasCapability = capabilities.HasFlag(check);

        // Assert
        Assert.Equal(expected, hasCapability);
    }

    [Fact]
    public void UICapabilities_All_ContainsAllFlags()
    {
        // Arrange
        var all = UICapabilities.TextDisplay | UICapabilities.MenuNavigation | UICapabilities.FormInput | 
                 UICapabilities.TableDisplay | UICapabilities.TreeView | UICapabilities.KeyboardShortcuts |
                 UICapabilities.MouseInteraction | UICapabilities.ColorDisplay | UICapabilities.ProgressIndicators |
                 UICapabilities.StatusBar;

        // Act & Assert
        Assert.True(all.HasFlag(UICapabilities.TextDisplay));
        Assert.True(all.HasFlag(UICapabilities.StatusBar));
        Assert.True(all.HasFlag(UICapabilities.MouseInteraction));
    }

    [Theory]
    [InlineData(LayoutType.SingleColumn)]
    [InlineData(LayoutType.TwoColumn)]
    [InlineData(LayoutType.ThreeColumn)]
    [InlineData(LayoutType.Tabbed)]
    [InlineData(LayoutType.Split)]
    public void LayoutType_AllValuesAreDefined(LayoutType layoutType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(LayoutType), layoutType));
    }

    [Theory]
    [InlineData(UICommandPriority.Low, 0)]
    [InlineData(UICommandPriority.Normal, 1)]
    [InlineData(UICommandPriority.High, 2)]
    [InlineData(UICommandPriority.Critical, 3)]
    public void UICommandPriority_HasCorrectValues(UICommandPriority priority, int expectedValue)
    {
        // Act & Assert
        Assert.Equal(expectedValue, (int)priority);
    }

    [Theory]
    [InlineData(UIMessageType.Info)]
    [InlineData(UIMessageType.Warning)]
    [InlineData(UIMessageType.Error)]
    [InlineData(UIMessageType.Success)]
    [InlineData(UIMessageType.Debug)]
    public void UIMessageType_AllValuesAreDefined(UIMessageType messageType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(UIMessageType), messageType));
    }
}